// scripts/ui/HandArea.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class HandArea : HBoxContainer
{
    public override void _Ready()
    {
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 8);
    }

    public void Refresh(List<CardData> hand)
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        foreach (var card in hand)
        {
            var cardNode = new CardNode();
            cardNode.Setup(card);
            AddChild(cardNode);
        }
    }
}
