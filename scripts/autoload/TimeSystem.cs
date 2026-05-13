using Godot;
using System;
using CardSurvival.Game;

namespace CardSurvival;

public enum TimeOfDay
{
    Morning,
    Noon,
    Evening,
    Night
}

public enum WeatherType
{
    Sunny,
    Rainy,
    Stormy,
    Snowy,
    Foggy
}

public partial class TimeSystem : Node
{
    [Signal] public delegate void OnTimeChangedEventHandler(float dayProgress, int currentDay, int timeOfDay);
    [Signal] public delegate void OnDayChangedEventHandler(int day);
    [Signal] public delegate void OnWeatherChangedEventHandler(int weather);
    [Signal] public delegate void OnSeasonChangedEventHandler(string season);

    // === 时间 ===
    public float DayProgress { get; set; } = 0f;
    public int CurrentDay { get; set; } = 1;
    public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;

    /// <summary>从存档恢复时间与天气。</summary>
    public void ApplyLoadedTimeAndWeather(float dayProgress, int currentDay, string season, WeatherType weather)
    {
        DayProgress = dayProgress;
        CurrentDay = currentDay;
        CurrentSeason = season;
        CurrentWeather = weather;
    }

    // === 季节（新增） ===
    public string CurrentSeason { get; set; } = "spring";
    private static int DaysPerSeason => SurviveTime.DaysPerSeason;
    private readonly string[] _seasons = { "spring", "summer", "autumn", "winter" };

    // === 温度基准（季节相关） ===
    public int BaseTemperature => CurrentSeason switch
    {
        "spring" => 15,
        "summer" => 30,
        "autumn" => 10,
        "winter" => -5,
        _ => 15
    };

    private readonly Random _random = new();
    private TimeOfDay _lastTimeOfDay = TimeOfDay.Morning;
    private float _lastEmittedProgress = 0f;
    private string _lastSeason = "spring";
    private PlayerSystem? _playerSystem;

    private PlayerSystem GetPlayerSystem()
    {
        if (_playerSystem == null)
            _playerSystem = GetNode<PlayerSystem>("/root/PlayerSystem");
        return _playerSystem;
    }

    public void AdvanceTime(float amount)
    {
        var oldTimeOfDay = GetTimeOfDay();
        var oldDay = CurrentDay;
        var oldSeason = CurrentSeason;

        DayProgress += amount;

        // 进位到下一天
        while (DayProgress >= 1.0f)
        {
            DayProgress -= 1.0f;
            CurrentDay++;
            RollWeather();

            // 季节变更检查
            var newSeasonIndex = ((CurrentDay - 1) / DaysPerSeason) % 4;
            CurrentSeason = _seasons[newSeasonIndex];

            // 季节变更时调整温度
            if (CurrentSeason != oldSeason)
            {
                EmitSignal(SignalName.OnSeasonChanged, CurrentSeason);
                GD.Print($"[TimeSystem] Season changed to {CurrentSeason}");
            }

            // 每天结算玩家状态消耗
            var playerSystem = GetPlayerSystem();
            playerSystem.ConsumeHunger(10);
            playerSystem.ConsumeThirst(12);
            playerSystem.TickNutritionDaily();

            // 温度影响
            ApplyTemperatureEffect();
        }

        // 时段内消耗
        var playerSystem2 = GetPlayerSystem();
        if (oldTimeOfDay == TimeOfDay.Night)
        {
            // 夜晚精神下降
            playerSystem2.UpdateSanity(-(int)(3 * playerSystem2.State.SanityDrainRate));
            // 夜晚精力恢复更快（睡眠）
            playerSystem2.RestoreEnergy(5);
        }

        // 时间推进信号
        var newTimeOfDay = GetTimeOfDay();

        var progressChanged = MathF.Abs(DayProgress - _lastEmittedProgress) >= 0.01f;
        var timeChanged = newTimeOfDay != _lastTimeOfDay;

        if (progressChanged || timeChanged)
        {
            _lastEmittedProgress = DayProgress;
            _lastTimeOfDay = newTimeOfDay;
            EmitSignal(SignalName.OnTimeChanged, DayProgress, CurrentDay, (int)newTimeOfDay);
        }

        if (CurrentDay != oldDay)
        {
            EmitSignal(SignalName.OnDayChanged, CurrentDay);

            // 每天结算效果系统
            var effects = GetNodeOrNull<EffectSystem>("/root/EffectSystem");
            effects?.TickEffects();
        }
    }

    private void ApplyTemperatureEffect()
    {
        var player = GetPlayerSystem();
        var tempDiff = player.State.Temperature - BaseTemperature;

        // 温度偏离基准时，缓慢向基准调整
        if (tempDiff > 5)
            player.UpdateTemperature(-1);
        else if (tempDiff < -5)
            player.UpdateTemperature(1);

        // 极寒/极热效果
        if (BaseTemperature < 0 && player.State.Temperature < 5)
        {
            player.TakeDamage(3); // 冻伤
            player.ConsumeHunger(5); // 发抖消耗能量
        }
        if (BaseTemperature > 25 && player.State.Temperature > 35)
        {
            player.TakeDamage(2); // 中暑
            player.ConsumeThirst(8); // 口渴加速
        }
    }

    public TimeOfDay GetTimeOfDay()
    {
        if (DayProgress < 0.25f) return TimeOfDay.Morning;
        if (DayProgress < 0.5f) return TimeOfDay.Noon;
        if (DayProgress < 0.75f) return TimeOfDay.Evening;
        return TimeOfDay.Night;
    }

    public float GetEnergyRestoreRate()
    {
        // 白天恢复快，夜晚恢复慢
        return GetTimeOfDay() switch
        {
            TimeOfDay.Night => 0.5f,
            TimeOfDay.Morning => 1.2f,
            TimeOfDay.Noon => 1.5f,
            TimeOfDay.Evening => 0.8f,
            _ => 1.0f
        };
    }

    private void RollWeather()
    {
        var rand = _random.NextDouble();

        // 季节影响天气概率
        var (sunnyChance, rainChance, stormChance, fogChance, snowChance) = CurrentSeason switch
        {
            "spring" => (0.30, 0.35, 0.10, 0.20, 0.05),
            "summer" => (0.50, 0.20, 0.20, 0.05, 0.05),
            "autumn" => (0.25, 0.30, 0.15, 0.25, 0.05),
            "winter" => (0.15, 0.10, 0.15, 0.15, 0.45),
            _ => (0.40, 0.25, 0.15, 0.15, 0.05)
        };

        if (rand < sunnyChance)
            CurrentWeather = WeatherType.Sunny;
        else if (rand < sunnyChance + rainChance)
            CurrentWeather = WeatherType.Rainy;
        else if (rand < sunnyChance + rainChance + stormChance)
            CurrentWeather = WeatherType.Stormy;
        else if (rand < sunnyChance + rainChance + stormChance + fogChance)
            CurrentWeather = WeatherType.Foggy;
        else
            CurrentWeather = WeatherType.Snowy;

        EmitSignal(SignalName.OnWeatherChanged, (int)CurrentWeather);
    }

    public bool IsNight()
    {
        return GetTimeOfDay() == TimeOfDay.Night;
    }

    public float GetNightModifier()
    {
        return IsNight() ? 2.0f : 1.0f;
    }

    public string GetTimeDisplay()
    {
        var hour = (int)(DayProgress * 24);
        var minute = (int)((DayProgress * 24 - hour) * 60);
        return $"{hour:D2}:{minute:D2}";
    }

    /// <summary>用于 <c>I18n</c> 的词条键，如 <c>time.season.spring</c>。</summary>
    public string GetSeasonMessageKey() =>
        CurrentSeason switch
        {
            "spring" => "time.season.spring",
            "summer" => "time.season.summer",
            "autumn" => "time.season.autumn",
            "winter" => "time.season.winter",
            _ => "time.season.unknown"
        };

    /// <summary>用于 <c>I18n</c> 的词条键，如 <c>time.weather.sunny</c>。</summary>
    public string GetWeatherMessageKey() =>
        CurrentWeather switch
        {
            WeatherType.Sunny => "time.weather.sunny",
            WeatherType.Rainy => "time.weather.rainy",
            WeatherType.Stormy => "time.weather.stormy",
            WeatherType.Snowy => "time.weather.snowy",
            WeatherType.Foggy => "time.weather.foggy",
            _ => "time.weather.unknown"
        };
}
