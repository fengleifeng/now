// scripts/ui/MainMenu.cs
using Godot;
using CardSurvival;

namespace CardSurvival.UI;

public partial class MainMenu : Control
{
	private Button _startButton = null!;
	private Button _traitModeButton = null!;
	private Button _continueButton = null!;
	private Button _settingsButton = null!;
	private Button _quitButton = null!;

	public override void _Ready()
	{
		GetNode<GameSettings>("/root/GameSettings").Reload();

		_startButton = GetNode<Button>("CenterRoot/MainContainer/StartButton");
		_traitModeButton = GetNode<Button>("CenterRoot/MainContainer/TraitModeButton");
		_continueButton = GetNode<Button>("CenterRoot/MainContainer/ContinueButton");
		_settingsButton = GetNode<Button>("CenterRoot/MainContainer/SettingsButton");
		_quitButton = GetNode<Button>("CenterRoot/MainContainer/QuitButton");

		_startButton.Pressed += OnStartPressed;
		_traitModeButton.Pressed += OnTraitModePressed;
		_continueButton.Pressed += OnContinuePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;

		var saveSystem = GetNode<SaveSystem>("/root/SaveSystem");
		_continueButton.Disabled = !saveSystem.HasSaveFile();
	}

	/// <summary>默认幸存者：不选特质，营养为 PlayerState 默认值，直接进入游戏。</summary>
	private void OnStartPressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").BeginNewGameWithDefaults();
		GetTree().ChangeSceneToFile("res://scenes/GameRoot.tscn");
	}

	/// <summary>可选一条特质；仍可不选即以默认营养开局。</summary>
	private void OnTraitModePressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").ResetState();
		GetTree().ChangeSceneToFile("res://scenes/CharacterSelect.tscn");
	}

	private void OnContinuePressed()
	{
		GetNode<PlayerSystem>("/root/PlayerSystem").RequestLoadSave();
		GetTree().ChangeSceneToFile("res://scenes/GameRoot.tscn");
	}

	private void OnSettingsPressed()
	{
		GD.Print("[MainMenu] Settings clicked");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
