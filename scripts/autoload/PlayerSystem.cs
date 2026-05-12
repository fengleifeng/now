using Godot;
using CardSurvival.Data;
using CardSurvival.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardSurvival;

public partial class PlayerSystem : Node
{
    [Signal] public delegate void OnStatsChangedEventHandler();
    [Signal] public delegate void OnPlayerDeathEventHandler();
    [Signal] public delegate void OnLocationChangedEventHandler(string locationId);

    public PlayerState State { get; private set; } = new();
    public List<CharacterTrait> Traits { get; private set; } = new();
    public bool ShouldLoadSave { get; private set; }

    public override void _Ready()
    {
        GD.Print($"[PlayerSystem] HP:{State.Health} Hunger:{State.Hunger} Thirst:{State.Thirst}");
    }

    public void SetTraits(List<CharacterTrait> traits)
    {
        Traits = new List<CharacterTrait>(traits);
        ApplyTraitBonuses();
    }

    public void RequestLoadSave()
    {
        ShouldLoadSave = true;
    }

    public bool ConsumeLoadSaveRequest()
    {
        if (!ShouldLoadSave) return false;
        ShouldLoadSave = false;
        return true;
    }

    private void ApplyTraitBonuses()
    {
        foreach (var trait in Traits)
        {
            switch (trait)
            {
                case CharacterTrait.Strong:
                    State.MaxWeight = (int)(State.MaxWeight * 1.5f);
                    break;
                case CharacterTrait.Survivalist:
                    State.HungerRate = State.HungerRate * 0.8f;
                    State.ThirstRate = State.ThirstRate * 0.8f;
                    break;
                case CharacterTrait.Wise:
                    State.CombineBonus = 0.1f;
                    break;
                case CharacterTrait.Healer:
                    State.HealBonus = 0.3f;
                    break;
                case CharacterTrait.Hunter:
                    State.HuntBonus = 0.15f;
                    break;
                case CharacterTrait.Agile:
                    State.MoveEnergyCostMultiplier = 0.8f;
                    break;
                case CharacterTrait.Carpenter:
                    State.BuildSpeed = State.BuildSpeed * 1.2f;
                    break;
                case CharacterTrait.Explorer:
                    State.DiscoverBonus = 0.15f;
                    break;
            }
        }
    }

    // ===== 生命 =====

    public void TakeDamage(int amount)
    {
        if (State.Health <= 0) return;
        State.Health = Math.Max(0, State.Health - amount);
        EmitSignal(SignalName.OnStatsChanged);
        if (State.Health <= 0)
            EmitSignal(SignalName.OnPlayerDeath);
    }

    public void Heal(int amount)
    {
        var bonusAmount = (int)(amount * (1 + State.HealBonus));
        State.Health = Math.Min(State.MaxHealth, State.Health + bonusAmount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 饱食 =====

    public void Eat(int amount)
    {
        State.Hunger = Math.Min(State.MaxHunger, State.Hunger + amount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public void ConsumeHunger(int amount)
    {
        var hungerAmount = (int)(amount * State.HungerRate);
        State.Hunger = Math.Max(0, State.Hunger - hungerAmount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 口渴（新增） =====

    public void UpdateThirst(int amount)
    {
        State.Thirst = Math.Clamp(State.Thirst + amount, 0, State.MaxThirst);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public void Drink(int amount)
    {
        State.Thirst = Math.Min(State.MaxThirst, State.Thirst + amount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public void ConsumeThirst(int amount)
    {
        var thirstAmount = (int)(amount * State.ThirstRate);
        State.Thirst = Math.Max(0, State.Thirst - thirstAmount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 精力 =====

    public void ConsumeEnergy(int amount)
    {
        State.Energy = Math.Max(0, State.Energy - amount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public void RestoreEnergy(int amount)
    {
        State.Energy = Math.Min(State.MaxEnergy, State.Energy + amount);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 精神 =====

    public void UpdateSanity(int amount)
    {
        State.Sanity = Math.Clamp(State.Sanity + amount, 0, State.MaxSanity);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 免疫力（新增） =====

    public void UpdateImmunity(int amount)
    {
        State.Immunity = Math.Clamp(State.Immunity + amount, 0, State.MaxImmunity);
        EmitSignal(SignalName.OnStatsChanged);
    }

    // ===== 体温 =====

    public void UpdateTemperature(int delta)
    {
        State.Temperature = Math.Clamp(State.Temperature + delta, State.MinTemperature, State.MaxTemperature);
        EmitSignal(SignalName.OnStatsChanged);
        if (State.Temperature <= State.MinTemperature)
            TakeDamage(5);
    }

    // ===== 配方 =====

    public void AddLearnedRecipe(string recipeKey)
    {
        if (!State.LearnedRecipes.Contains(recipeKey))
        {
            State.LearnedRecipes.Add(recipeKey);
            EmitSignal(SignalName.OnStatsChanged);
        }
    }

    public bool IsRecipeLearned(string recipeKey)
    {
        return State.LearnedRecipes.Contains(recipeKey);
    }

    // ===== 项目 =====

    public void AddProject(ProjectData projectData)
    {
        if (State.ActiveProjects.Any(p => p.Id == projectData.Id)) return;
        State.ActiveProjects.Add(new ProjectState
        {
            Id = projectData.Id,
            Progress = 0,
            Required = projectData.RequiredSteps
        });
        EmitSignal(SignalName.OnStatsChanged);
    }

    public List<ProjectState> GetActiveProjects()
    {
        return State.ActiveProjects;
    }

    public bool BuildProject(string projectId)
    {
        var project = State.ActiveProjects.FirstOrDefault(p => p.Id == projectId);
        if (project == null) return false;
        project.Progress++;
        // 建造技能经验
        GainSkill("build");
        EmitSignal(SignalName.OnStatsChanged);
        return project.Progress >= project.Required;
    }

    public ProjectState? CompleteProject(string projectId)
    {
        var project = State.ActiveProjects.FirstOrDefault(p => p.Id == projectId);
        if (project == null || project.Progress < project.Required) return null;
        State.ActiveProjects.Remove(project);
        EmitSignal(SignalName.OnStatsChanged);
        return project;
    }

    public void AddCompletedBuilding(string buildingId)
    {
        if (!State.CompletedBuildings.Contains(buildingId))
        {
            State.CompletedBuildings.Add(buildingId);
            EmitSignal(SignalName.OnStatsChanged);
        }
    }

    public bool HasCompletedBuilding(string buildingId)
    {
        return State.CompletedBuildings.Contains(buildingId);
    }

    // ===== 技能系统（新增） =====

    public void GainSkill(string skillName)
    {
        switch (skillName)
        {
            case "gather": State.GatherLevel++; break;
            case "hunt": State.HuntLevel++; break;
            case "cook": State.CookLevel++; break;
            case "build": State.BuildLevel++; break;
            case "explore": State.ExploreLevel++; break;
            case "fight": State.FightLevel++; break;
        }
    }

    public int GetSkillLevel(string skillName) => skillName switch
    {
        "gather" => State.GatherLevel,
        "hunt" => State.HuntLevel,
        "cook" => State.CookLevel,
        "build" => State.BuildLevel,
        "explore" => State.ExploreLevel,
        "fight" => State.FightLevel,
        _ => 1
    };

    /// <summary>
    /// 根据技能等级返回加成倍率（每级 +10%）
    /// </summary>
    public float GetSkillBonus(string skillName)
    {
        return 1.0f + (GetSkillLevel(skillName) - 1) * 0.1f;
    }

    // ===== 工具方法 =====

    public bool IsDead() => State.Health <= 0;

    public void ResetState()
    {
        State = new PlayerState();
        Traits.Clear();
        ShouldLoadSave = false;
        EmitSignal(SignalName.OnStatsChanged);
    }
}
