using Godot;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CraftPopup : Control
{
	public event Action<CombineRule>? OnCraftRecipe;
	public event Action<string>? OnBuildProject;
	public event Action<List<CardData>>? OnFreeCraft;
	public event Action? OnClearFreeCraft;
	public event Action? OnClose;

	private VBoxContainer _recipeList = null!;
	private VBoxContainer _projectList = null!;
	private HBoxContainer _slotRow = null!;
	private HBoxContainer _handRow = null!;
	private Label _preview = null!;
	private Button _freeCraftButton = null!;
	private readonly List<CardData> _selected = new();
	private List<CombineRule> _recipes = new();
	private List<ProjectState> _projects = new();
	private List<CardData> _hand = new();
	private CardManager _cardManager = null!;
	private CombineSystem _combineSystem = null!;

	public override void _Ready()
	{
		_cardManager = GetNode<CardManager>("/root/CardManager");
		_combineSystem = GetNode<CombineSystem>("/root/CombineSystem");
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		BuildUI();
	}

	private void BuildUI()
	{
		AddChild(CreateOverlay());

		var panel = CreateCenteredPanel(new Vector2(560, 520));
		AddChild(panel);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		panel.AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 6);
		margin.AddChild(root);

		root.AddChild(CreateTitleRow("合成与建造"));
		root.AddChild(MakeTitle("配方"));
		_recipeList = MakeList(root, 112, vertical: true);

		root.AddChild(MakeTitle("工程"));
		_projectList = MakeList(root, 118, vertical: true);

		root.AddChild(MakeTitle("自由合成"));
		_slotRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_slotRow.AddThemeConstantOverride("separation", 6);
		root.AddChild(_slotRow);

		_preview = new Label { Text = "选择 2-4 张手牌进行尝试", HorizontalAlignment = HorizontalAlignment.Center };
		_preview.AddThemeFontSizeOverride("font_size", 12);
		root.AddChild(_preview);

		var buttonRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttonRow.AddThemeConstantOverride("separation", 8);
		root.AddChild(buttonRow);

		_freeCraftButton = new Button { Text = "合成", Disabled = true, CustomMinimumSize = new Vector2(80, 28) };
		_freeCraftButton.Pressed += () => OnFreeCraft?.Invoke(new List<CardData>(_selected));
		buttonRow.AddChild(_freeCraftButton);

		var clear = new Button { Text = "清空", CustomMinimumSize = new Vector2(80, 28) };
		clear.Pressed += ClearSelection;
		buttonRow.AddChild(clear);

		root.AddChild(MakeTitle("手牌选择"));
		var handScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0, 70),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		root.AddChild(handScroll);

		_handRow = new HBoxContainer();
		_handRow.AddThemeConstantOverride("separation", 6);
		handScroll.AddChild(_handRow);
		RefreshFreeCraft();
	}

	private ColorRect CreateOverlay()
	{
		var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.62f) };
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		overlay.MouseFilter = MouseFilterEnum.Stop;
		overlay.GuiInput += e =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				OnClose?.Invoke();
		};
		return overlay;
	}

	private static Panel CreateCenteredPanel(Vector2 size)
	{
		var panel = new Panel();
		panel.CustomMinimumSize = size;
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		return panel;
	}

	private HBoxContainer CreateTitleRow(string titleText)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);

		var title = new Label { Text = titleText };
		title.AddThemeFontSizeOverride("font_size", 16);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(title);

		var close = new Button { Text = "关闭", CustomMinimumSize = new Vector2(70, 28) };
		close.Pressed += () => OnClose?.Invoke();
		row.AddChild(close);
		return row;
	}

	private static Label MakeTitle(string text)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", 13);
		label.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
		return label;
	}

	private static VBoxContainer MakeList(VBoxContainer parent, int height, bool vertical)
	{
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, height) };
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		scroll.VerticalScrollMode = vertical ? ScrollContainer.ScrollMode.Auto : ScrollContainer.ScrollMode.Disabled;
		parent.AddChild(scroll);

		var list = new VBoxContainer();
		list.AddThemeConstantOverride("separation", 3);
		list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(list);
		return list;
	}

	public void Refresh(List<CombineRule> learnedRecipes, List<ProjectState> projects, List<CardData> hand)
	{
		_recipes = learnedRecipes;
		_projects = projects;
		_hand = hand;
		_selected.RemoveAll(card => !_hand.Contains(card));
		RefreshRecipes();
		RefreshProjects();
		RefreshHand();
		RefreshFreeCraft();
	}

	private void RefreshRecipes()
	{
		Clear(_recipeList);
		if (_recipes.Count == 0)
		{
			_recipeList.AddChild(new Label { Text = "还没有配方。可从手牌选择物品进行自由合成。" });
			return;
		}

		foreach (var rule in _recipes)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			_recipeList.AddChild(row);

			var label = new Label { Text = $"{DescribeIngredients(rule)} -> {DescribeResults(rule)}" };
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			label.ClipText = true;
			row.AddChild(label);

			var button = new Button { Text = "合成", Disabled = !HasIngredients(rule), CustomMinimumSize = new Vector2(70, 24) };
			var captured = rule;
			button.Pressed += () => OnCraftRecipe?.Invoke(captured);
			row.AddChild(button);
		}
	}

	private void RefreshProjects()
	{
		Clear(_projectList);
		if (_projects.Count == 0)
		{
			_projectList.AddChild(new Label { Text = "暂无可建造工程。" });
			return;
		}

		foreach (var project in _projects)
		{
			var def = _cardManager.GetProject(project.Id);
			if (def == null) continue;

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			_projectList.AddChild(row);

			var label = new Label { Text = $"{def.Name}  材料:{GetName(def.MaterialId)}  {project.Progress}/{project.Required}" };
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			label.ClipText = true;
			row.AddChild(label);

			var hasMat = _hand.Any(c => c.Id == def.MaterialId);
			var build = new Button { Text = "建造", Disabled = !hasMat, CustomMinimumSize = new Vector2(70, 24) };
			var id = project.Id;
			build.Pressed += () => OnBuildProject?.Invoke(id);
			row.AddChild(build);
		}
	}

	private void RefreshHand()
	{
		Clear(_handRow);
		foreach (var card in _hand)
		{
			var button = new Button
			{
				Text = card.Name,
				Disabled = _selected.Contains(card),
				CustomMinimumSize = new Vector2(82, 30)
			};
			var captured = card;
			button.Pressed += () =>
			{
				if (_selected.Count >= 4 || _selected.Contains(captured)) return;
				_selected.Add(captured);
				RefreshFreeCraft();
				RefreshHand();
			};
			_handRow.AddChild(button);
		}
	}

	private void RefreshFreeCraft()
	{
		if (_slotRow == null) return;
		Clear(_slotRow);
		for (int i = 0; i < 4; i++)
		{
			var text = i < _selected.Count ? _selected[i].Name : "空";
			var button = new Button { Text = text, CustomMinimumSize = new Vector2(88, 34), Disabled = i >= _selected.Count };
			var index = i;
			button.Pressed += () =>
			{
				if (index < _selected.Count)
				{
					_selected.RemoveAt(index);
					RefreshFreeCraft();
					RefreshHand();
				}
			};
			_slotRow.AddChild(button);
		}

		_freeCraftButton.Disabled = _selected.Count < 2;
		var preview = _combineSystem.PreviewResult(_selected);
		_preview.Text = _selected.Count < 2
			? "选择 2-4 张手牌进行尝试"
			: preview == null
				? "预览：没有匹配配方"
				: $"预览：{string.Join(" + ", _selected.Select(c => c.Name))} -> {DescribeResultIds(preview)}";
	}

	private void ClearSelection()
	{
		_selected.Clear();
		RefreshFreeCraft();
		RefreshHand();
		OnClearFreeCraft?.Invoke();
	}

	private bool HasIngredients(CombineRule rule)
	{
		// 构建可用材料计数（适配堆叠）
		var available = new Dictionary<string, int>();
		foreach (var card in _hand)
		{
			var key = card.Id;
			available.TryGetValue(key, out var count);
			available[key] = count + card.Stack;
			// 标签匹配也加入计数
			foreach (var tag in card.Tags)
			{
				var tagKey = "tag:" + tag.ToString();
				available.TryGetValue(tagKey, out var tagCount);
				available[tagKey] = tagCount + card.Stack;
			}
		}

		foreach (var need in GetIngredientIds(rule))
		{
			if (available.TryGetValue(need, out var count) && count > 0)
			{
				available[need] = count - 1;
			}
			else if (available.TryGetValue("tag:" + need, out var tagCount) && tagCount > 0)
			{
				available["tag:" + need] = tagCount - 1;
			}
			else
			{
				return false;
			}
		}
		return true;
	}

	private IEnumerable<string> GetIngredientIds(CombineRule rule)
	{
		if (rule.Ingredients.Count > 0) return rule.Ingredients;
		return new[] { rule.CardA, rule.CardB };
	}

	private string DescribeIngredients(CombineRule rule)
	{
		return string.Join("+", GetIngredientIds(rule).Select(GetName));
	}

	private string DescribeResults(CombineRule rule)
	{
		return rule.Results.Count == 0 ? "消除" : string.Join("+", rule.Results.Select(GetName));
	}

	private string DescribeResultIds(string ids)
	{
		if (string.IsNullOrEmpty(ids)) return "消除";
		return string.Join("+", ids.Split("+").Select(GetName));
	}

	private string GetName(string id)
	{
		return _cardManager.GetCard(id)?.Name ?? id;
	}

	private static void Clear(Node node)
	{
		foreach (var child in node.GetChildren().ToArray())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

}
                                             