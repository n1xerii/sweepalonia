using Avalonia.Controls;

namespace sweepalonia;

public class Cell
{
    public int myRow { get; set; }
    public int myCol { get; set; }
    public Button myBtn { get; set; }
    public bool revealed{ get; set; }
    public bool isMine{ get; set; }
    public bool flagged { get; set; }
}