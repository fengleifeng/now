using Godot;
using CardSurvival.Game;

namespace CardSurvival.UI.Game;

/// <summary>
/// 将 <see cref="CombineSystem"/> 的 Godot 信号桥接到 <see cref="CardSurvivalGame"/> 与 UI 反馈（日志、音效）。
/// 与「尝试合成」的指令路径分离，仅处理合成结果回调。
/// </summary>
public sealed class CombineSignalBridge
{
	/// <summary>
	/// 订阅合成成功/失败/配方学习等信号。
	/// </summary>
	public CombineSignalBridge(
		GameServices services,
		CardSurvivalGame game,
		GameViewController view)
	{
		var combine = services.Combine;
		var settings = services.Settings;

		combine.Connect(CombineSystem.SignalName.OnCombineSuccess,
			Callable.From((string a, string b, string[] results, double synthesisMinutes) =>
			{
				game.OnCombineSuccess(a, b, results, synthesisMinutes);
				settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
			}));

		combine.Connect(CombineSystem.SignalName.OnCombineFail,
			Callable.From((string a, string b) =>
			{
				view.AppendLog(I18n.Tf("ui.combine_fail_fmt", a, b));
				game.AdvanceCombineFailTime();
				settings.PlayCardUiSound(CardUiSoundKind.CombineFail);
			}));

		combine.Connect(CombineSystem.SignalName.OnMultiCombineSuccess,
			Callable.From((string[] ingredients, string[] results, double synthesisMinutes) =>
			{
				game.OnMultiCombineSuccess(ingredients, results, synthesisMinutes);
				settings.PlayCardUiSound(CardUiSoundKind.CombineSuccess);
			}));

		combine.Connect(CombineSystem.SignalName.OnMultiCombineFail,
			Callable.From(() =>
			{
				view.AppendLog(I18n.T("ui.combine_impossible"));
				game.AdvanceCombineFailTime();
				settings.PlayCardUiSound(CardUiSoundKind.CombineFail);
			}));

		combine.Connect(CombineSystem.SignalName.OnRecipeLearned,
			Callable.From((string _) => game.InvalidateRulesCache()));
	}
}
