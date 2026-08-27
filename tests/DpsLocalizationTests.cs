using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using SephiriaDpsMeter;

internal static class DpsLocalizationTests
{
    private static int passed;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static void Main()
    {
        MeterLocalization text = new MeterLocalization();
        Check(!text.IsChinese && text[TextKey.SettingsTitle] == "DPS Settings", "English before game locale is available");
        foreach (string locale in new[] { "zh-CN", "zh-cn", "ZH-CN" })
        {
            text.SetLanguage(locale);
            Check(text.IsChinese && text[TextKey.Hits] == "命中", "Simplified Chinese: " + locale);
        }
        foreach (string locale in new[] { null, "", "zh-TW", "zh-HK", "zh", "zh-Hant", "zh-Hans",
            "en-US", "ko-KR", "ja-JP", "de-DE", "es-ES", "fr-FR", "it-IT", "pl-PL", "pt-BR",
            "ru-RU", "sv-SE", "th-TH", "tr-TR", "workshop-locale", "unknown" })
        {
            text.SetLanguage(locale);
            Check(!text.IsChinese && text[TextKey.Hits] == "Hits", "English fallback: " + (locale ?? "<null>"));
        }

        foreach (TextKey key in Enum.GetValues(typeof(TextKey)))
        {
            text.SetLanguage("zh-CN");
            string chinese = text[key];
            text.SetLanguage("en-US");
            string english = text[key];
            Check(!string.IsNullOrEmpty(chinese) && Regex.IsMatch(chinese, @"[\u4e00-\u9fff]"), "Chinese translation: " + key);
            Check(!string.IsNullOrEmpty(english) && !Regex.IsMatch(english, @"[\u4e00-\u9fff]"), "English translation: " + key);
        }

        text.SetLanguage("zh-CN");
        Check(text.RoomSummary(28) == "房间  #28 · 当前房间统计", "Chinese room number");
        Check(text.PlayerName(null, 42) == "玩家 42", "Chinese unnamed player");
        text.SetLanguage("en-US");
        Check(text.RoomSummary(28) == "Room #28 · This room", "English room number after live switch");
        Check(text.PlayerName(null, 42) == "Player 42", "Unnamed player updates after live switch");
        Check(text.PlayerName("", 42) == "Player 42", "Empty player name fallback");
        Check(text.PlayerName("夜华", 42) == "夜华", "Chinese player names preserved in English UI");
        Check(text.PlayerName("Alice", 42) == "Alice", "English player names preserved");
        text.SetLanguage("zh-CN");
        Check(text[TextKey.SettingsTitle] == "DPS 面板设置", "Switch back to Chinese");
        text.SetLanguage("zh-TW");
        Check(text[TextKey.SettingsTitle] == "DPS Settings", "Traditional Chinese switches to English");

        CultureInfo previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
            text.SetLanguage("ko-KR");
            Check(!text.IsChinese, "Game language takes precedence over Chinese OS culture");
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            text.SetLanguage("zh-CN");
            Check(text.IsChinese, "Game language takes precedence over English OS culture");
        }
        finally { Thread.CurrentThread.CurrentCulture = previous; }

        Console.WriteLine("Localization tests passed: " + passed);
    }
}
