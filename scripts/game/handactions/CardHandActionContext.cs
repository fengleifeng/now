using CardSurvival.Game;

namespace CardSurvival.Game.HandActions;

/// <summary>手牌卡牌操作执行时可访问的游戏服务（由 <see cref="CardSurvivalGame"/> 构造）。</summary>
public readonly struct CardHandActionContext
{
	public CardSurvivalGame.Host Host { get; init; }
}
