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
        ResetTraitDerivedNumericState();
        Traits = new List<CharacterTrait>(traits);
        ApplyTraitBonuses();
        EmitSignal(SignalName.OnStatsChanged);
    }

    /// <summary>新游戏默认幸存者：无特质，营养为 <see cref="PlayerState"/> 默认值。</summary>
    public void BeginNewGameWithDefaults()
    {
        ResetState();
    }

    /// <summary>每游戏日结算营养消耗。</summary>
    public void TickNutritionDaily()
    {
        State.Protein = Math.Max(0, State.Protein - 4);
        State.Vitamins = Math.Max(0, State.Vitamins - 3);
        State.Carbohydrate = Math.Max(0, State.Carbohydrate - 3);
        if (State.Protein < 25)
            UpdateImmunity(-3);
        if (State.Vitamins < 20)
            UpdateImmunity(-2);
        if (State.Carbohydrate > 75 && State.Hunger > 70)
            State.BodyFatIndex = Math.Min(100, State.BodyFatIndex + 1);
        else if (State.Protein > 60 && State.Hunger > 40)
            State.BodyFatIndex = Math.Max(8, State.BodyFatIndex - 1);
        EmitSignal(SignalName.OnStatsChanged);
    }

    /// <summary>进食后根据卡牌或标签启发增加营养。</summary>
    public void ApplyMealNutrients(CardData card)
    {
        var p = card.ProteinValue;
        var v = card.VitaminValue;
        var c = card.CarbValue;
        var bf = card.BodyFatDelta;
        if (p == 0 && v == 0 && c == 0 && bf == 0
            && (card.FoodValue > 0 || card.Tags.Contains(CardTag.Food)))
            NutrientHeuristic(card, ref p, ref v, ref c, ref bf);

        State.Protein = Math.Clamp(State.Protein + p, 0, 100);
        State.Vitamins = Math.Clamp(State.Vitamins + v, 0, 100);
        State.Carbohydrate = Math.Clamp(State.Carbohydrate + c, 0, 100);
        State.BodyFatIndex = Math.Clamp(State.BodyFatIndex + bf, 8, 95);
        EmitSignal(SignalName.OnStatsChanged);
    }

    public float GetBodyFatMoveMultiplier()
    {
        var x = Math.Clamp(State.BodyFatIndex - 22, 0, 45);
        return 1f + x * 0.0035f;
    }

    private static void NutrientHeuristic(CardData card, ref int p, ref int v, ref int c, ref int bf)
    {
        if (card.Tags.Contains(CardTag.Meat))
        {
            p += card.Tags.Contains(CardTag.Cooked) ? 18 : 12;
            v += 2;
            c += card.Tags.Contains(CardTag.Cooked) ? 8 : 0;
            bf += card.Tags.Contains(CardTag.Raw) ? 1 : 0;
        }
        else if (card.Tags.Contains(CardTag.Berry) || card.Tags.Contains(CardTag.Plant))
        {
            v += 10;
            c += 6;
            bf -= 1;
        }
        if (card.Tags.Contains(CardTag.Aquatic) && card.Tags.Contains(CardTag.Food))
        {
            p += 10;
            v += 3;
            c += 2;
        }
    }

    private void ResetTraitDerivedNumericState()
    {
        State.HungerRate = 1f;
        State.ThirstRate = 1f;
        State.CombineBonus = 0f;
        State.HealBonus = 0f;
        State.HuntBonus = 0f;
        State.BuildSpeed = 1f;
        State.DiscoverBonus = 0f;
        State.MoveEnergyCostMultiplier = 1f;
        State.MaxWeight = 50;
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

    public void IncrementLifetimeExplore()
    {
        State.Progression.IncrementExplore();
        EmitSignal(SignalName.OnStatsChanged);
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
