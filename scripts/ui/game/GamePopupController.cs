using Godot;
using CardSurvival.Core;
using CardSurvival.Data;
using CardSurvival.Game;

namespace CardSurvival.UI.Game;

/// <summary>
/// 游戏内弹窗生命周期：打开/关闭合成、菜单、卡牌操作、分步合成等模态框。
/// </summary>
public sealed class GamePopupController
{
	private readonly GameServices _services;
	private readonly GameHud _hud;
	private readonly CardSurvivalGame _game;
	private readonly GameViewController _view;
	private readonly Action<CardData, string> _onCardAction;
	private Control? _popup;

	public GamePopupController(
		GameServices services,
		GameHud hud,
		CardSurvivalGame game,
		GameViewController view,
		Action<CardData, string> onCardAction)
	{
		_services = services;
		_hud = hud;
		_game = game;
		_view = view;
		_onCardAction = onCardAction;
	}

	/// <summary>关闭当前弹窗并刷新主界面。</summary>
	public void Close()
	{
		_popup?.QueueFree();
		_popup = null;
		_view.RefreshAll();
	}

	/// <summary>若合成弹窗已打开，在状态变化后刷新其列表。</summary>
	public void RefreshCraftIfOpen()
	{
		_view.RequestRefresh();
		if (_popup is CraftPopup craft)
			craft.Refresh(_game.GetLearnedRecipes(), _services.Player.GetActiveProjects(), _services.Cards.GetPlayableHand());
	}

	/// <summary>打开手牌卡牌操作弹窗。</summary>
	public void OpenCardAction(CardData card)
	{
		Close();
		_services.Settings.PlayCardUiSound(CardUiSoundKind.Tap);
		var popup = new CardActionPopup();
		_popup = popup;
		popup.Setup(card);
		_hud.PopupLayer.AddChild(popup);
		popup.OnAction += _onCardAction;
		popup.OnClose += Close;
	}

	/// <summary>打开分步合成确认弹窗。</summary>
	public void OpenStagedCombine()
	{
		var rule = _services.Cards.GetStagedCombineRule();
		if (rule == null)
			return;

		var popup = new StagedCombinePopup();
		popup.Setup(rule, _services.Cards);
		_popup = popup;
		_hud.PopupLayer.AddChild(popup);
		popup.OnSynthesize += () =>
		{
			if (_game.TryCompleteStagedCombine())
			{
				_services.Settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
				Close();
			}
		};
		popup.OnCancelStage += () =>
		{
			_game.CancelStagedCombine();
			Close();
		};
		popup.OnClose += Close;
	}

	/// <summary>打开合成/建造弹窗。</summary>
	public void OpenCraft()
	{
		Close();
		_services.Settings.PlayCardUiSound(CardUiSoundKind.Tap);
		var popup = new CraftPopup();
		_popup = popup;
		_hud.PopupLayer.AddChild(popup);
		popup.Refresh(_game.GetLearnedRecipes(), _services.Player.GetActiveProjects(), _services.Cards.GetPlayableHand());
		popup.OnCraftRecipe += rule =>
		{
			if (_game.CraftRecipe(rule))
				_services.Settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
		};
		popup.OnStageRecipe += rule =>
		{
			if (!_game.TryStageCombineRecipe(rule))
				return;
			_services.Settings.PlayCardUiSound(CardUiSoundKind.Tap);
			Close();
		};
		popup.OnBuildProject += id =>
		{
			if (_game.BuildProject(id))
				_services.Settings.PlayCardUiSound(CardUiSoundKind.Tap);
		};
		popup.OnFreeCraft += ingredients => _game.FreeCraft(ingredients);
		popup.OnClearFreeCraft += () => { };
		popup.OnClose += Close;
	}

	/// <summary>打开游戏菜单（存档、备份、退出）。</summary>
	public void OpenMenu()
	{
		Close();
		var popup = new MenuPopup();
		_popup = popup;
		_hud.PopupLayer.AddChild(popup);
		popup.OnSave += () =>
		{
			_game.SaveGame();
			_view.AppendLog(I18n.T("ui.save_ok"));
		};
		popup.OnBackup += () =>
		{
			_game.SaveUserBackup();
			_view.AppendLog(I18n.T("ui.backup_ok"));
		};
		popup.OnQuit += () => _hud.Root.GetTree().ChangeSceneToFile(ScenePaths.MainMenu);
		popup.OnClose += Close;
	}
}
