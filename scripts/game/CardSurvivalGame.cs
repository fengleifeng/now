using System;
using System.Collections.Generic;
using System.Linq;
using CardSurvival;
using CardSurvival.Data;
using CardSurvival.Game.HandActions;
using Godot;

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
		public Action? OnAchievementProbe { get; init; }
		public Random Rng { get; init; } = new();
	}

	private readonly Host _h;
	private List<CombineRule>? _rulesCache;
	private SceneHandDanceCatalog? _sceneDanceCatalog;

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
		_h.OnAchievementProbe?.Invoke();
		_h.RequestUiRefresh();
		_h.Log(I18n.T("log.newgame"));
	}

	public bool TryLoadGame()
	{
		var ok = _h.Save.LoadGame(_h.Player.State, _h.Cards, _h.Map, _h.Time, _h.Player, _h.Effects);
		if (!ok) return false;
		EnsureAvailableProjects();
		_h.OnAchievementProbe?.Invoke();
		_h.RequestUiRefresh();
		_h.Log(I18n.T("log.loadok"));
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
			_h.Log(I18n.T("log.explore_no_energy"));
			return;
		}

		_h.Player.ConsumeEnergy(exploreCost);
		_h.Time.AdvanceTime(0.05f);

		var pool = loc.GetExplorePoolForSeason(_h.Time.CurrentSeason);
		var found = ExploreFromPool(pool);
		_h.Log(found == null
			? I18n.Tf("log.explore_empty_fmt", loc.Name)
			: I18n.Tf("log.explore_found_fmt", loc.Name, found.Name));

		if (_h.Time.CurrentWeather == WeatherType.Sunny && _h.Rng.NextDouble() < 0.25)
		{
			var bonus = ExploreFromPool(pool);
			_h.Log(bonus == null
				? I18n.T("log.sunny_extra_none")
				: I18n.Tf("log.sunny_extra_fmt", bonus.Name));
		}

		if (loc.SpecialEffect == "energy_bonus")
			_h.Player.RestoreEnergy(5);
		if (loc.SpecialEffect == "wolf_danger" && _h.Rng.NextDouble() < 0.10)
			ResolveWolfEvent();

		_h.Player.GainSkill("explore");
		TriggerRandomEvent();
		_h.Player.IncrementLifetimeExplore();
		_h.OnAchievementProbe?.Invoke();
		_h.RequestUiRefresh();
	}

	public void Rest()
	{
		if (_h.Player.State.Energy >= _h.Player.State.MaxEnergy)
		{
			_h.Log(I18n.T("log.rest_full"));
			return;
		}

		if (_h.Player.State.Hunger < 10 || _h.Player.State.Thirst < 10)
		{
			_h.Log(I18n.T("log.rest_hungry"));
			return;
		}

		int energyRestore = 20;
		var shelterKey = "shelter.wild";
		if (_h.Player.HasCompletedBuilding("stone_house"))
		{
			energyRestore = 60;
			shelterKey = "shelter.stone_house";
		}
		else if (_h.Player.HasCompletedBuilding("house"))
		{
			energyRestore = 45;
			shelterKey = "shelter.house";
		}
		else if (_h.Player.HasCompletedBuilding("tent") || _h.Cards.GetPlayableHand().Any(c => c.Id == "tent"))
		{
			energyRestore = 30;
			shelterKey = "shelter.tent";
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
		_h.Log(I18n.Tf("log.rest_fmt", I18n.T(shelterKey), energyRestore));
		_h.RequestUiRefresh();
	}

	public void Sharpen()
	{
		const int cost = 8;
		if (_h.Player.State.Energy < cost)
		{
			_h.Log(I18n.T("log.sharpen_no_energy"));
			return;
		}

		if (!_h.Cards.TrySharpenToolInHand())
		{
			_h.Log(I18n.T("log.sharpen_no_tool"));
			return;
		}

		_h.Player.ConsumeEnergy(cost);
		_h.Time.AdvanceTime(0.02f);
		_h.Log(I18n.T("log.sharpen_ok"));
		_h.RequestUiRefresh();
	}

	public void NightRitual()
	{
		if (!_h.Time.IsNight())
		{
			_h.Log(I18n.T("log.ritual_not_night"));
			return;
		}

		if (!_h.Cards.HasCardInHand("campfire") || !_h.Cards.HasCardInHand("herb"))
		{
			_h.Log(I18n.T("log.ritual_need_cards"));
			return;
		}

		var hasDisease = _h.Effects.GetDiseases().Count > 0;
		if (!hasDisease && _h.Player.State.Sanity >= 50)
		{
			_h.Log(I18n.T("log.ritual_no_need"));
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
				? I18n.T("log.ritual_cured")
				: I18n.T("log.ritual_calm"));
		}
		else
			_h.Log(I18n.T("log.ritual_sanity"));

		_h.Player.UpdateSanity(20);
		_h.RequestUiRefresh();
	}

	public void MoveToLocation(string id)
	{
		if (!_h.Map.GetConnections(_h.Map.CurrentLocation).Contains(id))
		{
			_h.Log(I18n.T("log.move_not_adjacent"));
			return;
		}

        var cost = Math.Max(1, (int)MathF.Round(15 * _h.Player.State.MoveEnergyCostMultiplier * _h.Player.GetBodyFatMoveMultiplier()));
		if (_h.Player.State.Energy < cost)
		{
			_h.Log(I18n.T("log.move_no_energy"));
			return;
		}

		_h.Player.ConsumeEnergy(cost);
		_h.Player.ConsumeHunger(3);
		_h.Player.ConsumeThirst(3);
		_h.Time.AdvanceTime(0.10f);
		_h.Map.MoveToLocation(id);
		_h.Player.State.CurrentLocation = id;
		_h.Log(I18n.Tf("log.move_fmt", _h.Map.GetLocation(id)?.Name ?? id));
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
		if (!_h.Combine.IsRecipeUnlockedForAttempt(rule))
		{
			_h.Log(I18n.T("log.stage_locked"));
			return false;
		}

		if (!_h.Cards.TryStageRecipe(rule, _h.Combine))
		{
			_h.Log(I18n.T("log.stage_fail"));
			return false;
		}

		_h.Log(I18n.T("log.stage_ok"));
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
		foreach (var id in rule.Results)
			AddCraftResultToHandOrAnchored(id);
		_h.Cards.ClearStagedCombineMetaOnly();
		EnsureAvailableProjects();
		_h.RefreshCraftIfOpen();
		_h.Log(I18n.Tf("log.combine_success_fmt", DescribeRule(rule)));
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
		if (!_h.Combine.IsRecipeUnlockedForAttempt(rule))
		{
			_h.Log(I18n.T("log.recipe_locked"));
			return false;
		}

		if (!_h.Combine.CanCraftRecipe(rule, play))
		{
			_h.Log(I18n.T("log.not_enough_mats"));
			return false;
		}

		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction(_h.Combine.GetSynthesisMinutes(rule)));
		var consumed = _h.Combine.CraftRecipe(rule, play);
		foreach (var card in consumed)
			_h.Cards.RemoveCardFromHand(card);
		foreach (var id in rule.Results)
			AddResultToScene(id);

		EnsureAvailableProjects();
		_h.Log(I18n.Tf("log.combine_success_fmt", DescribeRule(rule)));
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
			_h.Log(I18n.T("log.build_no_mat"));
			return false;
		}

		_h.Cards.RemoveCardFromHand(material);
		var stepMinutes = def.BuildStepMinutes > 0 ? def.BuildStepMinutes : _h.Settings.GetDefaultActionMinutes("BuildProjectStep");
		var minutes = stepMinutes / Math.Max(0.01f, _h.Player.State.BuildSpeed);
		_h.Time.AdvanceTime(Math.Max(SurviveTime.MinutesToDayFraction(1f), SurviveTime.MinutesToDayFraction(minutes)));
		var done = _h.Player.BuildProject(id);
		_h.Log(I18n.Tf("log.build_progress_fmt", def.Name, project.Progress, project.Required));

		if (done)
		{
			_h.Player.CompleteProject(id);
			_h.Player.AddCompletedBuilding(id);
			_h.Map.AddCompletedBuildingToEnvironment(def.ResultCardId);
			AddResultToScene(def.ResultCardId);
			_h.Log(I18n.Tf("log.build_done_fmt", def.Name));

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
			? I18n.Tf("log.pair_vanish_fmt", a, b)
			: I18n.Tf("log.pair_success_fmt", a, b));
	}

	public void OnMultiCombineSuccess(string[] ingredients, string[] results, double synthesisMinutes)
	{
		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction((float)synthesisMinutes));
		foreach (var id in results)
			AddResultToScene(id);
		_h.Log(results.Length == 0
			? I18n.Tf("log.multi_vanish_fmt", string.Join("+", ingredients))
			: I18n.Tf("log.multi_success_fmt", string.Join("+", ingredients)));
	}

	public List<CombineRule> GetLearnedRecipes()
	{
		_rulesCache ??= _h.Combine.GetRules();
		return _rulesCache
			.Where(r => RecipeUnlockPolicy.IsVisibleInCraftList(_h.Settings, _h.Player.State, r, _h.Combine.GetRecipeKey(r)))
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

	/// <summary>
	/// 手牌拖到场景/地点栏时：若当前地点与卡牌匹配 <c>scene_hand_dances.json</c> 中的规则，则消耗精力/时间尝试产出（如湖泊 + 长矛/鱼叉 → 鱼）。
	/// </summary>
	/// <param name="playDanceFeedback">为 true 时 UI 可做「场景舞动」反馈（通常表示成功获得产物）。</param>
	/// <returns>已处理互动（含精力不足等）则为 true；无匹配规则则为 false，由调用方决定是否放下到场景。</returns>
	public bool TrySceneHandDance(CardData tool, out bool playDanceFeedback)
	{
		playDanceFeedback = false;
		EnsureSceneDanceCatalog();
		var loc = _h.Map.CurrentLocation;
		var rule = _sceneDanceCatalog?.Match(loc, tool.Id);

		if (rule == null)
			return false;

		if (_h.Player.State.Energy < rule.EnergyCost)
		{
			var msg = string.IsNullOrEmpty(rule.LogNoEnergy)
				? I18n.T("log.scene_no_energy")
				: (rule.LogNoEnergy.StartsWith("[") ? rule.LogNoEnergy : $"[color=orange]{rule.LogNoEnergy}[/color]");
			_h.Log(msg);
			_h.RequestUiRefresh();
			return true;
		}

		_h.Player.ConsumeEnergy(rule.EnergyCost);
		var minutes = rule.TimeMinutes > 0f
			? rule.TimeMinutes
			: _h.Settings.GetDefaultActionMinutes("SceneHandDance");
		_h.Time.AdvanceTime(SurviveTime.MinutesToDayFraction(minutes));

		var success = rule.SuccessChance >= 1f || _h.Rng.NextDouble() <= rule.SuccessChance;
		if (success)
		{
			var count = Math.Max(1, rule.ResultCount);
			for (var i = 0; i < count; i++)
			{
				var inst = _h.Cards.CreateCardInstance(rule.ResultCardId);
				if (inst != null)
					_h.Cards.AddCardToHand(inst);
			}

			var resultName = _h.Cards.GetCard(rule.ResultCardId)?.Name ?? rule.ResultCardId;
			var ok = string.IsNullOrEmpty(rule.LogSuccess)
				? I18n.Tf("log.scene_success_fmt", resultName)
				: (rule.LogSuccess.StartsWith("[") ? rule.LogSuccess : $"[color=green]{rule.LogSuccess}[/color]");
			_h.Log(ok);
			ApplySceneHandDanceDurability(tool, rule.DurabilityCost);
			playDanceFeedback = true;
		}
		else
		{
			var fail = string.IsNullOrEmpty(rule.LogFail)
				? I18n.T("log.scene_fail_default")
				: (rule.LogFail.StartsWith("[") ? rule.LogFail : $"[color=yellow]{rule.LogFail}[/color]");
			_h.Log(fail);
			ApplySceneHandDanceDurability(tool, rule.DurabilityCost);
		}

		_h.RequestUiRefresh();
		return true;
	}

	private void EnsureSceneDanceCatalog()
	{
		if (_sceneDanceCatalog != null) return;
		var path = ProjectSettings.GlobalizePath(ContentPaths.SceneHandDances);
		_sceneDanceCatalog = SceneHandDanceCatalog.Load(path);
	}

	private void ApplySceneHandDanceDurability(CardData tool, int cost)
	{
		if (cost <= 0) return;
		if (tool.Durability <= 0) return;
		tool.Durability -= cost;
		if (tool.Durability <= 0)
			_h.Cards.ConsumeCardFromHand(tool, "durability");
	}

	public void OnHandCardToScene(CardData data)
	{
		CardOpTime("DropToScene", data);
		_h.Cards.MoveHandCardToScene(data);
		_h.Log(I18n.Tf("log.drop_fmt", data.Name));
		_h.RequestUiRefresh();
	}

	public void OnSceneCardToHand(CardData card)
	{
		CardOpTime("PickupScene", card);
		_h.Cards.MoveSceneCardToHand(card);
		_h.Log(I18n.Tf("log.pickup_fmt", card.Name));
		_h.RequestUiRefresh();
	}

	public void OnPlayerDeath()
	{
		_h.Log(I18n.T("log.death"));
	}

	public void OnCardConsumed(string id, string reason)
	{
		if (reason == "hand_limit")
			_h.Log(I18n.Tf("log.hand_overflow_fmt", id));
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
				? I18n.T("log.lucky_trace")
				: I18n.Tf("log.lucky_find_fmt", bonus.Name));
		}
		else
		{
			_h.Player.TakeDamage(10);
			_h.Log(I18n.T("log.snake"));
		}
	}

	private void ResolveWolfEvent()
	{
		if (_h.Player.HasCompletedBuilding("wall"))
		{
			_h.Log(I18n.T("log.wolf_wall"));
			return;
		}

		var hasWeapon = _h.Cards.GetPlayableHand().Any(c => c.Tags.Contains(CardTag.Weapon));
		var successChance = hasWeapon ? 0.75f + _h.Player.State.HuntBonus : 0.15f + _h.Player.State.HuntBonus;
		if (_h.Rng.NextDouble() <= successChance)
		{
			_h.Player.GainSkill("fight");
			_h.Log(I18n.T("log.wolf_repelled"));
		}
		else
		{
			_h.Player.TakeDamage(15);
			_h.Effects.TryTriggerDisease("injury", 0.6f);
			_h.Log(I18n.T("log.wolf_hit"));
		}
	}

	private void ResolveStormEvent()
	{
		var sheltered = _h.Cards.GetPlayableHand().Any(c => c.Tags.Contains(CardTag.Shelter))
		                || _h.Player.HasCompletedBuilding("house")
		                || _h.Player.HasCompletedBuilding("stone_house");
		if (sheltered)
			_h.Log(I18n.T("log.storm_safe"));
		else
		{
			_h.Player.UpdateTemperature(-5);
			_h.Effects.TryTriggerDisease("cold", 0.3f);
			_h.Log(I18n.T("log.storm_cold"));
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

	private static string DescribeRule(CombineRule rule)
	{
		var inputs = rule.Ingredients.Count > 0 ? rule.Ingredients : new List<string> { rule.CardA, rule.CardB };
		return $"{string.Join("+", inputs)} -> {string.Join("+", rule.Results)}";
	}
}
