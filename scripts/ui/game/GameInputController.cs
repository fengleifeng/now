using System;
using CardSurvival.Data;
using CardSurvival.Game;

namespace CardSurvival.UI.Game;

/// <summary>
/// 游戏内输入路由：手牌/场景点击、拖拽结束后的合成/舞动/拾取等，委托 <see cref="CardSurvivalGame"/> 执行业务。
/// </summary>
public sealed class GameInputController
{
	private readonly GameServices _services;
	private readonly GameHud _hud;
	private readonly CardSurvivalGame _game;
	private readonly GameViewController _view;
	private readonly Func<GamePopupController> _popups;

	public GameInputController(
		GameServices services,
		GameHud hud,
		CardSurvivalGame game,
		GameViewController view,
		Func<GamePopupController> popups)
	{
		_services = services;
		_hud = hud;
		_game = game;
		_view = view;
		_popups = popups;
	}

	private GamePopupController Popups => _popups();

	/// <summary>手牌卡牌被点击（非拖拽）。</summary>
	public void OnHandCardClicked(CardNode node)
	{
		Popups.Close();
		_services.Settings.PlayCardUiSound(CardUiSoundKind.Tap);

		if (CardManager.IsStagedCombineDisplay(node.Data))
		{
			Popups.OpenStagedCombine();
			return;
		}

		if (node.Data.IsStagedIngredient)
			return;

		Popups.OpenCardAction(node.Data);
	}

	/// <summary>锚定条上的卡牌被点击。</summary>
	public void OnAnchoredCardClicked(CardNode cardNode)
	{
		Popups.OpenCardAction(cardNode.Data);
	}

	/// <summary>卡牌操作弹窗中选择了某动作。</summary>
	public void HandleCardAction(CardData card, string action)
	{
		_game.HandleCardAction(card, action);
		switch (action)
		{
			case "view":
				break;
			case "discard":
				_services.Settings.PlayCardUiSound(CardUiSoundKind.Discard);
				break;
			default:
				_services.Settings.PlayCardUiSound(CardUiSoundKind.Use);
				break;
		}
	}

	/// <summary>手牌拖拽结束：尝试两卡合成、场景舞动、或放入场景。</summary>
	public void OnHandCardDragEnded(CardNode dropped)
	{
		if (!dropped.WasDragged)
			return;

		var data = dropped.Data;
		if (CardManager.IsStagedCombineDisplay(data) || data.IsStagedIngredient)
		{
			_view.RefreshAll();
			return;
		}

		var hand = _services.Cards.GetHandForUi();
		var target = dropped.GetOverlappingCard(_hud.HandArea);
		if (target != null && hand.Contains(data) && hand.Contains(target.Data))
		{
			if (_game.TryCombineHandPair(data, target.Data))
				_view.RequestRefresh();
			else
				_view.RefreshAll();
			return;
		}

		if (dropped.IsOverArea(_hud.SceneArea) && hand.Contains(data))
		{
			if (_game.TrySceneHandDance(data, out var dancePulse))
			{
				if (dancePulse)
					_hud.SceneArea.PlayDancePulse();
				_services.Settings.PlayCardUiSound(
					dancePulse ? CardUiSoundKind.CombineSuccess : CardUiSoundKind.Tap);
				_view.RequestRefresh();
				return;
			}

			_game.OnHandCardToScene(data);
			_services.Settings.PlayCardUiSound(CardUiSoundKind.Drop);
			return;
		}

		_view.RefreshAll();
	}

	/// <summary>场景物品被点击：拾回手牌。</summary>
	public void OnSceneCardClicked(CardNode node)
	{
		var card = node.Data;
		if (card.Type == CardType.Location)
			return;
		if (!_services.Cards.GetSceneCards().Contains(card))
			return;

		_game.OnSceneCardToHand(card);
		_services.Settings.PlayCardUiSound(CardUiSoundKind.Pickup);
	}

	/// <summary>场景卡牌拖到手牌区：拾回手牌。</summary>
	public void OnSceneCardDragEnded(CardNode dropped)
	{
		var card = dropped.Data;
		if (dropped.IsOverArea(_hud.HandArea) && _services.Cards.GetSceneCards().Contains(card))
		{
			_game.OnSceneCardToHand(card);
			_services.Settings.PlayCardUiSound(CardUiSoundKind.Pickup);
		}
		else
		{
			_view.RefreshAll();
		}
	}
}
