using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

/// <summary>通用手牌操作：饮用、进食、治疗使用、查看、丢弃。</summary>
public sealed class ViewHandCommand : HandCardCommandBase
{
	public override string Id => "view";
	public override string Label => "查看";
	public override bool CanExecute(CardData _) => true;
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "View", card);
		h.Log($"{card.Name}: {card.Description}");
	}
}

public sealed class DrinkHandCommand : HandCardCommandBase
{
	public override string Id => "drink";
	public override string Label => "饮用";
	public override bool CanExecute(CardData card) => card.ThirstValue > 0;
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Drink", card);
		h.Player.Drink(card.ThirstValue);
		h.Cards.ConsumeCardFromHand(card, "drink");
		h.Log($"喝了 {card.Name}，解渴 +{card.ThirstValue}。");
	}
}

public sealed class EatHandCommand : HandCardCommandBase
{
	public override string Id => "eat";
	public override string Label => "食用";
	public override bool CanExecute(CardData card) => card.FoodValue > 0;
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Eat", card);
		h.Player.Eat(card.FoodValue);
		if (card.HealValue > 0)
			h.Player.Heal(card.HealValue);
		h.Player.ApplyMealNutrients(card);
		if (card.Tags.Contains(CardTag.Raw))
			h.Effects.TryTriggerDisease("food_poisoning", 0.2f);
		h.Cards.ConsumeCardFromHand(card, "eat");
		h.Log($"食用了 {card.Name}。");
	}
}

public sealed class UseHealHandCommand : HandCardCommandBase
{
	public override string Id => "use";
	public override string Label => "使用";
	public override bool CanExecute(CardData card) =>
		card.HealValue > 0 && card.FoodValue <= 0 && card.ThirstValue <= 0;
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Use", card);
		h.Player.Heal(card.HealValue);
		h.Cards.ConsumeCardFromHand(card, "use");
		var cured = h.Effects.TryCureWithCard(card);
		foreach (var disease in cured)
			h.Log($"[color=green]治愈了 {disease}！[/color]");
		h.Log($"使用了 {card.Name}。");
	}
}

public sealed class DiscardHandCommand : HandCardCommandBase
{
	public override string Id => "discard";
	public override string Label => "丢弃";
	public override HandCardUiBand UiBand => HandCardUiBand.Footer;
	public override bool CanExecute(CardData _) => true;
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Discard", card);
		h.Cards.ConsumeCardFromHand(card, "discard");
		h.Log($"[color=gray]丢弃了 {card.Name}。[/color]");
	}
}
