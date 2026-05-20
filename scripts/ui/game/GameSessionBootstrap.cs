using System;
using CardSurvival.Game;

namespace CardSurvival.UI.Game;

/// <summary>
/// 创建并配置 <see cref="CardSurvivalGame"/> 实例，注入 Autoload 与 UI 回调。
/// </summary>
public static class GameSessionBootstrap
{
	/// <summary>
	/// 用全局服务与 UI 回调组装一局游戏逻辑对象（不依赖 Control 节点）。
	/// </summary>
	public static CardSurvivalGame Create(
		GameServices services,
		GameViewController view,
		Func<GamePopupController?> getPopups)
	{
		return new CardSurvivalGame(new CardSurvivalGame.Host
		{
			Cards = services.Cards,
			Player = services.Player,
			Time = services.Time,
			Map = services.Map,
			Combine = services.Combine,
			Effects = services.Effects,
			Save = services.Save,
			Settings = services.Settings,
			Log = view.AppendLog,
			RequestUiRefresh = view.RequestRefresh,
			RefreshCraftIfOpen = () => getPopups()?.RefreshCraftIfOpen(),
			OnAchievementProbe = () => services.Achievements.OnStateMayHaveChanged()
		});
	}

	/// <summary>根据 PlayerSystem 标志开始新局或读档。</summary>
	public static void StartOrLoad(GameServices services, CardSurvivalGame game)
	{
		if (services.Player.ConsumeLoadSaveRequest() && game.TryLoadGame())
			return;
		game.NewGame();
	}
}
