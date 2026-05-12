// scripts/ui/MainMenu.cs
using Godot;

namespace CardSurvival.UI;

public partial class MainMenu : Control
{
    private Button _startButton = null!;
    private Button _continueButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("MainContainer/StartButton");
        _continueButton = GetNode<Button>("MainContainer/ContinueButton");
        _settingsButton = GetNode<Button>("MainContainer/SettingsButton");
        _quitButton = GetNode<Button>("MainContainer/QuitButton");

        _startButton.Pressed += OnStartPressed;
        _continueButton.Pressed += OnContinuePressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _quitButton.Pressed += OnQuitPressed;

        var saveSystem = GetNode<SaveSystem>("/root/SaveSystem");
        _continueButton.Disabled = !saveSystem.HasSaveFile();
    }

    private void OnStartPressed()
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
