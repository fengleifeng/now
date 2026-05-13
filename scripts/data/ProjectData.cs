// scripts/data/ProjectData.cs
namespace CardSurvival.Data;

public class ProjectData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string MaterialId { get; set; } = "";
    public int RequiredSteps { get; set; } = 1;
    public string ResultCardId { get; set; } = "";
    public string UnlockRecipe { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>每步建造消耗时间（游戏分钟）；0 表示使用全局 DefaultActionMinutes.BuildProjectStep。</summary>
    public int BuildStepMinutes { get; set; }
}

public class ProjectState
{
    public string Id { get; set; } = "";
    public int Progress { get; set; } = 0;
    public int Required { get; set; } = 1;
}
