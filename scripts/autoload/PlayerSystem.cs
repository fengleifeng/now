// scripts/autoload/PlayerSystem.cs
using Godot;
using CardSurvival.Data;

namespace CardSurvival;

public partial class PlayerSystem : Node
{
    [Signal] public delegate void OnPlayerDamagedEventHandler(int amount);
    [Signal] public delegate void OnPlayerHealedEventHandler(int amount);
    [Signal] public delegate void OnPlayerDeathEventHandler();

    public PlayerState State { get; private set; } = new();

    public override void _Ready()
    {
        GD.Print($"[PlayerSystem] HP:{State.Health} Hunger:{State.Hunger}");
    }

    public void TakeDamage(int amount)
    {
        State.Health = Math.Max(0, State.Health - amount);
        EmitSignal(SignalName.OnPlayerDamaged, amount);
        if (State.Health <= 0)
            EmitSignal(SignalName.OnPlayerDeath);
    }

    public void Heal(int amount)
    {
        State.Health = Math.Min(State.MaxHealth, State.Health + amount);
        EmitSignal(SignalName.OnPlayerHealed, amount);
    }

    public void ConsumeHunger(int amount)
    {
        State.Hunger = Math.Max(0, State.Hunger - amount);
        if (State.Hunger <= 0)
            TakeDamage(10);
    }

    public void Eat(int amount)
    {
        State.Hunger = Math.Min(State.MaxHunger, State.Hunger + amount);
    }

    public bool IsDead() => State.Health <= 0;
}
