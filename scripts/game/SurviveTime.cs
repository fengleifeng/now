using System;

namespace CardSurvival.Game;

/// <summary>
/// 游戏内时间换算：1 日 = 24×60 游戏分钟；季节长度以「日」计。
/// </summary>
public static class SurviveTime
{
	public const int MinutesPerDay = 24 * 60;
	public const int DaysPerSeason = 30;

	public static float MinutesToDayFraction(float minutes) =>
		Math.Max(0f, minutes) / MinutesPerDay;
}
