using System.Collections.Generic;
using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

/// <summary>为某张手牌提供可用操作列表（可按卡牌 Id 注册不同实现）。</summary>
public interface ICardHandCommandSource
{
	IEnumerable<IHandCardCommand> EnumerateFor(CardData card);
}
