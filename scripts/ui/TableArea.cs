// scripts/ui/TableArea.cs
using Godot;
using CardSurvival.Data;
using System.Collections.Generic;

namespace CardSurvival.UI;

public partial class TableArea : HBoxContainer
{
	[Signal] public delegate void OnTableCardClickedEventHandler(CardNode card);

	private CardManager _cardManager = null!;

	public override void _Ready()
	{
		Alignment = BoxContainer.AlignmentMode.Center;
		AddThemeConstantOverride("separation", 12);
		MouseFilter = MouseFilterEnum.Pass;

		_cardManager = GetNode<CardManager>("/root/CardManager");
		GD.Print("[TableArea] Ready");
	}

	public void AddStartingCards()
	{
		AddLocationCard("forest", "森林", CardType.Location);
		AddLocationCard("lake", "湖泊", CardType.Location);
		AddLocationCard("mountain", "山地", CardType.Location);
	}

	public void AddLocationCard(string id, string name, CardType type)
	{
		var cardData = new CardData
		{
			Id = id,
			Name = name,
			Type = type,
			Stack = 1,
			MaxStack = 1,
			Tags = new List<CardTag>(),
			Durability = -1,
			Description = "可探索的区域",
			IsDraggable = false
		};

		_cardManager.AddCardToTable(cardData);

		var cardNode = new CardNode();
		cardNode.Setup(cardData);
		cardNode.OnCardClicked += OnCardClicked;
		AddChild(cardNode);
	}

	private void OnCardClicked(CardNode card)
	{
		EmitSignal(SignalName.OnTableCardClicked, card);
	}
}
