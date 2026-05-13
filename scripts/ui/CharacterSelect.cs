// scripts/ui/CharacterSelect.cs
using Godot;
using System.Collections.Generic;
using System.Linq;

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
		_startButton = GetNode<Button>("CenterRoot/MainContainer/StartButton");
		_backButton = GetNode<Button>("CenterRoot/MainContainer/BackButton");
		_selectedLabel = GetNode<Label>("CenterRoot/MainContainer/SelectedLabel");

		var grid = GetNode<GridContainer>("CenterRoot/MainContainer/TraitsGrid");
		foreach (var child in grid.GetChildren())
		{
			if (child is Button button)
			{
				_traitButtons.Add(button);
				var captured = button;
				button.Pressed += () => OnTraitPressed(captured);
			}
		}

		_startButton.Pressed += OnStartPressed;
		_backButton.Pressed += OnBackPressed;

		UpdateSelectedDisplay();
	}

	private void OnTraitPressed(Button button)
	{
		var trait = _traitMap[button.Name];

		if (_selectedTrait == trait)
		{
			// Deselect
			_selectedTrait = null;
			button.Modulate = Godot.Colors.White;
		}
		else
		{
			// Deselect previous if any
			if (_selectedTrait.HasValue)
			{
				var prevButton = _traitButtons.First(b => _traitMap[b.Name] == _selectedTrait.Value);
				prevButton.Modulate = Godot.Colors.White;
			}
			// Select new
			_selectedTrait = trait;
			button.Modulate = new Godot.Color(0.6f, 0.8f, 0.6f);
		}

		UpdateSelectedDisplay();
	}

	private void UpdateSelectedDisplay()
	{
		_selectedLabel.Text = _selectedTrait.HasValue
			? $"已选特质：{_selectedTrait.Value}（可直接开始）"
			: "未选特质：将以默认营养与无特质加成开局";
		_startButton.Disabled = false;
	}

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
