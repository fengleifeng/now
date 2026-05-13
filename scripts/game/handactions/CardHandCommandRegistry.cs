using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

/// <summary>按卡牌 Id 解析操作源；未注册则使用 <see cref="DefaultHandCardCommandSource"/>。</summary>
/// <remarks>
/// 为某张卡定制操作：在启动时调用
/// <c>CardHandCommandRegistry.Register("herb", new TemplateHandCardCommandSource(new ViewHandCommand(), new MyHerbCommand(), new DiscardHandCommand()));</c>
/// 其中 <c>MyHerbCommand</c> 为继承 <see cref="HandCardCommandBase"/> 的自定义类。
/// </remarks>
public static class CardHandCommandRegistry
{
	private static readonly DefaultHandCardCommandSource Default = new();
	private static readonly Dictionary<string, ICardHandCommandSource> ByCardId = new();

	/// <summary>为指定卡牌 Id 注册专用操作源（覆盖默认）。</summary>
	public static void Register(string cardId, ICardHandCommandSource source) => ByCardId[cardId] = source;

	public static ICardHandCommandSource ResolveSource(CardData card) =>
		ByCardId.TryGetValue(card.Id, out var s) ? s : Default;

	public static IEnumerable<IHandCardCommand> CommandsFor(CardData card) =>
		ResolveSource(card).EnumerateFor(card);
}
