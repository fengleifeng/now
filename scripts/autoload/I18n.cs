using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CardSurvival.Data;
using CardSurvival.Game;
using Godot;

/// <summary>
/// 国际化：从 <see cref="ContentPaths.GameStrings"/> 加载 TSV（key、zh_CN、en），注册到 <see cref="TranslationServer"/>。
/// </summary>
public partial class I18n : Node
{
	public const string DefaultLocale = "zh_CN";
	public const string EnglishLocale = "en";

	public static I18n? Instance { get; private set; }

	/// <summary>在 <see cref="ApplyLocale"/> 之后触发；动态构建的 UI 可在此刷新文案。</summary>
	public static event Action? LocaleChanged;

	private readonly Dictionary<string, string> _fallbackZh = new();

	public override void _Ready()
	{
		Instance = this;
		LoadTable();
		var settings = GetNodeOrNull<GameSettings>("/root/GameSettings");
		ApplyLocale(settings?.UiLocale ?? DefaultLocale);
	}

	/// <summary>切换语言并写入 TranslationServer；由菜单或设置调用。</summary>
	public void ApplyLocale(string locale)
	{
		var normalized = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim();
		if (normalized != DefaultLocale && normalized != EnglishLocale)
			normalized = DefaultLocale;
		TranslationServer.SetLocale(normalized);
		LocaleChanged?.Invoke();
	}

	public static string T(string key)
	{
		var raw = TranslationServer.Translate(key).ToString();
		if (!string.IsNullOrEmpty(raw) && raw != key)
			return raw;
		if (Instance != null && Instance._fallbackZh.TryGetValue(key, out var zh))
			return zh;
		return key;
	}

	public static string Tf(string key, params object[] args) =>
		args.Length == 0 ? T(key) : string.Format(CultureInfo.InvariantCulture, T(key), args);

	public static string CardTypeName(CardType t) =>
		T(t switch
		{
			CardType.Resource => "cardtype.resource",
			CardType.Creature => "cardtype.creature",
			CardType.Tool => "cardtype.tool",
			CardType.Weapon => "cardtype.weapon",
			CardType.Building => "cardtype.building",
			CardType.Status => "cardtype.status",
			CardType.Event => "cardtype.event",
			CardType.Location => "cardtype.location",
			CardType.Container => "cardtype.container",
			CardType.Seed => "cardtype.seed",
			_ => "cardtype.generic"
		});

	private readonly List<Translation> _registered = new();

	private void LoadTable()
	{
		_fallbackZh.Clear();
		foreach (var t in _registered)
			TranslationServer.RemoveTranslation(t);
		_registered.Clear();

		var path = ProjectSettings.GlobalizePath(ContentPaths.GameStrings);
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"[I18n] Missing locale file: {ContentPaths.GameStrings}");
			return;
		}

		using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (!f.IsOpen())
		{
			GD.PrintErr($"[I18n] Cannot open: {path}");
			return;
		}

		var text = f.GetAsText();
		var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (lines.Length < 2)
		{
			GD.PrintErr("[I18n] Locale table too short");
			return;
		}

		var header = SplitTsv(lines[0]);
		if (header.Length < 3 || !string.Equals(header[0].Trim(), "key", StringComparison.OrdinalIgnoreCase))
		{
			GD.PrintErr("[I18n] First row must be: key<TAB>zh_CN<TAB>en");
			return;
		}

		var zhCol = 1;
		var enCol = 2;
		var zhTr = new Translation { Locale = DefaultLocale };
		var enTr = new Translation { Locale = EnglishLocale };

		for (var i = 1; i < lines.Length; i++)
		{
			var line = lines[i].Trim();
			if (line.Length == 0 || line.StartsWith('#'))
				continue;
			var cols = SplitTsv(line);
			if (cols.Length < 3) continue;
			var key = cols[0].Trim();
			if (key.Length == 0) continue;
			var zh = Unescape(cols[zhCol]);
			var en = Unescape(cols[enCol]);
			_fallbackZh[key] = zh;
			zhTr.AddMessage(key, zh);
			enTr.AddMessage(key, en);
		}

		TranslationServer.AddTranslation(zhTr);
		TranslationServer.AddTranslation(enTr);
		_registered.Add(zhTr);
		_registered.Add(enTr);
		GD.Print($"[I18n] Loaded {_fallbackZh.Count} keys");
	}

	private static string[] SplitTsv(string line)
	{
		var parts = line.Split('\t');
		for (var i = 0; i < parts.Length; i++)
			parts[i] = parts[i].Trim();
		return parts;
	}

	private static string Unescape(string s)
	{
		if (s.IndexOf('\\') < 0) return s;
		var sb = new StringBuilder(s.Length);
		for (var i = 0; i < s.Length; i++)
		{
			if (s[i] == '\\' && i + 1 < s.Length)
			{
				switch (s[i + 1])
				{
					case 'n':
						sb.Append('\n');
						i++;
						continue;
					case 't':
						sb.Append('\t');
						i++;
						continue;
					case '\\':
						sb.Append('\\');
						i++;
						continue;
				}
			}

			sb.Append(s[i]);
		}

		return sb.ToString();
	}
}
