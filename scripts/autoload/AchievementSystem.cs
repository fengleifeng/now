using Godot;
using CardSurvival.Data;
using CardSurvival.Game;

/// <summary>
/// 成就 Autoload：加载数据、订阅手牌增量，将判定与解锁委托给 <see cref="AchievementEngine"/>。
/// </summary>
public partial class AchievementSystem : Node
{
	private AchievementEngine? _engine;
	private PlayerSystem? _player;
	private CombineSystem? _combine;

	public override void _Ready()
	{
		var path = ProjectSettings.GlobalizePath(ContentPaths.Achievements);
		var defs = DataLoader.LoadAchievements(path);
		_engine = new AchievementEngine(defs);
		_player = GetNodeOrNull<PlayerSystem>("/root/PlayerSystem");
		_combine = GetNodeOrNull<CombineSystem>("/root/CombineSystem");
		var cards = GetNodeOrNull<CardManager>("/root/CardManager");
		if (cards != null)
			cards.Connect(CardManager.SignalName.OnHandStackGained,
				Callable.From<string, int>(OnHandStackGained));
		GD.Print($"[AchievementSystem] Loaded {defs.Count} achievement definitions");
	}

	public bool TryDequeueLog(out string bbcodeLine)
	{
		if (_engine == null)
		{
			bbcodeLine = "";
			return false;
		}

		return _engine.TryDequeueLog(out bbcodeLine);
	}

	public void OnStateMayHaveChanged()
	{
		if (_engine == null || _player == null || _combine == null) return;
		_engine.Reevaluate(_player.State, key => _combine.LearnRecipe(key));
	}

	private void OnHandStackGained(string cardId, int amount)
	{
		if (_engine == null || _player == null || _combine == null) return;
		_engine.NotifyHandGained(_player.State, cardId, amount, key => _combine.LearnRecipe(key));
	}
}
