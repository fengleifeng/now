using System.Collections.Generic;
using System.Linq;
using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

/// <summary>
/// 用固定命令模板列表构造操作源，便于为单张卡注册：<c>Register("herb", new TemplateHandCardCommandSource(...))</c>。
/// </summary>
public sealed class TemplateHandCardCommandSource : ICardHandCommandSource
{
	private readonly IHandCardCommand[] _order;

	public TemplateHandCardCommandSource(params IHandCardCommand[] order) => _order = order;

	public IEnumerable<IHandCardCommand> EnumerateFor(CardData card) =>
		_order.Where(c => c.CanExecute(card));
}
