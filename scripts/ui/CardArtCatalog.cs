using System;
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

/// <summary>
/// 解析卡牌展示用图标路径：优先 cards.json 的 IconPath，其次按 Id / 标签 / 类型的默认 SVG。
/// </summary>
public static class CardArtCatalog
{
	private const string CardsRoot = "res://assets/cards/";

	private static readonly CardTag[] TagPriority =
	{
		CardTag.Wood, CardTag.Water, CardTag.Food, CardTag.Meat, CardTag.Fire, CardTag.Shelter,
		CardTag.Medicine, CardTag.Metal, CardTag.Stone, CardTag.Berry, CardTag.Plant, CardTag.Nature
	};

	/// <summary>返回可用于 ResourceLoader 的纹理路径；文件不存在时仍返回类型默认路径。</summary>
	public static string ResolveIconPath(CardData card)
	{
		if (!string.IsNullOrWhiteSpace(card.IconPath) && ResourceLoader.Exists(card.IconPath))
			return card.IconPath;

		var byId = $"{CardsRoot}ids/{card.Id}.svg";
		if (ResourceLoader.Exists(byId))
			return byId;

		foreach (var tag in TagPriority)
		{
			if (!card.Tags.Contains(tag))
				continue;
			var byTag = $"{CardsRoot}tags/{tag.ToString().ToLowerInvariant()}.svg";
			if (ResourceLoader.Exists(byTag))
				return byTag;
		}

		return $"{CardsRoot}types/{card.Type.ToString().ToLowerInvariant()}.svg";
	}

	/// <summary>加载卡牌图标纹理；失败时返回 null（由 UI 显示字形兜底）。</summary>
	public static Texture2D? LoadIcon(CardData card)
	{
		var path = ResolveIconPath(card);
		if (!ResourceLoader.Exists(path))
			return null;
		return ResourceLoader.Load<Texture2D>(path);
	}
}
