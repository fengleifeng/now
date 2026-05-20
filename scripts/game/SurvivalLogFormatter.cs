namespace CardSurvival.Game;

/// <summary>
/// 将状态变化格式化为日志用字符串，让玩家清楚「行为带来了什么变化」。
/// </summary>
public static class SurvivalLogFormatter
{
	/// <summary>整数属性变化，如 +5 / -3。</summary>
	public static string SignedDelta(int delta) =>
		delta >= 0 ? $"+{delta}" : delta.ToString();

	/// <summary>带颜色的 BBCode 变化片段（绿增红减）。</summary>
	public static string ColoredDelta(int delta)
	{
		if (delta == 0)
			return "[color=gray]0[/color]";
		return delta > 0
			? $"[color=green]{SignedDelta(delta)}[/color]"
			: $"[color=red]{SignedDelta(delta)}[/color]";
	}
}
