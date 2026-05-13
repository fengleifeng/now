using Godot;

namespace CardSurvival.UI;

/// <summary>
/// 与策划文档第十二章配色对齐的 UI 常量与样式应用（深蓝灰荒野风）。
/// </summary>
public static class GameTheme
{
	public static Color BgDeep => new(15f / 255f, 20f / 255f, 26f / 255f);
	public static Color PanelSidebar => new(22f / 255f, 26f / 255f, 32f / 255f);
	public static Color PanelMain => new(30f / 255f, 34f / 255f, 40f / 255f);
	public static Color PanelElevated => new(36f / 255f, 40f / 255f, 48f / 255f);
	public static Color Separator => new(77f / 255f, 71f / 255f, 64f / 255f);
	public static Color TextPrimary => new(204f / 255f, 204f / 255f, 204f / 255f);
	public static Color TextMuted => new(0.62f, 0.62f, 0.62f);
	public static Color AccentRegion => new(179f / 255f, 217f / 255f, 179f / 255f);
	public static Color AccentScene => new(217f / 255f, 204f / 255f, 153f / 255f);
	public static Color AccentHand => new(217f / 255f, 204f / 255f, 179f / 255f);

	public static Color BtnBrown => new(64f / 255f, 51f / 255f, 38f / 255f);
	public static Color BtnBrownHover => new(82f / 255f, 68f / 255f, 52f / 255f);
	public static Color BtnExplore => new(38f / 255f, 64f / 255f, 38f / 255f);
	public static Color BtnExploreHover => new(48f / 255f, 82f / 255f, 48f / 255f);
	public static Color BtnMove => new(38f / 255f, 38f / 255f, 64f / 255f);
	public static Color BtnMoveHover => new(52f / 255f, 52f / 255f, 88f / 255f);
	public static Color BtnBorder => new(128f / 255f, 115f / 255f, 89f / 255f);

	public static Color StatHigh => new(77f / 255f, 204f / 255f, 77f / 255f);
	public static Color StatMid => new(230f / 255f, 179f / 255f, 77f / 255f);
	public static Color StatLow => new(230f / 255f, 77f / 255f, 77f / 255f);
	public static Color EnergyFill => new(77f / 255f, 153f / 255f, 230f / 255f);
	public static Color SanityFill => new(179f / 255f, 102f / 255f, 204f / 255f);

	private static StyleBoxFlat Box(Color bg, Color? border = null, int radius = 4)
	{
		var s = new StyleBoxFlat { BgColor = bg, CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius };
		if (border.HasValue)
		{
			s.BorderColor = border.Value;
			s.SetBorderWidthAll(1);
		}
		return s;
	}

	public static void ApplyPanel(PanelContainer panel, Color bg)
	{
		panel.AddThemeStyleboxOverride("panel", Box(bg, Separator, 0));
	}

	public static void ApplyPanelSoft(PanelContainer panel, Color bg)
	{
		panel.AddThemeStyleboxOverride("panel", Box(bg, new Color(Separator.R, Separator.G, Separator.B, 0.45f), 4));
	}

	/// <summary>弹窗面板：略高对比、圆角，便于与主界面区分。</summary>
	public static void ApplyModalPanel(PanelContainer panel)
	{
		var border = new Color(Separator.R, Separator.G, Separator.B, 0.75f);
		var s = new StyleBoxFlat
		{
			BgColor = PanelElevated,
			BorderColor = border,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10
		};
		s.SetBorderWidthAll(2);
		panel.AddThemeStyleboxOverride("panel", s);
	}

	public static void StyleSidebarButton(Button b)
	{
		var n = Box(BtnBrown, BtnBorder, 4);
		var h = Box(BtnBrownHover, BtnBorder, 4);
		var p = Box(new Color(BtnBrown.R * 0.82f, BtnBrown.G * 0.82f, BtnBrown.B * 0.82f), BtnBorder, 4);
		b.AddThemeStyleboxOverride("normal", n);
		b.AddThemeStyleboxOverride("hover", h);
		b.AddThemeStyleboxOverride("pressed", p);
		b.AddThemeStyleboxOverride("disabled", Box(new Color(BtnBrown.R * 0.55f, BtnBrown.G * 0.55f, BtnBrown.B * 0.55f), BtnBorder, 4));
		b.AddThemeColorOverride("font_color", new Color(230f / 255f, 230f / 255f, 230f / 255f));
		b.AddThemeColorOverride("font_disabled_color", new Color(0.45f, 0.45f, 0.45f));
	}

	public static void StyleExploreButton(Button b)
	{
		var n = Box(BtnExplore, BtnBorder, 4);
		var h = Box(BtnExploreHover, BtnBorder, 4);
		b.AddThemeStyleboxOverride("normal", n);
		b.AddThemeStyleboxOverride("hover", h);
		b.AddThemeStyleboxOverride("pressed", Box(new Color(BtnExplore.R * 0.82f, BtnExplore.G * 0.82f, BtnExplore.B * 0.82f), BtnBorder, 4));
		b.AddThemeColorOverride("font_color", Colors.White);
	}

	public static void StyleMoveButton(Button b)
	{
		var n = Box(BtnMove, BtnBorder, 4);
		var h = Box(BtnMoveHover, BtnBorder, 4);
		b.AddThemeStyleboxOverride("normal", n);
		b.AddThemeStyleboxOverride("hover", h);
		b.AddThemeStyleboxOverride("pressed", Box(new Color(BtnMove.R * 0.82f, BtnMove.G * 0.82f, BtnMove.B * 0.82f), BtnBorder, 4));
		b.AddThemeColorOverride("font_color", Colors.White);
	}

	public static void StyleSectionLabel(Label label, Color? color = null)
	{
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", color ?? AccentScene);
	}

	public static void StyleRichLog(RichTextLabel log)
	{
		log.AddThemeFontSizeOverride("normal_font_size", 12);
		log.AddThemeColorOverride("default_color", TextPrimary);
	}

	public static Color ProgressFillForRatio(float ratio01, bool energy = false, bool sanity = false)
	{
		if (energy) return EnergyFill;
		if (sanity) return SanityFill;
		if (ratio01 > 0.6f) return StatHigh;
		if (ratio01 > 0.3f) return StatMid;
		return StatLow;
	}
}
