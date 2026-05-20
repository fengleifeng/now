using Godot;
using CardSurvival;
using CardSurvival.Data;

namespace CardSurvival.UI;

/// <summary>
/// 单张卡牌控件：上方插画区、下方名称与属性，拖拽/点击逻辑不变。
/// </summary>
public partial class CardNode : Control
{
	[Signal] public delegate void OnCardDragEndedEventHandler(CardNode card);
	[Signal] public delegate void OnCardClickedEventHandler(CardNode card);

	public CardData Data { get; private set; } = null!;
	public bool WasDragged => _wasDragged;

	private Label _nameLabel = null!;
	private Label _typeLabel = null!;
	private Label _attrLabel = null!;
	private ColorRect _fill = null!;
	private Label _glyphLabel = null!;
	private TextureRect _iconRect = null!;
	private ColorRect _typeAccent = null!;
	private PanelContainer _artPanel = null!;
	private bool _initialized;
	private bool _isDragging;
	private bool _wasDragged;
	private bool _mousePressedOnNode;
	private Vector2 _dragOffset;
	private Vector2 _dragStart;
	private Vector2 _dropGlobalPos;

	public override void _Ready() => InitializeUI();

	/// <summary>绑定卡牌数据并刷新显示（名称、类型、图标）。</summary>
	public void Setup(CardData data)
	{
		InitializeUI();
		Data = data;
		_nameLabel.Text = data.Stack > 1 ? data.Name + " x" + data.Stack : data.Name;
		_typeLabel.Text = GetTypeText(data.Type);
		_attrLabel.Text = GetAttrText(data);
		var typeColor = GetTypeColor(data.Type);
		_fill.Color = typeColor;
		_typeAccent.Color = typeColor.Lightened(0.12f);
		TooltipText = data.Description;
		MouseDefaultCursorShape = data.IsDraggable ? CursorShape.PointingHand : CursorShape.Arrow;
		ApplyCardArt(data);
	}

	/// <summary>加载默认或自定义图标；无图时显示名称首字。</summary>
	private void ApplyCardArt(CardData data)
	{
		var tex = CardArtCatalog.LoadIcon(data);
		if (tex != null)
		{
			_iconRect.Texture = tex;
			_iconRect.Visible = true;
			_glyphLabel.Visible = false;
			return;
		}

		_iconRect.Texture = null;
		_iconRect.Visible = false;
		_glyphLabel.Visible = true;
		_glyphLabel.Text = string.IsNullOrEmpty(data.Name) ? "?" : data.Name.Substring(0, 1);
	}

	private void InitializeUI()
	{
		if (_initialized) return;
		_initialized = true;

		CustomMinimumSize = new Vector2(128, 112);
		SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		SizeFlagsVertical = SizeFlags.ShrinkBegin;
		MouseFilter = MouseFilterEnum.Stop;
		TextureFilter = CanvasItem.TextureFilterEnum.Linear;

		var panel = new Panel();
		panel.SetAnchorsPreset(LayoutPreset.FullRect);
		var frame = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.14f),
			BorderColor = new Color(0.42f, 0.48f, 0.55f, 0.65f),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6
		};
		frame.SetBorderWidthAll(1);
		panel.AddThemeStyleboxOverride("panel", frame);
		AddChild(panel);

		_fill = new ColorRect();
		_fill.SetAnchorsPreset(LayoutPreset.FullRect);
		_fill.OffsetLeft = 4;
		_fill.OffsetTop = 4;
		_fill.OffsetRight = -4;
		_fill.OffsetBottom = -4;
		AddChild(_fill);

		_typeAccent = new ColorRect
		{
			CustomMinimumSize = new Vector2(5, 0),
			Color = new Color(0.5f, 0.5f, 0.5f)
		};
		_typeAccent.SetAnchorsPreset(LayoutPreset.LeftWide);
		_typeAccent.OffsetLeft = 4;
		_typeAccent.OffsetRight = 9;
		_typeAccent.OffsetTop = 4;
		_typeAccent.OffsetBottom = -4;
		AddChild(_typeAccent);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 2);
		margin.AddChild(box);

		_artPanel = new PanelContainer();
		_artPanel.CustomMinimumSize = new Vector2(0, 52);
		_artPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var artBg = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.28f),
			CornerRadiusTopLeft = 5,
			CornerRadiusTopRight = 5,
			CornerRadiusBottomRight = 5,
			CornerRadiusBottomLeft = 5
		};
		artBg.SetBorderWidthAll(1);
		artBg.BorderColor = new Color(1, 1, 1, 0.08f);
		_artPanel.AddThemeStyleboxOverride("panel", artBg);
		box.AddChild(_artPanel);

		var artMargin = new MarginContainer();
		artMargin.AddThemeConstantOverride("margin_left", 4);
		artMargin.AddThemeConstantOverride("margin_right", 4);
		artMargin.AddThemeConstantOverride("margin_top", 4);
		artMargin.AddThemeConstantOverride("margin_bottom", 4);
		_artPanel.AddChild(artMargin);

		var artCenter = new CenterContainer();
		artCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		artCenter.SizeFlagsVertical = SizeFlags.ExpandFill;
		artMargin.AddChild(artCenter);

		var iconHost = new Control { CustomMinimumSize = new Vector2(44, 44) };
		artCenter.AddChild(iconHost);

		_glyphLabel = new Label();
		_glyphLabel.SetAnchorsPreset(LayoutPreset.FullRect);
		_glyphLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_glyphLabel.VerticalAlignment = VerticalAlignment.Center;
		_glyphLabel.AddThemeFontSizeOverride("font_size", 22);
		_glyphLabel.AddThemeColorOverride("font_color", Colors.White);
		iconHost.AddChild(_glyphLabel);

		_iconRect = new TextureRect();
		_iconRect.SetAnchorsPreset(LayoutPreset.FullRect);
		_iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_iconRect.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		_iconRect.Visible = false;
		iconHost.AddChild(_iconRect);

		var nameBar = new PanelContainer();
		nameBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var nameBg = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.42f),
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomRight = 3,
			CornerRadiusBottomLeft = 3
		};
		nameBar.AddThemeStyleboxOverride("panel", nameBg);
		box.AddChild(nameBar);

		var nameMargin = new MarginContainer();
		nameMargin.AddThemeConstantOverride("margin_left", 4);
		nameMargin.AddThemeConstantOverride("margin_right", 4);
		nameMargin.AddThemeConstantOverride("margin_top", 2);
		nameMargin.AddThemeConstantOverride("margin_bottom", 2);
		nameBar.AddChild(nameMargin);

		_nameLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MaxLinesVisible = 2,
			ClipText = true
		};
		_nameLabel.AddThemeFontSizeOverride("font_size", 11);
		_nameLabel.AddThemeColorOverride("font_color", Colors.White);
		nameMargin.AddChild(_nameLabel);

		_typeLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_typeLabel.AddThemeFontSizeOverride("font_size", 9);
		_typeLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.88f, 0.88f));
		box.AddChild(_typeLabel);

		_attrLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_attrLabel.AddThemeFontSizeOverride("font_size", 9);
		_attrLabel.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.45f));
		box.AddChild(_attrLabel);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left) return;

		if (mb.Pressed)
		{
			_mousePressedOnNode = true;
			_dragStart = GetGlobalMousePosition();
			_wasDragged = false;
			if (Data.IsDraggable)
				StartDrag();
		}
		else if (_isDragging)
		{
			_mousePressedOnNode = false;
			EndDrag();
		}
		else if (_mousePressedOnNode)
		{
			_mousePressedOnNode = false;
			EmitSignal(SignalName.OnCardClicked, this);
		}
		else
		{
			_mousePressedOnNode = false;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isDragging) return;
		if (@event is InputEventMouseMotion motion)
		{
			GlobalPosition = motion.GlobalPosition - _dragOffset;
			if ((motion.GlobalPosition - _dragStart).Length() > 15f)
				_wasDragged = true;
		}
		else if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
		{
			EndDrag();
		}
	}

	private void StartDrag()
	{
		_isDragging = true;
		_dragOffset = GetGlobalMousePosition() - GlobalPosition;
		var tree = GetTree();
		var oldParent = GetParent();
		oldParent?.RemoveChild(this);

		var layer = new CanvasLayer { Name = "_DragLayer", Layer = 100 };
		tree?.Root.AddChild(layer);
		layer.AddChild(this);
		GlobalPosition = GetGlobalMousePosition() - _dragOffset;
	}

	private void EndDrag()
	{
		_isDragging = false;
		_dropGlobalPos = GlobalPosition;
		_mousePressedOnNode = false;
		if (_wasDragged)
			EmitSignal(SignalName.OnCardDragEnded, this);
		else
			EmitSignal(SignalName.OnCardClicked, this);

		var parent = GetParent();
		parent?.RemoveChild(this);
		if (parent?.Name == "_DragLayer")
			parent.QueueFree();
		QueueFree();
	}

	public bool IsOverArea(Control area) =>
		new Rect2(_dropGlobalPos, Size).Intersects(area.GetGlobalRect());

	public CardNode? GetOverlappingCard(Control container) =>
		FindCardNode(container, new Rect2(_dropGlobalPos, Size));

	private CardNode? FindCardNode(Node node, Rect2 myRect)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is CardNode card && card != this && myRect.Intersects(new Rect2(card.GlobalPosition, card.Size)))
				return card;
			var nested = FindCardNode(child, myRect);
			if (nested != null)
				return nested;
		}
		return null;
	}

	private static string GetTypeText(CardType type) => I18n.CardTypeName(type);

	private static string GetAttrText(CardData data)
	{
		if (data.Durability > 0) return I18n.Tf("card.hint.durability_fmt", data.Durability);
		if (data.FoodValue > 0) return I18n.Tf("card.hint.food_fmt", data.FoodValue);
		if (data.HealValue > 0) return I18n.Tf("card.hint.heal_fmt", data.HealValue);
		if (data.ThirstValue > 0) return I18n.Tf("card.hint.thirst_fmt", data.ThirstValue);
		return "";
	}

	private static Color GetTypeColor(CardType type) => type switch
	{
		CardType.Resource => new Color(0.42f, 0.33f, 0.2f),
		CardType.Creature => new Color(0.25f, 0.45f, 0.24f),
		CardType.Tool => new Color(0.32f, 0.34f, 0.55f),
		CardType.Weapon => new Color(0.45f, 0.28f, 0.32f),
		CardType.Building => new Color(0.48f, 0.38f, 0.23f),
		CardType.Status => new Color(0.55f, 0.2f, 0.2f),
		CardType.Event => new Color(0.2f, 0.25f, 0.5f),
		CardType.Location => new Color(0.2f, 0.43f, 0.34f),
		CardType.Container => new Color(0.35f, 0.4f, 0.45f),
		CardType.Seed => new Color(0.32f, 0.48f, 0.3f),
		_ => new Color(0.3f, 0.3f, 0.3f)
	};
}
