// scripts/ui/CraftGrid.cs
using Godot;
using CardSurvival.Data;
using System.Collections.Generic;

namespace CardSurvival.UI;

public partial class CraftGrid : VBoxContainer
{
	[Signal] public delegate void OnCraftRequestedEventHandler();

	private const int SlotCount = 4;
	private readonly List<CardData?> _slots = new();
	private readonly List<Panel> _slotPanels = new();
	private readonly List<Label> _slotLabels = new();
	private HBoxContainer _slotsRow = null!;
	private Button _craftButton = null!;
	private Label _resultLabel = null!;

	public override void _Ready()
	{
		AddThemeConstantOverride("separation", 4);

		var titleLabel = new Label();
		titleLabel.Text = "合成图表";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeFontSizeOverride("font_size", 12);
		titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
		AddChild(titleLabel);

		_slotsRow = new HBoxContainer();
		_slotsRow.Alignment = BoxContainer.AlignmentMode.Center;
		_slotsRow.AddThemeConstantOverride("separation", 4);
		AddChild(_slotsRow);

		for (int i = 0; i < SlotCount; i++)
		{
			_slots.Add(null);

			var slotBg = new Panel();
			slotBg.CustomMinimumSize = new Vector2(70, 55);
			slotBg.MouseFilter = MouseFilterEnum.Stop;
			slotBg.Name = $"Slot{i}";
			_slotsRow.AddChild(slotBg);
			_slotPanels.Add(slotBg);

			var label = new Label();
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			label.AddThemeFontSizeOverride("font_size", 11);
			label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			label.Text = "空";
			slotBg.AddChild(label);
			label.SetAnchorsPreset(LayoutPreset.FullRect);
			_slotLabels.Add(label);
		}

		var plusLabel = new Label();
		plusLabel.Text = "→";
		plusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		plusLabel.AddThemeFontSizeOverride("font_size", 16);
		plusLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.5f));
		_slotsRow.AddChild(plusLabel);

		var resultSlot = new Panel();
		resultSlot.CustomMinimumSize = new Vector2(70, 55);
		resultSlot.AddThemeColorOverride("panel", new Color(0.2f, 0.18f, 0.12f));
		_slotsRow.AddChild(resultSlot);

		_resultLabel = new Label();
		_resultLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_resultLabel.VerticalAlignment = VerticalAlignment.Center;
		_resultLabel.AddThemeFontSizeOverride("font_size", 11);
		_resultLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		_resultLabel.Text = "?";
		resultSlot.AddChild(_resultLabel);
		_resultLabel.SetAnchorsPreset(LayoutPreset.FullRect);

		_craftButton = new Button();
		_craftButton.Text = "合成";
		_craftButton.Disabled = true;
		_craftButton.CustomMinimumSize = new Vector2(80, 28);
		_craftButton.AddThemeFontSizeOverride("font_size", 13);
		_craftButton.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
		_craftButton.AddThemeColorOverride("bg_color", new Color(0.35f, 0.28f, 0.15f));
		_craftButton.AddThemeColorOverride("border_color", new Color(0.6f, 0.5f, 0.3f));
		AddChild(_craftButton);

		_craftButton.Pressed += () => EmitSignal(SignalName.OnCraftRequested);
	}

	public bool AddCard(CardData card)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == null)
			{
				_slots[i] = card;
				_slotLabels[i].Text = card.Name;
				_slotLabels[i].AddThemeColorOverride("font_color", Colors.White);
				_slotPanels[i].AddThemeColorOverride("panel", new Color(0.25f, 0.2f, 0.12f));
				UpdateCraftButton();
				return true;
			}
		}
		return false;
	}

	public void RemoveCardAt(int index)
	{
		if (index < 0 || index >= SlotCount) return;
		_slots[index] = null;
		_slotLabels[index].Text = "空";
		_slotLabels[index].AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		_slotPanels[index].AddThemeColorOverride("panel", new Color(0.15f, 0.13f, 0.1f));
		UpdateCraftButton();
	}

	public void ClearSlots()
	{
		for (int i = 0; i < SlotCount; i++)
		{
			_slots[i] = null;
			_slotLabels[i].Text = "空";
			_slotLabels[i].AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			_slotPanels[i].AddThemeColorOverride("panel", new Color(0.15f, 0.13f, 0.1f));
		}
		_resultLabel.Text = "?";
		_resultLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		UpdateCraftButton();
	}

	public List<CardData> GetSlots()
	{
		var result = new List<CardData>();
		foreach (var slot in _slots)
		{
			if (slot != null)
				result.Add(slot);
		}
		return result;
	}

	public int GetFilledSlotCount()
	{
		int count = 0;
		foreach (var slot in _slots)
		{
			if (slot != null) count++;
		}
		return count;
	}

	public void SetResultPreview(string text)
	{
		_resultLabel.Text = text;
		_resultLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
	}

	public void ClearResultPreview()
	{
		_resultLabel.Text = "?";
		_resultLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
	}

	private void UpdateCraftButton()
	{
		_craftButton.Disabled = GetFilledSlotCount() < 2;
	}

	public int GetSlotIndexAtPosition(Vector2 globalPos)
	{
		for (int i = 0; i < _slotPanels.Count; i++)
		{
			var rect = _slotPanels[i].GetGlobalRect();
			if (rect.HasPoint(globalPos))
				return i;
		}
		return -1;
	}

	public bool IsOverGrid(Vector2 globalPos)
	{
		foreach (var panel in _slotPanels)
		{
			if (panel.GetGlobalRect().HasPoint(globalPos))
				return true;
		}
		return false;
	}
}
