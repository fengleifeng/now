// scripts/ui/StatusPanel.cs
using Godot;

namespace CardSurvival.UI;

public partial class StatusPanel : HBoxContainer
{
    private ProgressBar _hpBar = null!;
    private ProgressBar _hungerBar = null!;
    private Label _hpLabel = null!;
    private Label _hungerLabel = null!;
    private PlayerSystem _player = null!;

    public override void _Ready()
    {
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 16);

        _player = GetNode<PlayerSystem>("/root/PlayerSystem");

        var hpGroup = new VBoxContainer();
        AddChild(hpGroup);

        _hpLabel = new Label();
        _hpLabel.Text = "HP";
        _hpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        hpGroup.AddChild(_hpLabel);

        _hpBar = new ProgressBar();
        _hpBar.MinValue = 0;
        _hpBar.MaxValue = _player.State.MaxHealth;
        _hpBar.CustomMinimumSize = new Vector2(140, 22);
        hpGroup.AddChild(_hpBar);

        var hungerGroup = new VBoxContainer();
        AddChild(hungerGroup);

        _hungerLabel = new Label();
        _hungerLabel.Text = "Hunger";
        _hungerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        hungerGroup.AddChild(_hungerLabel);

        _hungerBar = new ProgressBar();
        _hungerBar.MinValue = 0;
        _hungerBar.MaxValue = _player.State.MaxHunger;
        _hungerBar.CustomMinimumSize = new Vector2(140, 22);
        hungerGroup.AddChild(_hungerBar);

        _player.OnPlayerDamaged += OnDamaged;
        _player.OnPlayerHealed += OnHealed;
    }

    public override void _Process(double delta)
    {
        _hpBar.Value = _player.State.Health;
        _hungerBar.Value = _player.State.Hunger;
        _hpLabel.Text = $"HP {_player.State.Health}/{_player.State.MaxHealth}";
        _hungerLabel.Text = $"Hunger {_player.State.Hunger}/{_player.State.MaxHunger}";
    }

    private void OnDamaged(int amount)
    {
        GD.Print($"[StatusPanel] Player damaged: {amount}");
    }

    private void OnHealed(int amount)
    {
        GD.Print($"[StatusPanel] Player healed: {amount}");
    }
}
