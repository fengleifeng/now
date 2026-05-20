using System.Linq;
using Godot;
using CardSurvival;
using CardSurvival.Data;
using CardSurvival.Game.HandActions;

namespace CardSurvival.UI;

public partial class CardActionPopup : Control
{
	public event Action<CardData, string>? OnAction;
	public event Action? OnClose;

	private CardData _card = null!;

	public void Setup(CardData card)
	{
		_card = card;
	}

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		BuildUI();
	}

	private void BuildUI()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		UiLayout.ClampPanelMinSize(shell, new Vector2(380, 320));
		UiLayout.BindResponsive(this, () => UiLayout.ClampPanelMinSize(shell, new Vector2(380, 320)));
		GameTheme.ApplyModalPanel(shell);
		center.AddChild(shell);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		shell.AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		margin.AddChild(box);

		var artRow = new HBoxContainer();
		artRow.AddThemeConstantOverride("separation", 10);
		artRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(artRow);

		var artSlot = new PanelContainer();
		artSlot.CustomMinimumSize = new Vector2(72, 72);
		var slotStyle = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.4f),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6,
			BorderColor = new Color(1, 1, 1, 0.15f)
		};
		slotStyle.SetBorderWidthAll(1);
		artSlot.AddThemeStyleboxOverride("panel", slotStyle);
		artRow.AddChild(artSlot);

		var slotMargin = new MarginContainer();
		slotMargin.AddThemeConstantOverride("margin_left", 4);
		slotMargin.AddThemeConstantOverride("margin_right", 4);
		slotMargin.AddThemeConstantOverride("margin_top", 4);
		slotMargin.AddThemeConstantOverride("margin_bottom", 4);
		artSlot.AddChild(slotMargin);

		var stack = new Control { CustomMinimumSize = new Vector2(64, 64) };
		slotMargin.AddChild(stack);

		var glyph = new Label();
		glyph.SetAnchorsPreset(LayoutPreset.FullRect);
		glyph.HorizontalAlignment = HorizontalAlignment.Center;
		glyph.VerticalAlignment = VerticalAlignment.Center;
		glyph.AddThemeFontSizeOverride("font_size", 28);
		glyph.AddThemeColorOverride("font_color", Colors.White);
		glyph.Text = string.IsNullOrEmpty(_card.Name) ? "?" : _card.Name.Substring(0, 1);
		stack.AddChild(glyph);

		var icon = new TextureRect();
		icon.SetAnchorsPreset(LayoutPreset.FullRect);
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		icon.Visible = false;
		var tex = CardArtCatalog.LoadIcon(_card);
		if (tex != null)
		{
			icon.Texture = tex;
			icon.Visible = true;
			glyph.Visible = false;
		}

		stack.AddChild(icon);

		var titleCol = new VBoxContainer();
		titleCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleCol.AddThemeConstantOverride("separation", 4);
		artRow.AddChild(titleCol);

		var titleRow = new HBoxContainer();
		titleRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleCol.AddChild(titleRow);

		var titleLabel = new Label { Text = _card.Name };
		titleLabel.AddThemeFontSizeOverride("font_size", 18);
		titleLabel.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleRow.AddChild(titleLabel);

		var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(28, 28) };
		GameTheme.StyleSidebarButton(closeBtn);
		closeBtn.Pressed += () => OnClose?.Invoke();
		titleRow.AddChild(closeBtn);

		var typeLabel = new Label
		{
			Text = $"[{GetTypeText(_card.Type)}]  {string.Join(" ", _card.Tags.Select(t => t.ToString()))}"
		};
		typeLabel.AddThemeFontSizeOverride("font_size", 11);
		typeLabel.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		typeLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleCol.AddChild(typeLabel);

		box.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var descLabel = new Label { Text = _card.Description };
		descLabel.AddThemeFontSizeOverride("font_size", 13);
		descLabel.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		descLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		descLabel.CustomMinimumSize = new Vector2(0, 36);
		box.AddChild(descLabel);

		var attrText = GetAttrText();
		if (!string.IsNullOrEmpty(attrText))
		{
			var attrLabel = new Label { Text = attrText };
			attrLabel.AddThemeFontSizeOverride("font_size", 12);
			attrLabel.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.45f));
			box.AddChild(attrLabel);
		}

		box.AddChild(new HSeparator { SelfModulate = GameTheme.Separator });

		var cmds = CardHandCommandRegistry.CommandsFor(_card).ToList();

		var actionRow = new HBoxContainer();
		actionRow.AddThemeConstantOverride("separation", 8);
		actionRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(actionRow);

		foreach (var cmd in cmds.Where(c => c.UiBand == HandCardUiBand.Toolbar))
			AddActionButton(actionRow, cmd.Label, cmd.Id);

		var spacer = new Control();
		spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
		box.AddChild(spacer);

		var bottomRow = new HBoxContainer();
		bottomRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(bottomRow);

		var spacer2 = new Control();
		spacer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		bottomRow.AddChild(spacer2);

		var discardCmd = cmds.FirstOrDefault(c => c.UiBand == HandCardUiBand.Footer);
		if (discardCmd != null)
		{
			var discardBtn = new Button { Text = discardCmd.Label, CustomMinimumSize = new Vector2(80, 28) };
			discardBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
			var id = discardCmd.Id;
			discardBtn.Pressed += () =>
			{
				OnAction?.Invoke(_card, id);
				OnClose?.Invoke();
			};
			bottomRow.AddChild(discardBtn);
		}
	}

	private void AddActionButton(HBoxContainer parent, string text, string action)
	{
		var btn = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(70, 30),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		GameTheme.StyleSidebarButton(btn);
		btn.Pressed += () =>
		{
			OnAction?.Invoke(_card, action);
			OnClose?.Invoke();
		};
		parent.AddChild(btn);
	}

	private string GetAttrText()
	{
		var parts = new System.Collections.Generic.List<string>();
		if (_card.FoodValue > 0) parts.Add(I18n.Tf("cardaction.hunger_fmt", _card.FoodValue));
		if (_card.ThirstValue > 0) parts.Add(I18n.Tf("cardaction.thirst_fmt", _card.ThirstValue));
		if (_card.HealValue > 0) parts.Add(I18n.Tf("cardaction.heal_fmt", _card.HealValue));
		if (_card.ProteinValue != 0)
			parts.Add(I18n.Tf("cardaction.protein_fmt", _card.ProteinValue > 0 ? "+" : "", _card.ProteinValue));
		if (_card.VitaminValue != 0)
			parts.Add(I18n.Tf("cardaction.vitamin_fmt", _card.VitaminValue > 0 ? "+" : "", _card.VitaminValue));
		if (_card.CarbValue != 0)
			parts.Add(I18n.Tf("cardaction.carb_fmt", _card.CarbValue > 0 ? "+" : "", _card.CarbValue));
		if (_card.BodyFatDelta != 0)
			parts.Add(I18n.Tf("cardaction.bodyfat_fmt", _card.BodyFatDelta > 0 ? "+" : "", _card.BodyFatDelta));
		if (_card.Durability > 0) parts.Add(I18n.Tf("cardaction.durability_fmt", _card.Durability));
		if (_card.BurnValue > 0) parts.Add(I18n.Tf("cardaction.burn_fmt", _card.BurnValue));
		if (_card.ToolPower > 0) parts.Add(I18n.Tf("cardaction.toolpower_fmt", _card.ToolPower));
		if (_card.ArmorValue > 0) parts.Add(I18n.Tf("cardaction.armor_fmt", _card.ArmorValue));
		return string.Join("  ", parts);
	}

	private static string GetTypeText(CardType type) => I18n.CardTypeName(type);
}
