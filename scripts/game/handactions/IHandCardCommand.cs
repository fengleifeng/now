using CardSurvival.Data;

namespace CardSurvival.Game.HandActions;

public enum HandCardUiBand
{
	Toolbar,
	Footer
}

/// <summary>单条手牌操作：弹窗展示 + 点击后执行。</summary>
public interface IHandCardCommand
{
	string Id { get; }
	string Label { get; }
	HandCardUiBand UiBand { get; }
	bool CanExecute(CardData card);
	void Execute(CardHandActionContext ctx, CardData card);
}

/// <summary>手牌操作命令基类：子类实现具体卡牌逻辑或通用操作。</summary>
public abstract class HandCardCommandBase : IHandCardCommand
{
	public abstract string Id { get; }
	public abstract string Label { get; }
	public virtual HandCardUiBand UiBand => HandCardUiBand.Toolbar;
	public abstract bool CanExecute(CardData card);
	public abstract void Execute(CardHandActionContext ctx, CardData card);
}
