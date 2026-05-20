using Godot;
using System.Collections.Generic;
using System.Linq;
using CardSurvival.Core;
using CardSurvival.Domain;
using CardSurvival.UI.Menu;

namespace CardSurvival.UI;

/// <summary>
/// 特质选择场景：仅处理 UI 交互与跳转，特质枚举定义在 <see cref="CharacterTrait"/> 领域层。
/// </summary>
public partial class CharacterSelect : Control
{
	private GameServices _services = null!;
	private Button _startButton = null!;
	private Button _backButton = null!;
	private Label _selectedLabel = null!;
	private Label _titleLabel = null!;
	private Label _descriptionLabel = null!;
	private readonly List<Button> _traitButtons = new();
	private GridContainer _traitsGrid = null!;
	private CharacterTrait? _selectedTrait;

	/// <summary>绑定控件、样式、特质按钮与响应式布局。</summary>
	public override void _Ready()
	{
		_services = GameServices.From(this);
		var main = MenuScenePaths.ResolveCharacterMainVBox(this);
		_titleLabel = main.GetNode<Label>("TitleLabel");
		_descriptionLabel = main.GetNode<Label>("DescriptionLabel");
		_startButton = main.GetNode<Button>("StartButton");
		_backButton = main.GetNode<Button>("BackButton");
		_selectedLabel = main.GetNode<Label>("SelectedLabel");
		_traitsGrid = main.GetNode<GridContainer>("TraitsGrid");

		ApplyPanelChrome();
		WireTraitButtons();
		WireNavigationButtons();
		ApplyPageTexts();
		I18n.LocaleChanged += ApplyPageTexts;
		UpdateSelectedDisplay();
		UiLayout.BindResponsive(this, ApplyPageLayout);
	}

	/// <summary>取消国际化订阅。</summary>
	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyPageTexts;
		base._ExitTree();
	}

	/// <summary>应用面板主题与内边距。</summary>
	private void ApplyPanelChrome()
	{
		var menuPanel = MenuScenePaths.ResolveCharacterMenuPanel(this);
		GameTheme.ApplyModalPanel(menuPanel);
		const int pad = 18;
		menuPanel.AddThemeConstantOverride("margin_left", pad);
		menuPanel.AddThemeConstantOverride("margin_top", pad);
		menuPanel.AddThemeConstantOverride("margin_right", pad);
		menuPanel.AddThemeConstantOverride("margin_bottom", pad);
	}

	/// <summary>为特质网格内每个按钮绑定选中逻辑。</summary>
	private void WireTraitButtons()
	{
		foreach (var child in _traitsGrid.GetChildren())
		{
			if (child is not Button button)
				continue;
			GameTheme.StyleSidebarButton(button);
			_traitButtons.Add(button);
			var captured = button;
			button.Pressed += () => OnTraitPressed(captured);
		}
	}

	/// <summary>绑定开始与返回按钮。</summary>
	private void WireNavigationButtons()
	{
		GameTheme.StyleSidebarButton(_startButton);
		GameTheme.StyleSidebarButton(_backButton);
		_startButton.Pressed += OnStartPressed;
		_backButton.Pressed += OnBackPressed;
	}

	/// <summary>按视口调整面板宽度、网格列数与按钮高度。</summary>
	private void ApplyPageLayout()
	{
		var panel = MenuScenePaths.ResolveCharacterMenuPanel(this);
		var vp = UiLayout.ViewportSize(this);
		var w = Mathf.Min(UiLayout.Scaled(this, 560), vp.X - 32f);
		panel.CustomMinimumSize = new Vector2(Mathf.Max(260f, w), 0);
		UiLayout.ApplyTraitGrid(_traitsGrid, this);
		var traitH = UiLayout.Scaled(this, 88);
		foreach (var b in _traitButtons)
			b.CustomMinimumSize = new Vector2(0, traitH);
	}

	/// <summary>刷新页面静态文案。</summary>
	private void ApplyPageTexts()
	{
		_titleLabel.Text = I18n.T("character.page_title");
		_descriptionLabel.Text = I18n.T("character.page_desc");
		_startButton.Text = I18n.T("character.start");
		_backButton.Text = I18n.T("character.back");
		UpdateSelectedDisplay();
	}

	/// <summary>切换当前选中的特质（再次点击取消选择）。</summary>
	private void OnTraitPressed(Button button)
	{
		var trait = TraitSelectionMap.FromButtonName(button.Name);

		if (_selectedTrait == trait)
		{
			_selectedTrait = null;
			button.Modulate = Colors.White;
		}
		else
		{
			if (_selectedTrait.HasValue)
			{
				var prev = _traitButtons.First(b =>
					TraitSelectionMap.FromButtonName(b.Name) == _selectedTrait.Value);
				prev.Modulate = Colors.White;
			}

			_selectedTrait = trait;
			button.Modulate = new Color(0.6f, 0.8f, 0.6f);
		}

		UpdateSelectedDisplay();
	}

	/// <summary>更新“已选择”提示文案。</summary>
	private void UpdateSelectedDisplay()
	{
		_selectedLabel.Text = _selectedTrait.HasValue
			? I18n.Tf("character.selected_fmt", I18n.T(TraitSelectionMap.TraitDisplayKey(_selectedTrait.Value)))
			: I18n.T("character.none_hint");
		_startButton.Disabled = false;
	}

	/// <summary>将选中特质写入 PlayerSystem 并进入游戏。</summary>
	private void OnStartPressed()
	{
		if (_selectedTrait.HasValue)
			_services.Player.SetTraits(new List<CharacterTrait> { _selectedTrait.Value });
		else
			_services.Player.SetTraits(new List<CharacterTrait>());

		GetTree().ChangeSceneToFile(ScenePaths.GameRoot);
	}

	/// <summary>返回主菜单。</summary>
	private void OnBackPressed() =>
		GetTree().ChangeSceneToFile(ScenePaths.MainMenu);
}
