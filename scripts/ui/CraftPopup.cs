using Godot;
using CardSurvival;
using CardSurvival.Data;

namespace CardSurvival.UI;

public partial class CraftPopup : Control
{
	public event Action<CombineRule>? OnCraftRecipe;
	public event Action<CombineRule>? OnStageRecipe;
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
	private Label _mainTitle = null!;
	private Button _topClose = null!;
	private Label _lblRecipes = null!;
	private Label _lblProjects = null!;
	private Label _lblFree = null!;
	private Label _lblHandPick = null!;
	private Button _clearButton = null!;

	public override void _Ready()
	{
		var services = GameServices.From(this);
		_cardManager = services.Cards;
		_combineSystem = services.Combine;
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		BuildUI();
		I18n.LocaleChanged += OnLocaleChanged;
		ApplyStaticI18n();
	}

	public override void _ExitTree()
	{
		I18n.LocaleChanged -= OnLocaleChanged;
		base._ExitTree();
	}

	private void OnLocaleChanged()
	{
		ApplyStaticI18n();
		RefreshRecipes();
		RefreshProjects();
		RefreshHand();
		RefreshFreeCraft();
	}

	private void ApplyStaticI18n()
	{
		_mainTitle.Text = I18n.T("craft.popup_title");
		_topClose.Text = I18n.T("craft.close");
		_lblRecipes.Text = I18n.T("craft.section_recipes");
		_lblProjects.Text = I18n.T("craft.section_projects");
		_lblFree.Text = I18n.T("craft.section_free");
		_lblHandPick.Text = I18n.T("craft.section_hand");
		_freeCraftButton.Text = I18n.T("craft.combine");
		_clearButton.Text = I18n.T("craft.clear");
	}

	private void BuildUI()
	{
		ModalUi.AddDimOverlay(this, () => OnClose?.Invoke());
		var center = ModalUi.AddCenterLayer(this);

		var shell = new PanelContainer();
		UiLayout.ClampPanelMinSize(shell, new Vector2(560, 520));
		UiLayout.BindResponsive(this, () => UiLayout.ClampPanelMinSize(shell, new Vector2(560, 520)));
		GameTheme.ApplyModalPanel(shell);
		center.AddChild(shell);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		shell.AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 6);
		margin.AddChild(root);

		var titleRow = new HBoxContainer();
		titleRow.AddThemeConstantOverride("separation", 8);
		root.AddChild(titleRow);

		_mainTitle = new Label();
		_mainTitle.AddThemeFontSizeOverride("font_size", 16);
		_mainTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		titleRow.AddChild(_mainTitle);

		_topClose = new Button { CustomMinimumSize = new Vector2(70, 28) };
		GameTheme.StyleSidebarButton(_topClose);
		_topClose.Pressed += () => OnClose?.Invoke();
		titleRow.AddChild(_topClose);

		_lblRecipes = CreateSectionLabel();
		root.AddChild(_lblRecipes);
		_recipeList = MakeList(root, 112, vertical: true);

		_lblProjects = CreateSectionLabel();
		root.AddChild(_lblProjects);
		_projectList = MakeList(root, 118, vertical: true);

		_lblFree = CreateSectionLabel();
		root.AddChild(_lblFree);
		_slotRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_slotRow.AddThemeConstantOverride("separation", 6);
		root.AddChild(_slotRow);

		_preview = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_preview.AddThemeFontSizeOverride("font_size", 12);
		root.AddChild(_preview);

		var buttonRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttonRow.AddThemeConstantOverride("separation", 8);
		root.AddChild(buttonRow);

		_freeCraftButton = new Button { Disabled = true, CustomMinimumSize = new Vector2(80, 28) };
		_freeCraftButton.Pressed += () => OnFreeCraft?.Invoke(new List<CardData>(_selected));
		buttonRow.AddChild(_freeCraftButton);

		_clearButton = new Button { CustomMinimumSize = new Vector2(80, 28) };
		_clearButton.Pressed += ClearSelection;
		buttonRow.AddChild(_clearButton);

		_lblHandPick = CreateSectionLabel();
		root.AddChild(_lblHandPick);
		var handScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0, 70),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			ClipContents = true
		};
		root.AddChild(handScroll);

		_handRow = new HBoxContainer();
		_handRow.AddThemeConstantOverride("separation", 6);
		handScroll.AddChild(_handRow);
		RefreshFreeCraft();
	}

	private static Label CreateSectionLabel()
	{
		var label = new Label();
		label.AddThemeFontSizeOverride("font_size", 13);
		label.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
		return label;
	}

	private static VBoxContainer MakeList(VBoxContainer parent, int height, bool vertical)
	{
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, height), ClipContents = true };
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
			_recipeList.AddChild(new Label { Text = I18n.T("craft.no_recipes_yet") });
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

			var stageBtn = new Button { Text = I18n.T("craft.stage"), Disabled = !HasIngredients(rule), CustomMinimumSize = new Vector2(56, 24) };
			var captured = rule;
			stageBtn.Pressed += () => OnStageRecipe?.Invoke(captured);
			row.AddChild(stageBtn);

			var craftBtn = new Button { Text = I18n.T("craft.craft_now"), Disabled = !HasIngredients(rule), CustomMinimumSize = new Vector2(56, 24) };
			craftBtn.Pressed += () => OnCraftRecipe?.Invoke(captured);
			row.AddChild(craftBtn);
		}
	}

	private void RefreshProjects()
	{
		Clear(_projectList);
		if (_projects.Count == 0)
		{
			_projectList.AddChild(new Label { Text = I18n.T("craft.no_projects") });
			return;
		}

		foreach (var project in _projects)
		{
			var def = _cardManager.GetProject(project.Id);
			if (def == null) continue;

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			_projectList.AddChild(row);

			var label = new Label { Text = I18n.Tf("craft.project_line_fmt", def.Name, GetName(def.MaterialId), project.Progress, project.Required) };
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			label.ClipText = true;
			row.AddChild(label);

			var hasMat = _hand.Any(c => c.Id == def.MaterialId);
			var build = new Button { Text = I18n.T("craft.build"), Disabled = !hasMat, CustomMinimumSize = new Vector2(70, 24) };
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
			var text = i < _selected.Count ? _selected[i].Name : I18n.T("craft.empty_slot");
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
			? I18n.T("craft.preview_need_more")
			: preview == null
				? I18n.T("craft.preview_no_rule")
				: I18n.Tf("craft.preview_fmt", string.Join(" + ", _selected.Select(c => c.Name)), DescribeResultIds(preview));
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
		return rule.Results.Count == 0 ? I18n.T("craft.eliminate") : string.Join("+", rule.Results.Select(GetName));
	}

	private string DescribeResultIds(string ids)
	{
		if (string.IsNullOrEmpty(ids)) return I18n.T("craft.eliminate");
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
