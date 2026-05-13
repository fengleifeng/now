using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

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
	private bool _initialized;
	private bool _isDragging;
	private bool _wasDragged;
	private bool _mousePressedOnNode;
	private Vector2 _dragOffset;
	private Vector2 _dragStart;
	private Vector2 _dropGlobalPos;

	public override void _Ready()
	{
		InitializeUI();
	}

	public void Setup(CardData data)
	{
		InitializeUI();
		Data = data;
		_nameLabel.Text = data.Stack > 1 ? data.Name + " x" + data.Stack.ToString() : data.Name;
		_typeLabel.Text = GetTypeText(data.Type);
		_attrLabel.Text = GetAttrText(data);
		var typeColor = GetTypeColor(data.Type);
		_fill.Color = typeColor;
		_typeAccent.Color = typeColor.Lightened(0.12f);
		TooltipText = data.Description;
		MouseDefaultCursorShape = data.IsDraggable ? CursorShape.PointingHand : CursorShape.Arrow;

		var glyph = string.IsNullOrEmpty(data.Name) ? "?" : data.Name.Substring(0, 1);
		_glyphLabel.Text = glyph;

		Texture2D? tex = null;
		if (!string.IsNullOrWhiteSpace(data.IconPath))
			tex = ResourceLoader.Load<Texture2D>(data.IconPath);
		if (tex != null)
		{
			_iconRect.Texture = tex;
			_iconRect.Visible = true;
			_glyphLabel.Visible = false;
		}
		else
		{
			_iconRect.Texture = null;
			_iconRect.Visible = false;
			_glyphLabel.Visible = true;
		}
	}

	private void InitializeUI()
	{
		if (_initialized) return;
		_initialized = true;

		CustomMinimumSize = new Vector2(128, 100);
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

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 3);
		margin.AddChild(box);

		var artRow = new HBoxContainer();
		artRow.AddThemeConstantOverride("separation", 6);
		artRow.CustomMinimumSize = new Vector2(0, 36);
		artRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(artRow);

		_typeAccent = new ColorRect
		{
			CustomMinimumSize = new Vector2(5, 0),
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		artRow.AddChild(_typeAccent);

		var artCenter = new CenterContainer();
		artCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		artCenter.SizeFlagsVertical = SizeFlags.ExpandFill;
		artRow.AddChild(artCenter);

		var artSlot = new PanelContainer();
		artSlot.CustomMinimumSize = new Vector2(40, 34);
		var slotBox = new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0.35f),
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomRight = 4,
			CornerRadiusBottomLeft = 4
		};
		slotBox.SetBorderWidthAll(1);
		slotBox.BorderColor = new Color(1, 1, 1, 0.12f);
		artSlot.AddThemeStyleboxOverride("panel", slotBox);
		artCenter.AddChild(artSlot);

		var slotInner = new MarginContainer();
		slotInner.AddThemeConstantOverride("margin_left", 2);
		slotInner.AddThemeConstantOverride("margin_right", 2);
		slotInner.AddThemeConstantOverride("margin_top", 2);
		slotInner.AddThemeConstantOverride("margin_bottom", 2);
		artSlot.AddChild(slotInner);

		var stack = new Control();
		stack.CustomMinimumSize = new Vector2(32, 28);
		slotInner.AddChild(stack);

		_glyphLabel = new Label();
		_glyphLabel.SetAnchorsPreset(LayoutPreset.FullRect);
		_glyphLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_glyphLabel.VerticalAlignment = VerticalAlignment.Center;
		_glyphLabel.AddThemeFontSizeOverride("font_size", 18);
		_glyphLabel.AddThemeColorOverride("font_color", Colors.White);
		stack.AddChild(_glyphLabel);

		_iconRect = new TextureRect();
		_iconRect.SetAnchorsPreset(LayoutPreset.FullRect);
		_iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_iconRect.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		_iconRect.Visible = false;
		stack.AddChild(_iconRect);

		_nameLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_nameLabel.AddThemeFontSizeOverride("font_size", 12);
		_nameLabel.AddThemeColorOverride("font_color", Colors.White);
		_nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(_nameLabel);

		_typeLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_typeLabel.AddThemeFontSizeOverride("font_size", 10);
		_typeLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.88f, 0.88f));
		box.AddChild(_typeLabel);

		_attrLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_attrLabel.AddThemeFontSizeOverride("font_size", 10);
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

	public bool IsOverArea(Control area)
	{
		return new Rect2(_dropGlobalPos, Size).Intersects(area.GetGlobalRect());
	}

	public CardNode? GetOverlappingCard(Control container)
	{
		return FindCardNode(container, new Rect2(_dropGlobalPos, Size));
	}

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

	private static string GetTypeText(CardType type) => type switch
	{
		CardType.Resource => "资源",
		CardType.Creature => "生物",
		CardType.Tool => "工具",
		CardType.Weapon => "武器",
		CardType.Building => "建筑",
		CardType.Status => "状态",
		CardType.Event => "事件",
		CardType.Location => "地点",
		CardType.Container => "容器",
		CardType.Seed => "种子",
		_ => "卡牌"
	};

	private static string GetAttrText(CardData data)
	{
		if (data.Durability > 0) return $"耐久 {data.Durability}";
		if (data.FoodValue > 0) return $"饱食 +{data.FoodValue}";
		if (data.HealValue > 0) return $"治疗 +{data.HealValue}";
		if (data.ThirstValue > 0) return $"解渴 +{data.ThirstValue}";
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
