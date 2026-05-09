// scripts/ui/TableArea.cs
using Godot;

namespace CardSurvival.UI;

public partial class TableArea : Control
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        GD.Print("[TableArea] Ready");
    }
}
