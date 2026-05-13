// scripts/ui/CharacterSelect.cs
using Godot;
using System.Collections.Generic;
using System.Linq;
using CardSurvival;

namespace CardSurvival.UI;

public enum CharacterTrait
{
	Strong,
	Agile,
	Wise,
	Survivalist,
	Healer,
	Hunter,
	Carpenter,
	Explorer
}

public partial class CharacterSelect : Control
{
	private Button _startButton = null!;
	private Button _backButton = null!;
	private Label _selectedLabel = null!;
	private Label _titleLabel = null!;
	private Label _descriptionLabel = null!;
	private readonly List<Button> _traitButtons = new();
	private CharacterTrait? _selectedTrait;

	private readonly Dictionary<string, CharacterTrait> _traitMap = new()
	{
		{"Trait1", CharacterTrait.Strong},
		{"Trait2", CharacterTrait.Agile},
		{"Trait3", CharacterTrait.Wise},
		{"Trait4", CharacterTrait.Survivalist},
		{"Trait5", CharacterTrait.Healer},
		{"Trait6", CharacterTrait.Hunter},
		{"Trait7", CharacterTrait.Carpenter},
		{"Trait8", CharacterTrait.Explorer}
	};

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("CenterRoot/MenuPanel/MainContainer/TitleLabel");
		_descriptionLabel = GetNode<Label>("CenterRoot/MenuPanel/MainContainer/DescriptionLabel");
		_startButton = GetNode<Button>("CenterRoot/MenuPanel/MainContainer/StartButton");
		_backButton = GetNode<Button>("CenterRoot/MenuPanel/MainContainer/BackButton");
		_selectedLabel = GetNode<Label>("CenterRoot/MenuPanel/MainContainer/SelectedLabel");

		var menuPanel = GetNode<PanelContainer>("CenterRoot/MenuPanel");
		GameTheme.ApplyModalPanel(menuPanel);
		const int pad = 18;
		menuPanel.AddThemeConstantOverride("margin_left", pad);
		menuPanel.AddThemeConstantOverride("margin_top", pad);
		menuPanel.AddThemeConstantOverride("margin_right", pad);
		menuPanel.AddThemeConstantOverride("margin_bottom", pad);

		var grid = GetNode<GridContainer>("CenterRoot/MenuPanel/MainContainer/TraitsGrid");
		foreach (var child in grid.GetChildren())
		{
			if (child is Button button)
			{
				GameTheme.StyleSidebarButton(button);
				_traitButtons.Add(button);
				var captured = button;
				button.Pressed += () => OnTraitPressed(captured);
			}
		}

		GameTheme.StyleSidebarButton(_startButton);
		GameTheme.StyleSidebarButton(_backButton);

		_startButton.Pressed += OnStartPressed;
		_backButton.Pressed += OnBackPressed;

		ApplyPageTexts();
		I18n.LocaleChanged += ApplyPageTexts;
		UpdateSelectedDisplay();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= ApplyPageTexts;
		base._ExitTree();
	}

	private void ApplyPageTexts()
	{
		_titleLabel.Text = I18n.T("character.page_title");
		_descriptionLabel.Text = I18n.T("character.page_desc");
		_startButton.Text = I18n.T("character.start");
		_backButton.Text = I18n.T("character.back");
		UpdateSelectedDisplay();
	}

	private void OnTraitPressed(Button button)
	{
		var trait = _traitMap[button.Name];

		if (_selectedTrait == trait)
		{
			_selectedTrait = null;
			button.Modulate = Godot.Colors.White;
		}
		else
		{
			if (_selectedTrait.HasValue)
			{
				var prevButton = _traitButtons.First(b => _traitMap[b.Name] == _selectedTrait.Value);
				prevButton.Modulate = Godot.Colors.White;
			}

			_selectedTrait = trait;
			button.Modulate = new Godot.Color(0.6f, 0.8f, 0.6f);
		}

		UpdateSelectedDisplay();
	}

	private void UpdateSelectedDisplay()
	{
		_selectedLabel.Text = _selectedTrait.HasValue
			? I18n.Tf("character.selected_fmt", TraitName(_selectedTrait.Value))
			: I18n.T("character.none_hint");
		_startButton.Disabled = false;
	}

	private static string TraitName(CharacterTrait t) =>
		I18n.T($"trait.{t.ToString().ToLowerInvariant()}");

	private void OnStartPressed()
	{
		var playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
		if (_selectedTrait.HasValue)
			playerSystem.SetTraits(new List<CharacterTrait> { _selectedTrait.Value });
		else
			playerSystem.SetTraits(new List<CharacterTrait>());

		GetTree().ChangeSceneToFile("res://scenes/GameRoot.tscn");
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}
}
