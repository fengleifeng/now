// scripts/data/PlayerState.cs
namespace CardSurvival.Data;

public class PlayerState
{
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Hunger { get; set; } = 100;
    public int MaxHunger { get; set; } = 100;
    public float DayProgress { get; set; } = 0f;
}
