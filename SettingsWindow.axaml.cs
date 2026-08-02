using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace sweepalonia;

public partial class SettingsWindow : Window
{
    private MainWindow main;
    
    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        
        main = mainWindow;
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        TextBox rowBox = RowBox;
        TextBox columnBox = ColumnBox;
        TextBox mineBox = MineBox;
        
        if (string.IsNullOrEmpty(rowBox.Text) || 
            string.IsNullOrEmpty(columnBox.Text))
        {
            Console.WriteLine("** No rows and/or columns given. **");
            return;
        }
        
        int rowsNum = int.Parse(rowBox.Text);
        int colsNum = int.Parse(columnBox.Text);

        // Limit minimum cells
        if (rowsNum < 3 || colsNum < 3)
        {
            Console.WriteLine("** Too few cells! (min: 3x3) **");
            return;
        }
        
        // Limit maximum cells
        if (rowsNum > 50 || colsNum > 50)
        {
            Console.WriteLine("** Too many cells! (max: 50x50) **");
            return;
        }
        
        double customMinePercentage;
        double parsedMineBox =  double.Parse(mineBox.Text);
        if (string.IsNullOrEmpty(mineBox.Text)) // Use default percentage when mine textbox is empty
        {
            Console.WriteLine("** No mine percentage given, using default: " + main.defaultMinePercentage + " **");
            customMinePercentage = main.defaultMinePercentage;
        }
        else if (parsedMineBox <= 0)   // Invalid percentage(0 or under)
        {
            Console.WriteLine("** Invalid percentage. **");
            return;
        }
        else if (parsedMineBox > 1)
        {
            Console.WriteLine("** Too high mine percentage. **");   // Invalid percentage(over 1)
            return;
        }
        else
        {
            customMinePercentage = parsedMineBox; 
        }
        
        main.NewGame(rowsNum, colsNum, customMinePercentage);
    }
}
