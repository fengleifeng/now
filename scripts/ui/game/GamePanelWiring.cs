using CardSurvival.Core;
using CardSurvival.Game;

namespace CardSurvival.UI.Game;

/// <summary>
/// 将各 HUD 面板的信号连接到游戏逻辑与输入/弹窗控制器。
/// </summary>
public static class GamePanelWiring
{
	/// <summary>绑定状态栏、场景区、手牌区与全局 Autoload 事件。</summary>
	public static void Connect(
		GameServices services,
		GameHud hud,
		CardSurvivalGame game,
		GameViewController view,
		GameInputController input,
		GamePopupController popups)
	{
		hud.StatusPanel.OnCraftClicked += popups.OpenCraft;
		hud.StatusPanel.OnRestClicked += () => game.Rest();
		hud.StatusPanel.OnSharpenClicked += () => game.Sharpen();
		hud.StatusPanel.OnNightRitualClicked += () => game.NightRitual();
		hud.StatusPanel.OnMenuClicked += popups.OpenMenu;

		hud.SceneArea.OnLocationExploreClicked += () => game.Explore();
		hud.SceneArea.OnLocationMoveClicked += game.MoveToLocation;
		hud.SceneArea.OnSceneCardClicked += input.OnSceneCardClicked;
		hud.SceneArea.OnSceneCardDragEnded += input.OnSceneCardDragEnded;

		hud.HandArea.OnHandCardClicked += input.OnHandCardClicked;
		hud.HandArea.OnHandCardDragEnded += input.OnHandCardDragEnded;

		services.Player.OnStatsChanged += view.RequestRefresh;
		services.Player.OnPlayerDeath += () => OnPlayerDeath(services, game, hud);
		services.Cards.OnCardAdded += _ => view.RequestRefresh();
		services.Cards.OnCardRemoved += _ => view.RequestRefresh();
		services.Cards.OnCardConsumed += game.OnCardConsumed;
		services.Cards.OnSceneCardsChanged += view.RequestRefresh;
		services.Map.OnLocationChanged += _ => view.RequestRefresh();
		services.Time.OnWeatherChanged += _ => view.RequestRefresh();
		services.Time.OnDayChanged += _ => view.RequestRefresh();
	}

	/// <summary>玩家死亡：清理会话并返回主菜单。</summary>
	private static void OnPlayerDeath(GameServices services, CardSurvivalGame game, GameHud hud)
	{
		game.OnPlayerDeath();
		hud.Root.GetTree().ChangeSceneToFile(ScenePaths.MainMenu);
	}
}
