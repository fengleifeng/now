using System.Linq;
using CardSurvival.Data;
using CardSurvival.Game;

namespace CardSurvival.Game.HandActions;

/// <summary>通用手牌操作：每个命令对应一种「行为」，并明确改变玩家生存状态。</summary>
public sealed class ViewHandCommand : HandCardCommandBase
{
	public override string Id => "view";
	public override string Label => "查看";
	public override bool CanExecute(CardData _) => true;

	/// <summary>消耗少量时间，仅记录描述（不直接改状态）。</summary>
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "View", card);
		h.Log(I18n.Tf("log.view_fmt", card.Name, card.Description));
	}
}

public sealed class DrinkHandCommand : HandCardCommandBase
{
	public override string Id => "drink";
	public override string Label => "饮用";
	public override bool CanExecute(CardData card) => card.ThirstValue > 0;

	/// <summary>恢复口渴并消耗卡牌，日志显示状态变化。</summary>
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		var thirstBefore = h.Player.State.Thirst;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Drink", card);
		h.Player.Drink(card.ThirstValue);
		CardInstantEffectApplicator.Apply(h, card);
		h.Cards.ConsumeCardFromHand(card, "drink");
		var thirstGain = h.Player.State.Thirst - thirstBefore;
		h.Log(I18n.Tf("log.drink_fmt", card.Name, thirstGain, SurvivalLogFormatter.ColoredDelta(thirstGain)));
	}
}

public sealed class EatHandCommand : HandCardCommandBase
{
	public override string Id => "eat";
	public override string Label => "食用";
	public override bool CanExecute(CardData card) => card.FoodValue > 0;

	/// <summary>恢复饱食、可选治疗与营养，生食可能致病。</summary>
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		var hungerBefore = h.Player.State.Hunger;
		var healthBefore = h.Player.State.Health;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Eat", card);
		h.Player.Eat(card.FoodValue);
		if (card.HealValue > 0)
			h.Player.Heal(card.HealValue);
		h.Player.ApplyMealNutrients(card);
		CardInstantEffectApplicator.Apply(h, card);
		if (card.Tags.Contains(CardTag.Raw))
			h.Effects.TryTriggerDisease("food_poisoning", 0.2f);
		h.Cards.ConsumeCardFromHand(card, "eat");
		var hungerGain = h.Player.State.Hunger - hungerBefore;
		var healGain = h.Player.State.Health - healthBefore;
		h.Log(I18n.Tf("log.eat_fmt", card.Name, hungerGain, healGain));
	}
}

public sealed class UseHealHandCommand : HandCardCommandBase
{
	public override string Id => "use";
	public override string Label => "使用";
	public override bool CanExecute(CardData card) =>
		card.HealValue > 0 && card.FoodValue <= 0 && card.ThirstValue <= 0;

	/// <summary>治疗、尝试祛病，并应用卡牌瞬间效果。</summary>
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		var healthBefore = h.Player.State.Health;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Use", card);
		h.Player.Heal(card.HealValue);
		CardInstantEffectApplicator.Apply(h, card);
		h.Cards.ConsumeCardFromHand(card, "use");
		var cured = h.Effects.TryCureWithCard(card);
		foreach (var disease in cured)
			h.Log(I18n.Tf("log.cure_fmt", disease));
		var healGain = h.Player.State.Health - healthBefore;
		h.Log(I18n.Tf("log.use_fmt", card.Name, healGain));
	}
}

public sealed class DiscardHandCommand : HandCardCommandBase
{
	public override string Id => "discard";
	public override string Label => "丢弃";
	public override HandCardUiBand UiBand => HandCardUiBand.Footer;
	public override bool CanExecute(CardData _) => true;

	/// <summary>丢弃卡牌，仅消耗时间。</summary>
	public override void Execute(CardHandActionContext ctx, CardData card)
	{
		var h = ctx.Host;
		h.Settings.AdvanceTimeForActionMinutes(h.Time, "Discard", card);
		h.Cards.ConsumeCardFromHand(card, "discard");
		h.Log(I18n.Tf("log.discard_fmt", card.Name));
	}
}
