using System;
using System.Collections.Generic;
using System.Linq;
using CardSurvival;
using CardSurvival.Data;
using CardSurvival.Game.HandActions;

namespace CardSurvival.Game;

/// <summary>
/// 《卡牌生存》式核心：所有生存/探索/移动/休息/仪式/合成结果落点逻辑集中在此类，
/// UI 层（GameRoot）只负责布局、弹窗与把日志写进 RichTextLabel。
/// </summary>
public sealed class CardSurvivalGame
{
	public sealed class Host
	{
		public required CardManager Cards { get; init; }
		public required PlayerSystem Player { get; init; }
		public required TimeSystem Time { get; init; }
		public required MapSystem Map { get; init; }
		public required CombineSystem Combine { get; init; }
		public required EffectSystem Effects { get; init; }
		public required SaveSystem Save { get; init; }
		public required GameSettings Settings { get; init; }
		public required Action<string> Log { get; init; }
		public required Action RequestUiRefresh { get; init; }
		public required Action RefreshCraftIfOpen { get; init; }
		public Random Rng { get; init; } = new();
	}

	private readonly Host _h;
	private List<CombineRule>? _rulesCache;

	public CardSurvivalGame(Host host) => _h = host;

	private void CardOpTime(string key) => _h.Settings.AdvanceTimeForActionMinutes(_h.Time, key);

	private void CardOpTime(string key, CardData card) => _h.Settings.AdvanceTimeForActionMinutes(_h.Time, key, card);

	public void AdvanceCombineFailTime()
	{
		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction(_h.Settings.GetDefaultActionMinutes("CombineFail")));
	}

	public void NewGame()
	{
		_h.Save.BeginNewPlaySession();
		_h.Cards.ClearRuntimeCards();
		_h.Map.ResetState();
		_h.Effects.ClearAll();
		_h.Time.CurrentDay = 1;
		_h.Time.DayProgress = 0f;
		_h.Time.CurrentSeason = "spring";
		_h.Player.State.CurrentLocation = _h.Map.CurrentLocation;
		EnsureAvailableProjects();
		_h.Cards.DrawInitialHand(4);
		_h.RequestUiRefresh();
		_h.Log("[color=green]卡牌生存：万物皆卡。探索、拾取、合成，活下去。[/color]");
	}

	public bool TryLoadGame()
	{
		var ok = _h.Save.LoadGame(_h.Player.State, _h.Cards, _h.Map, _h.Time, _h.Player, _h.Effects);
		if (!ok) return false;
		EnsureAvailableProjects();
		_h.RequestUiRefresh();
		_h.Log("[color=green]存档已加载。[/color]");
		return true;
	}

	public void SaveGame()
	{
		_h.Save.SaveGame(_h.Player.State, _h.Cards, _h.Map, _h.Time, _h.Player, _h.Effects);
	}

	public void SaveUserBackup(string? label = null)
	{
		_h.Save.SaveUserBackup(_h.Player.State, _h.Cards, _h.Map, _h.Time, _h.Player, _h.Effects, label);
	}

	public void Explore()
	{
		var loc = _h.Map.GetCurrentLocation();
		if (loc == null) return;

		var exploreCost = _h.Time.CurrentWeather == WeatherType.Foggy ? 15 : 10;
		if (_h.Player.State.Energy < exploreCost)
		{
			_h.Log("[color=red]精力不足，无法探索。[/color]");
			return;
		}

		_h.Player.ConsumeEnergy(exploreCost);
		_h.Time.AdvanceTime(0.05f);

		var pool = loc.GetExplorePoolForSeason(_h.Time.CurrentSeason);
		var found = ExploreFromPool(pool);
		_h.Log(found == null
			? $"探索了 {loc.Name}，一无所获。"
			: $"探索了 {loc.Name}，发现 {found.Name}。");

		if (_h.Time.CurrentWeather == WeatherType.Sunny && _h.Rng.NextDouble() < 0.25)
		{
			var bonus = ExploreFromPool(pool);
			_h.Log(bonus == null
				? "[color=yellow]晴光下你多搜寻了一圈，没有更多收获。[/color]"
				: $"[color=yellow]晴天馈赠：额外发现 {bonus.Name}。[/color]");
		}

		if (loc.SpecialEffect == "energy_bonus")
			_h.Player.RestoreEnergy(5);
		if (loc.SpecialEffect == "wolf_danger" && _h.Rng.NextDouble() < 0.10)
			ResolveWolfEvent();

		_h.Player.GainSkill("explore");
		TriggerRandomEvent();
		_h.RequestUiRefresh();
	}

	public void Rest()
	{
		if (_h.Player.State.Energy >= _h.Player.State.MaxEnergy)
		{
			_h.Log("[color=gray]你精神饱满，不需要休息。[/color]");
			return;
		}

		if (_h.Player.State.Hunger < 10 || _h.Player.State.Thirst < 10)
		{
			_h.Log("[color=red]太饿或太渴，无法安心休息。[/color]");
			return;
		}

		int energyRestore = 20;
		var shelterType = "野外";
		if (_h.Player.HasCompletedBuilding("stone_house"))
		{
			energyRestore = 60;
			shelterType = "石屋";
		}
		else if (_h.Player.HasCompletedBuilding("house"))
		{
			energyRestore = 45;
			shelterType = "木屋";
		}
		else if (_h.Player.HasCompletedBuilding("tent") || _h.Cards.GetPlayableHand().Any(c => c.Id == "tent"))
		{
			energyRestore = 30;
			shelterType = "帐篷";
		}

		var timePassed = 0.12f;
		if (_h.Time.IsNight())
		{
			timePassed = 0.20f;
			energyRestore = (int)(energyRestore * 1.5f);
		}

		_h.Player.RestoreEnergy(energyRestore);
		_h.Player.ConsumeHunger(5);
		_h.Player.ConsumeThirst(3);
		_h.Time.AdvanceTime(timePassed);
		_h.Log($"[color=cyan]在{shelterType}休息，精力 +{energyRestore}。[/color]");
		_h.RequestUiRefresh();
	}

	public void Sharpen()
	{
		const int cost = 8;
		if (_h.Player.State.Energy < cost)
		{
			_h.Log("[color=red]精力不足，无法磨刀。[/color]");
			return;
		}

		if (!_h.Cards.TrySharpenToolInHand())
		{
			_h.Log("[color=gray]没有可磨利的工具或武器。[/color]");
			return;
		}

		_h.Player.ConsumeEnergy(cost);
		_h.Time.AdvanceTime(0.02f);
		_h.Log("[color=green]磨刀：工具耐久 +1，精力 -8。[/color]");
		_h.RequestUiRefresh();
	}

	public void NightRitual()
	{
		if (!_h.Time.IsNight())
		{
			_h.Log("[color=gray]夜仪只在夜晚有效。[/color]");
			return;
		}

		if (!_h.Cards.HasCardInHand("campfire") || !_h.Cards.HasCardInHand("herb"))
		{
			_h.Log("[color=red]需要手牌中有火堆与草药。[/color]");
			return;
		}

		var hasDisease = _h.Effects.GetDiseases().Count > 0;
		if (!hasDisease && _h.Player.State.Sanity >= 50)
		{
			_h.Log("[color=gray]你尚不需要这场仪式。[/color]");
			return;
		}

		var herb = _h.Cards.GetPlayableHand().FirstOrDefault(c => c.Id == "herb");
		if (herb == null) return;

		_h.Cards.ConsumeCardFromHand(herb, "ritual");
		_h.Time.AdvanceTime(0.05f);
		_h.Player.ConsumeHunger(3);
		if (hasDisease)
		{
			var removed = _h.Effects.TryRemoveFirstDisease();
			_h.Log(removed
				? "[color=magenta]夜仪：青烟升起，病痛稍退。[/color]"
				: "[color=magenta]夜仪：心绪稍安。[/color]");
		}
		else
			_h.Log("[color=magenta]夜仪：草药在火边安抚了你的神经。[/color]");

		_h.Player.UpdateSanity(20);
		_h.RequestUiRefresh();
	}

	public void MoveToLocation(string id)
	{
		if (!_h.Map.GetConnections(_h.Map.CurrentLocation).Contains(id))
		{
			_h.Log("[color=red]只能前往相邻区域。[/color]");
			return;
		}

        var cost = Math.Max(1, (int)MathF.Round(15 * _h.Player.State.MoveEnergyCostMultiplier * _h.Player.GetBodyFatMoveMultiplier()));
		if (_h.Player.State.Energy < cost)
		{
			_h.Log("[color=red]精力不足，无法移动。[/color]");
			return;
		}

		_h.Player.ConsumeEnergy(cost);
		_h.Player.ConsumeHunger(3);
		_h.Player.ConsumeThirst(3);
		_h.Time.AdvanceTime(0.10f);
		_h.Map.MoveToLocation(id);
		_h.Player.State.CurrentLocation = id;
		_h.Log($"移动到 {(_h.Map.GetLocation(id)?.Name ?? id)}。");
		_h.RequestUiRefresh();
	}

	public void HandleCardAction(CardData card, string action)
	{
		if (card.IsStagedIngredient || CardManager.IsStagedCombineDisplay(card)) return;
		var ctx = new CardHandActionContext { Host = _h };
		foreach (var cmd in CardHandCommandRegistry.CommandsFor(card))
		{
			if (cmd.Id != action || !cmd.CanExecute(card)) continue;
			cmd.Execute(ctx, card);
			_h.RequestUiRefresh();
			return;
		}
	}

	/// <summary>手牌内两卡叠放合成：成功则移除此二卡（由 CombineSystem 信号产出）。</summary>
	public bool TryCombineHandPair(CardData a, CardData b)
	{
		if (a.IsStagedIngredient || b.IsStagedIngredient) return false;
		if (CardManager.IsStagedCombineDisplay(a) || CardManager.IsStagedCombineDisplay(b)) return false;
		if (!_h.Combine.TryCombine(a, b)) return false;
		_h.Cards.RemoveCardFromHand(a);
		_h.Cards.RemoveCardFromHand(b);
		return true;
	}

	public bool TryStageCombineRecipe(CombineRule rule)
	{
		if (!_h.Cards.TryStageRecipe(rule, _h.Combine))
		{
			_h.Log("[color=red]无法暂存（材料不足或已有暂存）。[/color]");
			return false;
		}

		_h.Log("[color=cyan]已暂存配方材料，点击「待合成」卡进行合成。[/color]");
		_h.RequestUiRefresh();
		return true;
	}

	public bool TryCompleteStagedCombine()
	{
		var rule = _h.Cards.GetStagedCombineRule();
		var order = _h.Cards.GetStagedConsumeOrder();
		if (rule == null || order == null || order.Count == 0) return false;

		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction(_h.Combine.GetSynthesisMinutes(rule)));
		foreach (var c in order)
			_h.Cards.ConsumeCardFromHand(c, "staged_craft");
		_h.Combine.LearnRuleAndResults(rule);
		foreach (var id in rule.Results)
			AddCraftResultToHandOrAnchored(id);
		_h.Cards.ClearStagedCombineMetaOnly();
		EnsureAvailableProjects();
		_h.RefreshCraftIfOpen();
		_h.Log($"[color=green]合成成功：{DescribeRule(rule)}。[/color]");
		return true;
	}

	public void CancelStagedCombine()
	{
		_h.Cards.CancelStagedRecipe();
		_h.RequestUiRefresh();
	}

	public bool CraftRecipe(CombineRule rule)
	{
		var play = _h.Cards.GetPlayableHand();
		if (!_h.Combine.CanCraftRecipe(rule, play))
		{
			_h.Log("[color=red]材料不足。[/color]");
			return false;
		}

		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction(_h.Combine.GetSynthesisMinutes(rule)));
		var consumed = _h.Combine.CraftRecipe(rule, play);
		foreach (var card in consumed)
			_h.Cards.RemoveCardFromHand(card);
		foreach (var id in rule.Results)
			AddResultToScene(id);

		EnsureAvailableProjects();
		_h.Log($"[color=green]合成成功：{DescribeRule(rule)}。[/color]");
		_h.RefreshCraftIfOpen();
		return true;
	}

	public bool FreeCraft(List<CardData> ingredients)
	{
		if (ingredients.Count < 2) return false;
		if (!_h.Combine.TryMultiCombine(ingredients)) return false;

		foreach (var card in ingredients)
			if (_h.Cards.GetHand().Contains(card))
				_h.Cards.RemoveCardFromHand(card);
		EnsureAvailableProjects();
		_h.RefreshCraftIfOpen();
		return true;
	}

	public bool BuildProject(string id)
	{
		var project = _h.Player.GetActiveProjects().FirstOrDefault(p => p.Id == id);
		var def = _h.Cards.GetProject(id);
		if (project == null || def == null) return false;

		var material = _h.Cards.GetPlayableHand().FirstOrDefault(c => c.Id == def.MaterialId);
		if (material == null)
		{
			_h.Log("[color=red]缺少建造材料。[/color]");
			return false;
		}

		_h.Cards.RemoveCardFromHand(material);
		var stepMinutes = def.BuildStepMinutes > 0 ? def.BuildStepMinutes : _h.Settings.GetDefaultActionMinutes("BuildProjectStep");
		var minutes = stepMinutes / Math.Max(0.01f, _h.Player.State.BuildSpeed);
		_h.Time.AdvanceTime(Math.Max(SurviveTime.MinutesToDayFraction(1f), SurviveTime.MinutesToDayFraction(minutes)));
		var done = _h.Player.BuildProject(id);
		_h.Log($"建造 {def.Name}: {project.Progress}/{project.Required}");

		if (done)
		{
			_h.Player.CompleteProject(id);
			_h.Player.AddCompletedBuilding(id);
			_h.Map.AddCompletedBuildingToEnvironment(def.ResultCardId);
			AddResultToScene(def.ResultCardId);
			_h.Log($"[color=green]{def.Name} 完成。[/color]");

			if (id == "house" || id == "stone_house")
				_h.Effects.AddEffect("sheltered", 1, -1);

			EnsureAvailableProjects();
		}
		_h.RefreshCraftIfOpen();
		return true;
	}

	public void OnCombineSuccess(string a, string b, string[] results, double synthesisMinutes)
	{
		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction((float)synthesisMinutes));
		foreach (var id in results)
			AddResultToScene(id);
		EnsureAvailableProjects();
		_h.Log(results.Length == 0
			? $"[color=green]{a}+{b} 被消除。[/color]"
			: $"[color=green]合成成功：{a}+{b}。[/color]");
	}

	public void OnMultiCombineSuccess(string[] ingredients, string[] results, double synthesisMinutes)
	{
		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction((float)synthesisMinutes));
		foreach (var id in results)
			AddResultToScene(id);
		_h.Log(results.Length == 0
			? $"[color=green]{string.Join("+", ingredients)} 被消除。[/color]"
			: $"[color=green]自由合成成功：{string.Join("+", ingredients)}。[/color]");
	}

	public List<CombineRule> GetLearnedRecipes()
	{
		_rulesCache ??= _h.Combine.GetRules();
		return _rulesCache
			.Where(r => IsStarterRecipe(r) || _h.Player.IsRecipeLearned(_h.Combine.GetRecipeKey(r)))
			.ToList();
	}

	public void InvalidateRulesCache() => _rulesCache = null;

	public void EnsureAvailableProjects()
	{
		foreach (var project in _h.Cards.GetAllProjects())
		{
			if (_h.Player.HasCompletedBuilding(project.Id)) continue;
			if (_h.Player.GetActiveProjects().Any(p => p.Id == project.Id)) continue;
			if (string.IsNullOrEmpty(project.UnlockRecipe)
			    || _h.Player.IsRecipeLearned(project.UnlockRecipe)
			    || _h.Player.HasCompletedBuilding(project.UnlockRecipe))
				_h.Player.AddProject(project);
		}
	}

	public void OnHandCardToScene(CardData data)
	{
		CardOpTime("DropToScene", data);
		_h.Cards.MoveHandCardToScene(data);
		_h.Log($"放下了 {data.Name}。");
		_h.RequestUiRefresh();
	}

	public void OnSceneCardToHand(CardData card)
	{
		CardOpTime("PickupScene", card);
		_h.Cards.MoveSceneCardToHand(card);
		_h.Log($"拾取了 {card.Name}。");
		_h.RequestUiRefresh();
	}

	public void OnPlayerDeath()
	{
		_h.Log("[color=red]你倒下了。[/color]");
	}

	public void OnCardConsumed(string id, string reason)
	{
		if (reason == "hand_limit")
			_h.Log($"[color=orange]手牌超过上限，丢弃 {id}。[/color]");
		_h.RequestUiRefresh();
	}

	private CardData? ExploreFromPool(List<string> pool)
	{
		if (pool.Count == 0) return _h.Cards.DrawCard();

		var id = pool[_h.Rng.Next(pool.Count)];
		var template = _h.Cards.GetCard(id);
		if (template == null) return null;

		var instance = _h.Cards.CreateCardInstance(id);
		if (instance != null)
			_h.Cards.AddCardToScene(instance);
		return instance;
	}

	private void TriggerRandomEvent()
	{
		var chance = 0.30f + _h.Player.State.DiscoverBonus;
		if (_h.Player.HasCompletedBuilding("watchtower"))
			chance *= 0.5f;
		if (_h.Rng.NextDouble() >= chance) return;

		var subRoll = _h.Rng.NextDouble();
		if (subRoll < 0.33f) ResolveWolfEvent();
		else if (subRoll < 0.50f) ResolveStormEvent();
		else if (subRoll < 0.83f)
		{
			var bonus = _h.Cards.ExploreLocation(_h.Map.CurrentLocation);
			_h.Log(bonus == null
				? "[color=yellow]发现了一些痕迹，但没有可用物资。[/color]"
				: $"[color=yellow]幸运发现：{bonus.Name}。[/color]");
		}
		else
		{
			_h.Player.TakeDamage(10);
			_h.Log("[color=orange]被毒蛇咬伤，生命 -10。[/color]");
		}
	}

	private void ResolveWolfEvent()
	{
		if (_h.Player.HasCompletedBuilding("wall"))
		{
			_h.Log("[color=green]围墙挡住了野兽袭击。[/color]");
			return;
		}

		var hasWeapon = _h.Cards.GetPlayableHand().Any(c => c.Tags.Contains(CardTag.Weapon));
		var successChance = hasWeapon ? 0.75f + _h.Player.State.HuntBonus : 0.15f + _h.Player.State.HuntBonus;
		if (_h.Rng.NextDouble() <= successChance)
		{
			_h.Player.GainSkill("fight");
			_h.Log("[color=green]你赶走了野兽。[/color]");
		}
		else
		{
			_h.Player.TakeDamage(15);
			_h.Effects.TryTriggerDisease("injury", 0.6f);
			_h.Log("[color=orange]野兽袭击，生命 -15，可能受伤。[/color]");
		}
	}

	private void ResolveStormEvent()
	{
		var sheltered = _h.Cards.GetPlayableHand().Any(c => c.Tags.Contains(CardTag.Shelter))
		                || _h.Player.HasCompletedBuilding("house")
		                || _h.Player.HasCompletedBuilding("stone_house");
		if (sheltered)
			_h.Log("[color=cyan]暴风雨来了，但庇护所保护了你。[/color]");
		else
		{
			_h.Player.UpdateTemperature(-5);
			_h.Effects.TryTriggerDisease("cold", 0.3f);
			_h.Log("[color=cyan]暴风雨来袭，温度 -5，可能感冒。[/color]");
		}
	}

	private void AddCraftResultToHandOrAnchored(string id)
	{
		if (string.IsNullOrEmpty(id)) return;
		var inst = _h.Cards.CreateCardInstance(id);
		if (inst == null) return;
		var def = _h.Cards.GetCard(id);
		if (def != null && (def.Tags.Contains(CardTag.Permanent) || (def.Type == CardType.Building && !def.IsDraggable)))
			_h.Cards.SetAnchoredCard(inst);
		else
			_h.Cards.AddCardToHand(inst);
	}

	private void AddResultToScene(string id)
	{
		if (string.IsNullOrEmpty(id)) return;
		var card = _h.Cards.CreateCardInstance(id);
		if (card != null)
			_h.Cards.AddCardToScene(card);
	}

	private static bool IsStarterRecipe(CombineRule rule)
	{
		if (rule.Ingredients.Count > 0)
			return false;
		if (rule.Results.Contains("axe") && rule.CardA == "wood" && rule.CardB == "stone")
			return true;
		if (rule.Results.Contains("campfire") && rule.CardA == "wood" && rule.CardB == "wood")
			return true;
		if (rule.Results.Contains("cooked_meat") && (rule.CardA == "meat" || rule.CardA == "fish") && rule.CardB == "campfire")
			return true;
		if (rule.Results.Contains("tent") && rule.CardA == "wood" && rule.CardB == "hide")
			return true;
		return false;
	}

	private static string DescribeRule(CombineRule rule)
	{
		var inputs = rule.Ingredients.Count > 0 ? rule.Ingredients : new List<string> { rule.CardA, rule.CardB };
		return $"{string.Join("+", inputs)} -> {string.Join("+", rule.Results)}";
	}
}
