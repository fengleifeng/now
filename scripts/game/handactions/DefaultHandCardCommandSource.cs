using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

/// <summary>默认：查看 → 饮用/食用/治疗使用（按数据可见）→ 丢弃。</summary>
public sealed class DefaultHandCardCommandSource : ICardHandCommandSource
{
	private static readonly IHandCardCommand[] ToolbarOrder =
	{
		new ViewHandCommand(),
		new DrinkHandCommand(),
		new EatHandCommand(),
		new UseHealHandCommand()
	};

	private static readonly IHandCardCommand[] FooterOrder = { new DiscardHandCommand() };

	public IEnumerable<IHandCardCommand> EnumerateFor(CardData card)
	{
		foreach (var c in ToolbarOrder.Where(x => x.CanExecute(card)))
			yield return c;
		foreach (var c in FooterOrder.Where(x => x.CanExecute(card)))
			yield return c;
	}
}
