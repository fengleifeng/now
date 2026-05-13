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

		var title = new Label { Text = "暂存合成", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 17);
		title.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		box.AddChild(title);

		var step = new Label { Text = "步骤 1 / 1", HorizontalAlignment = HorizontalAlignment.Center };
		step.AddThemeFontSizeOverride("font_size", 13);
		step.AddThemeColorOverride("font_color", GameTheme.AccentHand);
		box.AddChild(step);

		var mat = new Label
		{
			Text = $"材料：{DescribeMaterials()}",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		mat.AddThemeFontSizeOverride("font_size", 13);
		mat.AddThemeColorOverride("font_color", GameTheme.TextPrimary);
		mat.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(mat);

		var res = new Label
		{
			Text = _rule.Results.Count == 0 ? "产出：消除" : $"产出：{DescribeResults()}",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		res.AddThemeFontSizeOverride("font_size", 13);
		res.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		res.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(res);

		var hint = new Label
		{
			Text = "带「永久」标签或不可拖动的建筑会进入下方「固定」栏，并替换栏内上一张卡（旧卡落到场景）。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		hint.AddThemeFontSizeOverride("font_size", 11);
		hint.AddThemeColorOverride("font_color", GameTheme.TextMuted);
		hint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		box.AddChild(hint);

		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 10);
		box.AddChild(row);

		var synth = new Button { Text = "合成", CustomMinimumSize = new Vector2(100, 34) };
		GameTheme.StyleSidebarButton(synth);
		synth.Pressed += () => OnSynthesize?.Invoke();
		row.AddChild(synth);

		var cancel = new Button { Text = "取消暂存", CustomMinimumSize = new Vector2(100, 34) };
		GameTheme.StyleSidebarButton(cancel);
		cancel.Pressed += () => OnCancelStage?.Invoke();
		row.AddChild(cancel);

		var close = new Button { Text = "关闭", CustomMinimumSize = new Vector2(88, 30) };
		close.Pressed += () => OnClose?.Invoke();
		box.AddChild(close);
	}

	private string DescribeMaterials()
	{
		if (_rule.Ingredients.Count > 0)
			return string.Join(" + ", _rule.Ingredients.Select(id => _cards.GetCard(id)?.Name ?? id));
		if (_rule.MatchByTag)
			return $"{_rule.CardA} + {_rule.CardB}（标签匹配）";
		var na = _cards.GetCard(_rule.CardA)?.Name ?? _rule.CardA;
		var nb = _cards.GetCard(_rule.CardB)?.Name ?? _rule.CardB;
		return $"{na} + {nb}";
	}

	private string DescribeResults() =>
		string.Join(" + ", _rule.Results.Select(id => _cards.GetCard(id)?.Name ?? id));
}
