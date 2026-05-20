using System;
using System.Collections.Generic;
using CardSurvival.Data;

namespace CardSurvival.Game;

/// <summary>
/// 将卡牌 <see cref="CardData.Effects"/> 与燃烧值应用到玩家状态（进食/饮用/使用瞬间）。
/// </summary>
public static class CardInstantEffectApplicator
{
	/// <summary>施加卡牌上配置的所有瞬间效果。</summary>
	public static void Apply(CardSurvivalGame.Host host, CardData card)
	{
		if (card.Effects.Count > 0)
			host.Effects.ApplyInstantCardEffects(card.Effects);

		if (card.BurnValue > 0)
			host.Player.UpdateTemperature(card.BurnValue);
	}

	/// <summary>
	/// 将 cards.json 简写（如 sanity+5）转为 EffectSystem 可解析格式（Sanity:+5）。
	/// </summary>
	public static string? NormalizeEffectString(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return null;
		if (raw.StartsWith("cure:", StringComparison.OrdinalIgnoreCase))
			return null;

		var idx = raw.IndexOfAny(new[] { '+', '-' });
		if (idx <= 0)
			return null;

		var key = raw[..idx].Trim().ToLowerInvariant();
		var tail = raw[idx..];
		var target = key switch
		{
			"health" => "Health",
			"hunger" => "Hunger",
			"thirst" => "Thirst",
			"energy" => "Energy",
			"sanity" => "Sanity",
			"immunity" => "Immunity",
			"warmth" or "temperature" => "Temperature",
			_ => null
		};
		return target == null ? null : $"{target}:{tail}";
	}
}
