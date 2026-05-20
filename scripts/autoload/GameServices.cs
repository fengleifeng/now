using Godot;

namespace CardSurvival;

/// <summary>
/// 全局服务入口：集中解析 Autoload 单例，避免各 UI 类重复 GetNode("/root/...")。
/// 应注册在 project.godot 的 autoload 列表末尾，确保其它单例已就绪。
/// </summary>
public partial class GameServices : Node
{
	private CardManager? _cards;
	private CombineSystem? _combine;
	private PlayerSystem? _player;
	private TimeSystem? _time;
	private MapSystem? _map;
	private SaveSystem? _save;
	private EffectSystem? _effects;
	private GameSettings? _settings;
	private AchievementSystem? _achievements;

	/// <summary>卡牌与手牌/场景库存。</summary>
	public CardManager Cards => _cards ??= GetNode<CardManager>("/root/CardManager");

	/// <summary>合成规则与尝试合成。</summary>
	public CombineSystem Combine => _combine ??= GetNode<CombineSystem>("/root/CombineSystem");

	/// <summary>玩家状态、特质、项目进度。</summary>
	public PlayerSystem Player => _player ??= GetNode<PlayerSystem>("/root/PlayerSystem");

	/// <summary>游戏内时间与天气。</summary>
	public TimeSystem Time => _time ??= GetNode<TimeSystem>("/root/TimeSystem");

	/// <summary>地点与场景环境卡。</summary>
	public MapSystem Map => _map ??= GetNode<MapSystem>("/root/MapSystem");

	/// <summary>存档读写。</summary>
	public SaveSystem Save => _save ??= GetNode<SaveSystem>("/root/SaveSystem");

	/// <summary>疾病、增益效果。</summary>
	public EffectSystem Effects => _effects ??= GetNode<EffectSystem>("/root/EffectSystem");

	/// <summary>用户设置与 UI 音效。</summary>
	public GameSettings Settings => _settings ??= GetNode<GameSettings>("/root/GameSettings");

	/// <summary>成就检测与日志队列。</summary>
	public AchievementSystem Achievements => _achievements ??= GetNode<AchievementSystem>("/root/AchievementSystem");

	/// <summary>从任意场景节点获取 GameServices 单例。</summary>
	public static GameServices From(Node node) => node.GetNode<GameServices>("/root/GameServices");
}
