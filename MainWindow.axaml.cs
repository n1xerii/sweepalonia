using System;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace minesweepy;

public partial class MainWindow : Window
{
    private SettingsWindow settings;
    
    // DATA
    private Cell[,]? cells;
    private Button[,]? buttons;
    private int rows;
    private int columns;
    private int mineCount;
    private bool lostGame = false;
    private bool wonGame = false;
    int winConditionCount = 0;
    
    public int Rows
    {
        get { return rows; }
        set 
        { 
            if (value <= 0) { rows = 10; }
            else { rows = value; }
        }
    }
    public int Columns
    {
        get { return columns; }
        set 
        {
            if (value <= 0) { columns = 10; }
            else { columns = value; }
        }
    }
    public int MineCount
    {
        get { return mineCount; }
        set 
        {
            if (value <= 0) { mineCount = 10; }
            else { mineCount = value; }
        }
    }
    
    // MINES
    private bool[,]? mines { get; set; }
    private bool firstClick;
    public readonly double defaultMinePercentage = 0.12;
    public readonly double mediumMinePercentage = 0.16;
    public readonly double hardMinePercentage = 0.20;
    public readonly double impossibleMinePercentage = 0.30;
    
    // TIMER
    private Timer timer;
    private int seconds = 0;

    public MainWindow()
    {
        InitializeComponent();
        
        settings = new SettingsWindow(this);
        NewGame(10,10, defaultMinePercentage);
    }
    
    public void NewGame(int newRows, int newColumns, double minePercentage)
    {
        double newMines = newRows * newColumns * minePercentage;
        int finalMines = Convert.ToInt32(newMines);
        
        SetGameData(newRows, newColumns, finalMines);
        MakeBoard();
        
        Console.WriteLine("* Cells: " + cells.Length + " *");
        Console.WriteLine("* Mines: " + finalMines + " *");
    }
    public void StartDifficulty(string diff)
    {
        string newDiff = diff.ToLower();
        
        switch (newDiff)
        {
            case "easy":
                NewGame(10, 10, defaultMinePercentage);
                break;
            case "medium":
                NewGame(16, 16, mediumMinePercentage);
                break;
            case "hard":
                NewGame(24, 24, hardMinePercentage);
                break;
            case "impossible":
                NewGame(32, 32, impossibleMinePercentage);
                break;
            default:
                NewGame(10, 10, defaultMinePercentage);
                break;
        }
    }

    // BOARD
    public void SetGameData(int amountOfRows, int amountOfCols, int amountOfMines)
    {
        Rows = 0;
        Columns = 0;
        MineCount = 0;
        cells = null;
        buttons = null;
        mines = null;
        
        Rows = amountOfRows;
        Columns = amountOfCols;
        MineCount = amountOfMines;

        if (timer != null) EndGameTimer();
        winConditionCount = 0;
        wonGame = false;
        lostGame = false;
        firstClick = true;
    }
    public void MakeBoard()
    {
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Children.Clear();
        for (int r = 0; r < Rows; r++) { BoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); }
        for (int c = 0; c < Columns; c++) { BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); }
        
        cells = new Cell[Rows, Columns];
        buttons = new Button[Rows, Columns];

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                cells[r, c] = new Cell();
                cells[r, c].myRow = r;
                cells[r, c].myCol = c;
                
                var button = new Button();
                buttons[r, c] = button;
                cells[r, c].myBtn = button;
                    
                button.Tag = (r, c);
                button.Name =  $"{r}_{c}";
                button.Click += Cell_Click;
                button.ContextRequested += Cell_RightClick;
                
                Grid.SetRow(button, r);
                Grid.SetColumn(button, c);
                
                button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                button.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                button.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
                
                button.Background = Brushes.DimGray;
                
                BoardGrid.Children.Add(button);
            }
        }
    }

    // CELLS
    private void RecursiveCellReveal(int row, int col)
    {
        if (cells == null) return;
        
        // BOUNDS
        if (row < 0 || row >= Rows ||
            col < 0 || col >= Columns)
            return;

        Cell cell = cells[row, col];
        Button button = buttons[row, col];

        if (cell.revealed) return;
        if (cell.flagged)
        {
            Console.WriteLine("** Unflag before clicking **");
            return;
        }

        cell.revealed = true;
        winConditionCount++; // increase win count on every reveal
        WinCondition();
        
        if (cell.isMine)
        {
            button.Background = Brushes.Red;
            GameOver();
            return;
        }
        button.Background = Brushes.Green;
        
        int adjMineCount = CountAdjacentMines(row, col);   // check adjacent mines
        if (adjMineCount > 0)
        {
            button.Content = adjMineCount.ToString();   // set button text to adjacent mine count
            
            // responsive font
            button.FontSize = 20.0;
            button.LayoutUpdated += (_, __) =>
            {
                if (adjMineCount <= 0) return;
                var w = button.Bounds.Width;
                var h = button.Bounds.Height;
                if (w > 0 && h > 0) button.FontSize = Math.Min(w, h) * 0.6;
            };

            return;
        }
        
        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int colOffset = -1; colOffset <= 1; colOffset++)
            {
                if (rowOffset == 0 && colOffset == 0)
                    continue;

                RecursiveCellReveal(row + rowOffset, col + colOffset);
            }
        }
    }

    // MINES
    private void MakeMines(int mRows, int mCols, int amountOfMines, int clickedRow, int clickedColumn)
    {
        mines = new bool[mRows, mCols];
        Random rng = new Random();
        
        int placed = 0;
        while (placed < amountOfMines)
        {
            int row = rng.Next(Rows);
            int col = rng.Next(Columns);

            // Prevent making a mine on clicked cell
            if (cells[row, col] == cells[clickedRow, clickedColumn]) continue;
            
            // Skip already created mines
            if (mines[row, col]) continue;

            mines[row, col] = true;
            cells[row, col].isMine = true;
            placed++;
        }
    }
    private int CountAdjacentMines(int row, int col)
    {
        int count = 0;

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int colOffset = -1; colOffset <= 1; colOffset++)
            {
                if (rowOffset == 0 && colOffset == 0)
                    continue;

                int neighborRow = row + rowOffset;
                int neighborCol = col + colOffset;

                if (neighborRow < 0 || neighborRow >= Rows ||
                    neighborCol < 0 || neighborCol >= Columns)
                    continue;

                if (cells[neighborRow, neighborCol].isMine)
                    count++;
            }
        }

        return count;
    }
    
    // FLAGGING
    private void FlagCell(int row, int col)
    {
        if (lostGame || wonGame) return;
        if (cells == null) return;
        if (buttons == null) return;
        
        Cell thisCell = cells[row, col];
        Button cellBtn = buttons[row, col];

        if (thisCell.revealed) { return; }
        
        thisCell.flagged = !thisCell.flagged;

        if (thisCell.flagged) { cellBtn.Background = Brushes.Orange; }
        else { cellBtn.Background = Brushes.DimGray; }
    }
    
    // CLICKS
    private void Cell_Click(object? sender, RoutedEventArgs e)
    {
        if (lostGame || wonGame) return;
        if (sender is not Button button)
            return;
        
        var (r, c) = ((int, int))button.Tag;
        
        if (firstClick) // Generate mines after first click
        {
            MakeMines(Rows, Columns,  MineCount, r, c);
            firstClick = false;
            StartGameTimer();
        }
        
        RecursiveCellReveal(r, c);
    }
    private void Cell_RightClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        
        var (r, c) = ((int, int))button.Tag;

        FlagCell(r, c);
    }
    
    // WIN
    private void WinCondition()
    {
        int mustReveal = Rows * Columns - MineCount;
        
        // Check win condition and turn button colors
        if (winConditionCount >= mustReveal)
        {
            wonGame = true;
            EndGameTimer();
        
            if (buttons == null) return;
            if (cells == null) return;

            foreach (Cell cell in cells)
            {
                if (cell.isMine) continue;
                
                cell.myBtn.Click -= Cell_Click;
                cell.myBtn.Background = Brushes.DarkGreen;
            }
            foreach (Cell cell in cells)
            {
                if (cell.isMine)
                {
                    cell.myBtn.Background = Brushes.LawnGreen;
                }
            }
        }
    }
    // LOSS
    private void GameOver()
    {
        lostGame = true;
        EndGameTimer();
        
        if (buttons == null) return;
        if (cells == null) return;

        foreach (Cell cell in cells)
        {
            if (cell.isMine) continue;

            cell.myBtn.Click -= Cell_Click;
            cell.myBtn.Background = Brushes.DarkSlateGray;
        }
        foreach (Cell cell in cells)
        {
            if (cell.isMine)
            {
                cell.myBtn.Background = Brushes.DarkRed;
            }
        }
    }
    // TIMER
    public void StartGameTimer()
    {
        timer = new Timer(1000);
        timer.Elapsed += OnTimerElapsed;
        timer.AutoReset = true;
        timer.Start();
    }
    private void EndGameTimer()
    {
        timer.Stop(); 
        seconds = 0;
    }
    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        seconds++;
        Console.WriteLine("*** Time: " + seconds + " ***");
    }
    
    // TOP MENU
    private void MenuEasy_Click(object? sender, RoutedEventArgs e)
    { StartDifficulty("Easy"); }
    private void MenuMedium_Click(object? sender, RoutedEventArgs e)
    { StartDifficulty("Medium"); }
    private void MenuHard_Click(object? sender, RoutedEventArgs e)
    { StartDifficulty("Hard"); }
    private void MenuImpossible_Click(object? sender, RoutedEventArgs e)
    { StartDifficulty("Impossible"); }
    
    // SETTINGS
    private void Settings_Click(object? sender, RoutedEventArgs e)
    { settings.Show(); }
    
    // EXIT
    private void Exit() { settings.Close(); Close(); }
    private void Exit_Click(object? sender, RoutedEventArgs e) { Exit(); }
    private void TopLevel_OnClosed(object? sender, EventArgs e) { Exit(); }
}