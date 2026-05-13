using System.Linq;
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class MapPopup : Control
{
	public event Action<string>? OnMoveToLocation;
	public event Action? OnClose;

	private VBoxContainer _list = null!;
	private Label _titleLabel = null!;
	private Button _closeButton = null!;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		BuildUI();
		I18n.LocaleChanged += ApplyChrome;
		ApplyChrome();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyChrome;
		base._ExitTree();
	}

	private void ApplyChrome()
	{
		_titleLabel.Text = I18n.T("ui.map_title");
		_closeButton.Text = I18n.T("craft.close");
	}

	private void BuildUI()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		shell.CustomMinimumSize = new Vector2(500, 430);
		GameTheme.ApplyModalPanel(shell);
		center.AddChild(shell);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		shell.AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		margin.AddChild(box);

		var titleRow = new HBoxContainer();
		box.AddChild(titleRow);

		_titleLabel = new Label();
		_titleLabel.AddThemeFontSizeOverride("font_size", 16);
		_titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleRow.AddChild(_titleLabel);

		_closeButton = new Button { CustomMinimumSize = new Vector2(70, 28) };
		GameTheme.StyleSidebarButton(_closeButton);
		_closeButton.Pressed += () => OnClose?.Invoke();
		titleRow.AddChild(_closeButton);

		var scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			ClipContents = true
		};
		box.AddChild(scroll);

		_list = new VBoxContainer();
		_list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(_list);
	}

	public void Refresh(string currentLocationId, List<LocationData> allLocations)
	{
		var current = allLocations.FirstOrDefault(l => l.Id == currentLocationId);
		var adjacent = current?.Connections ?? new List<string>();

		foreach (var child in _list.GetChildren())
			child.QueueFree();

		foreach (var loc in allLocations)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			_list.AddChild(row);

			var info = new Label();
			info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			var suffix = loc.Id == currentLocationId
				? I18n.T("ui.map_suffix_current")
				: adjacent.Contains(loc.Id)
					? I18n.T("ui.map_suffix_adjacent")
					: I18n.T("ui.map_suffix_far");
			info.Text = $"{loc.Icon} {loc.Name}{suffix}\n{loc.Description}";
			row.AddChild(info);

			if (loc.Id != currentLocationId && adjacent.Contains(loc.Id))
			{
				var id = loc.Id;
				var move = new Button { Text = I18n.T("ui.map_go"), CustomMinimumSize = new Vector2(70, 30) };
				GameTheme.StyleMoveButton(move);
				move.Pressed += () => OnMoveToLocation?.Invoke(id);
				row.AddChild(move);
			}
			else
			{
				row.AddChild(new Control { CustomMinimumSize = new Vector2(70, 0) });
			}
		}
	}
}
