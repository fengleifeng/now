using System.Linq;
using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

/// <summary>手牌「待合成」占位卡：展示步骤与材料，确认合成或取消暂存。</summary>
public partial class StagedCombinePopup : Control
{
	public event Action? OnSynthesize;
	public event Action? OnCancelStage;
	public event Action? OnClose;

	private CombineRule _rule = null!;
	private CardManager _cards = null!;
	private Label _title = null!;
	private Label _step = null!;
	private Label _mat = null!;
	private Label _res = null!;
	private Label _hint = null!;
	private Button _synth = null!;
	private Button _cancel = null!;
	private Button _close = null!;

	public void Setup(CombineRule rule, CardManager cards)
	{
		_rule = rule;
		_cards = cards;
	}

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		BuildUi();
		I18n.LocaleChanged += ApplyTexts;
		ApplyTexts();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyTexts;
		base._ExitTree();
	}

	private void ApplyTexts()
	{
		_title.Text = I18n.T("staged.popup_title");
		_step.Text = I18n.T("staged.step");
		_mat.Text = I18n.Tf("staged.materials_fmt", DescribeMaterials());
		_res.Text = _rule.Results.Count == 0
			? I18n.T("staged.output_eliminate")
			: I18n.Tf("staged.output_fmt", DescribeResults());
		_hint.Text = I18n.T("staged.hint_permanent");
		_synth.Text = I18n.T("staged.synthesize");
		_cancel.Text = I18n.T("staged.cancel_stage");
		_close.Text = I18n.T("staged.close");
	}

	private void BuildUi()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		shell.CustomMinimumSize = new Vector2(420, 360);
		GameTheme.ApplyModalPanel(shell);
		center.AddChild(shell);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		shell.AddChild(margin);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 10);
		margin.AddChild(box);

		_title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_title.AddThemeFontSizeOverride("font_size", 17);
		_title.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		box.AddChild(_title);

		_step = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_step.AddThemeFontSizeOverride("font_size", 13);
		_step.AddThemeColorOverride("font_color", GameTheme.AccentHand);
		box.AddChild(_step);

		_mat = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_mat.AddThemeFontSizeOverride("font_size", 13);
		_mat.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		_mat.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(_mat);

		_res = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_res.AddThemeFontSizeOverride("font_size", 13);
		_res.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		_res.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(_res);

		_hint = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_hint.AddThemeFontSizeOverride("font_size", 11);
		_hint.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		_hint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(_hint);

		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 10);
		box.AddChild(row);

		_synth = new Button { CustomMinimumSize = new Vector2(100, 34) };
		GameTheme.StyleSidebarButton(_synth);
		_synth.Pressed += () => OnSynthesize?.Invoke();
		row.AddChild(_synth);

		_cancel = new Button { CustomMinimumSize = new Vector2(100, 34) };
		GameTheme.StyleSidebarButton(_cancel);
		_cancel.Pressed += () => OnCancelStage?.Invoke();
		row.AddChild(_cancel);

		_close = new Button { CustomMinimumSize = new Vector2(88, 30) };
		_close.Pressed += () => OnClose?.Invoke();
		box.AddChild(_close);
	}

	private string DescribeMaterials()
	{
		if (_rule.Ingredients.Count > 0)
			return string.Join(" + ", _rule.Ingredients.Select(id => _cards.GetCard(id)?.Name ?? id));
		if (_rule.MatchByTag)
			return I18n.Tf("staged.tag_pair_fmt", _rule.CardA, _rule.CardB);
		var na = _cards.GetCard(_rule.CardA)?.Name ?? _rule.CardA;
		var nb = _cards.GetCard(_rule.CardB)?.Name ?? _rule.CardB;
		return $"{na} + {nb}";
	}

	private string DescribeResults() =>
		string.Join(" + ", _rule.Results.Select(id => _cards.GetCard(id)?.Name ?? id));
}
