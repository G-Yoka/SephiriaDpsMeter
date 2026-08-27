using System;
using System.Globalization;

namespace SephiriaDpsMeter
{
    internal enum TextKey
    {
        IdentifyingRoom, RoomSummary, WaitingForRoom,
        Locating, Recording, Complete, Idle,
        TeamDamage, TeamDps, RoomTime, RunTime,
        PlayerShare, TotalDamage, Hits,
        UnknownRoomHint, WaitingForDamageHint, EnterRoomHint,
        SettingsTitle, ShowPanel, LockPosition, PanelOpacity, PanelScale,
        DamageShareSuffix, PlayerFallback
    }

    // Independent of Unity so language selection and every label can be tested.
    internal sealed class MeterLocalization
    {
        internal bool IsChinese { get; private set; }

        internal void SetLanguage(string gameLanguage)
        {
            // The game's shipped Simplified Chinese locale is zh-CN.
            // Traditional Chinese, unknown and not-yet-initialized locales use English.
            IsChinese = string.Equals(gameLanguage, "zh-CN", StringComparison.OrdinalIgnoreCase);
        }

        internal string this[TextKey key]
        {
            get
            {
                switch (key)
                {
                    case TextKey.IdentifyingRoom: return IsChinese ? "等待识别当前战斗房间" : "Identifying battle room";
                    case TextKey.RoomSummary: return IsChinese ? "房间  #{0} · 当前房间统计" : "Room #{0} · This room";
                    case TextKey.WaitingForRoom: return IsChinese ? "等待进入战斗房间" : "Waiting for a battle room";
                    case TextKey.Locating: return IsChinese ? "○  房间识别中" : "○  Locating";
                    case TextKey.Recording: return IsChinese ? "●  统计中" : "●  Recording";
                    case TextKey.Complete: return IsChinese ? "◆  房间结算" : "◆  Complete";
                    case TextKey.Idle: return IsChinese ? "○  待机" : "○  Idle";
                    case TextKey.TeamDamage: return IsChinese ? "团队伤害" : "Team damage";
                    case TextKey.TeamDps: return IsChinese ? "团队 DPS" : "Team DPS";
                    case TextKey.RoomTime: return IsChinese ? "房间用时" : "Room time";
                    case TextKey.RunTime: return IsChinese ? "本局用时" : "Run time";
                    case TextKey.PlayerShare: return IsChinese ? "玩家 / 伤害占比" : "Player / Damage share";
                    case TextKey.TotalDamage: return IsChinese ? "总伤害" : "Damage";
                    case TextKey.Hits: return IsChinese ? "命中" : "Hits";
                    case TextKey.UnknownRoomHint: return IsChinese ? "房间信息未就绪，暂不计入伤害" : "Room unknown — damage recording paused";
                    case TextKey.WaitingForDamageHint: return IsChinese ? "房间已开始，等待造成伤害…" : "Battle started. Waiting for damage…";
                    case TextKey.EnterRoomHint: return IsChinese ? "进入战斗房间后自动开始统计" : "Recording starts when you enter a battle room";
                    case TextKey.SettingsTitle: return IsChinese ? "DPS 面板设置" : "DPS Settings";
                    case TextKey.ShowPanel: return IsChinese ? "显示 DPS 面板" : "Show DPS panel";
                    case TextKey.LockPosition: return IsChinese ? "固定面板位置" : "Lock panel position";
                    case TextKey.PanelOpacity: return IsChinese ? "面板不透明度" : "Panel opacity";
                    case TextKey.PanelScale: return IsChinese ? "DPS 面板缩放" : "DPS panel scale";
                    case TextKey.DamageShareSuffix: return IsChinese ? "% 伤害占比" : "% of damage";
                    case TextKey.PlayerFallback: return IsChinese ? "玩家 {0}" : "Player {0}";
                    default: throw new ArgumentOutOfRangeException("key");
                }
            }
        }

        internal string RoomSummary(int sequence)
        {
            return string.Format(CultureInfo.InvariantCulture, this[TextKey.RoomSummary], sequence);
        }

        internal string PlayerName(string name, uint id)
        {
            return string.IsNullOrEmpty(name)
                ? string.Format(CultureInfo.InvariantCulture, this[TextKey.PlayerFallback], id)
                : name;
        }
    }
}
