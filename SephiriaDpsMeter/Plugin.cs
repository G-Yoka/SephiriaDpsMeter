using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaDpsMeter
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.sephiriamods.dpsmeter";
        public const string PluginName = "Sephiria Multiplayer DPS Meter";
        public const string PluginVersion = "1.4.2";

        private const float MeterWidth = 440f;

        internal static Plugin Instance;
        internal static ManualLogSource Log;

        private ConfigEntry<bool> meterEnabled;
        private ConfigEntry<Key> toggleKey;
        private ConfigEntry<bool> countShieldDamage;
        private ConfigEntry<bool> lockWindowPosition;
        private ConfigEntry<float> panelOpacity;
        private ConfigEntry<float> panelScale;
        private ConfigEntry<float> windowX;
        private ConfigEntry<float> windowY;

        private Harmony harmony;
        private Rect windowRect;
        private Rect settingsRect;
        private Vector2 scrollPosition;
        private bool wasVisible;
        private bool settingsVisible;
        private bool opacityDragging;
        private bool scaleDragging;
        private bool systemCursorOverridden;
        private bool savedSystemCursorVisible;
        private CursorLockMode savedSystemCursorLockMode;

        private readonly Dictionary<uint, PlayerDamage> damageByPlayer = new Dictionary<uint, PlayerDamage>();
        private bool battleStateKnown;
        private bool observedBattleState;
        private bool roomActive;
        private bool hasRoomResult;
        private bool runStateKnown;
        private bool observedRunStarted;
        private int roomSequence;
        private float roomStartedAt = -1f;
        private float roomEndedAt = -1f;
        private float displayedRunElapsed;

        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusStyle;
        private GUIStyle metricLabelStyle;
        private GUIStyle metricValueStyle;
        private GUIStyle columnStyle;
        private GUIStyle columnCenterStyle;
        private GUIStyle rankStyle;
        private GUIStyle nameStyle;
        private GUIStyle detailStyle;
        private GUIStyle damageStyle;
        private GUIStyle dpsStyle;
        private GUIStyle centerStyle;
        private GUIStyle closeStyle;
        private GUIStyle authorStyle;
        private Texture2D transparentTexture;
        private float currentMeterHeight = 190f;

        private static readonly Color PanelColor = new Color(0.045f, 0.055f, 0.075f, 0.96f);
        private static readonly Color CardColor = new Color(0.085f, 0.10f, 0.135f, 0.96f);
        private static readonly Color CardAltColor = new Color(0.07f, 0.085f, 0.115f, 0.96f);
        private static readonly Color MutedColor = new Color(0.58f, 0.64f, 0.72f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.95f, 1f, 1f);
        private static readonly Color AccentColor = new Color(0.25f, 0.72f, 1f, 1f);
        private static readonly Color ActiveColor = new Color(0.25f, 0.93f, 0.62f, 1f);
        private static readonly Color FinishedColor = new Color(1f, 0.73f, 0.25f, 1f);

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            meterEnabled = Config.Bind("Interface", "Visible", true,
                "Show the multiplayer DPS meter.");
            toggleKey = Config.Bind("Interface", "ToggleKey", Key.F9,
                "Key used to show or hide the DPS meter.");
            windowX = Config.Bind("Interface", "WindowX", 20f,
                "DPS meter horizontal position in pixels.");
            windowY = Config.Bind("Interface", "WindowY", 120f,
                "DPS meter vertical position in pixels.");
            countShieldDamage = Config.Bind("Statistics", "CountShieldDamage", true,
                "Include damage absorbed by normal and MP shields.");
            lockWindowPosition = Config.Bind("Interface", "LockWindowPosition", false,
                "Prevent the DPS panel from being dragged with the mouse.");
            panelOpacity = Config.Bind("Interface", "PanelOpacity", 0.92f,
                "DPS panel background opacity, from 0.25 to 1.0.");
            panelScale = Config.Bind("Interface", "PanelScale", 1f,
                "DPS panel scale, from 0.60 to 1.20.");

            panelScale.Value = Mathf.Clamp(panelScale.Value, 0.60f, 1.20f);
            windowRect = new Rect(windowX.Value, windowY.Value, MeterWidth, currentMeterHeight);
            settingsRect = new Rect(windowX.Value + 452f, windowY.Value, 300f, 264f);
            wasVisible = meterEnabled.Value;

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(PluginName + " v" + PluginVersion + " loaded. Room lifecycle mode, toggle: " + toggleKey.Value);
        }

        private void OnDestroy()
        {
            if (harmony != null)
                harmony.UnpatchSelf();

            SaveWindowPosition();
            RestoreSystemCursor();
            if (transparentTexture != null)
                Destroy(transparentTexture);
            Instance = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey.Value].wasPressedThisFrame)
                SetSettingsVisible(!settingsVisible);

            PollRoomLifecycle();
            PollRunLifecycle();

            if (wasVisible && !meterEnabled.Value)
                SaveWindowPosition();
            wasVisible = meterEnabled.Value;
        }

        private void PollRoomLifecycle()
        {
            PlayerAvatar player = GetCurrentPlayer();
            if (player == null)
            {
                if (battleStateKnown && roomActive)
                    EndRoom();
                battleStateKnown = false;
                return;
            }

            bool inBattle = player.IsInBattle;
            if (!battleStateKnown)
            {
                battleStateKnown = true;
                observedBattleState = inBattle;
                if (inBattle)
                    BeginRoom();
                return;
            }

            if (inBattle == observedBattleState)
                return;

            observedBattleState = inBattle;
            if (inBattle)
                BeginRoom();
            else
                EndRoom();
        }

        private static PlayerAvatar GetCurrentPlayer()
        {
            CombatManager manager = CombatManager.Instance;
            return manager != null ? manager.CurrentPlayer : null;
        }

        private void PollRunLifecycle()
        {
            DungeonManager manager = DungeonManager.Instance;
            if (manager == null)
            {
                runStateKnown = false;
                return;
            }

            bool runStarted = manager.NetworkisRunStarted;
            float gameElapsed = Mathf.Max(0f, manager.playedRealtimeClientside);
            if (!runStateKnown)
            {
                runStateKnown = true;
                observedRunStarted = runStarted;
                displayedRunElapsed = gameElapsed;
                return;
            }

            if (runStarted)
                displayedRunElapsed = gameElapsed;
            else if (observedRunStarted)
                displayedRunElapsed = gameElapsed;

            observedRunStarted = runStarted;
        }

        private void BeginRoom()
        {
            damageByPlayer.Clear();
            scrollPosition = Vector2.zero;
            roomSequence++;
            roomStartedAt = Time.realtimeSinceStartup;
            roomEndedAt = -1f;
            roomActive = true;
            hasRoomResult = true;
            Logger.LogDebug("DPS room #" + roomSequence + " started.");
        }

        private void EndRoom()
        {
            if (!roomActive)
                return;
            roomEndedAt = Time.realtimeSinceStartup;
            roomActive = false;
            Logger.LogDebug("DPS room #" + roomSequence + " ended: " + damageByPlayer.Count + " player rows.");
        }

        private void OnGUI()
        {
            if (!meterEnabled.Value && !settingsVisible)
                return;

            EnsureStyles();
            if (meterEnabled.Value)
            {
                int visibleRows = Mathf.Min(Mathf.Max(damageByPlayer.Count, 1), 6);
                currentMeterHeight = damageByPlayer.Count == 0 ? 190f : 158f + visibleRows * 48f;
                float scale = Mathf.Clamp(panelScale.Value, 0.60f, 1.20f);
                Rect logicalRect = new Rect(windowRect.x / scale, windowRect.y / scale, MeterWidth, currentMeterHeight);
                Matrix4x4 previousMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                logicalRect = GUI.Window(887421, logicalRect, DrawWindow, string.Empty, windowStyle);
                GUI.matrix = previousMatrix;

                windowRect = new Rect(logicalRect.x * scale, logicalRect.y * scale,
                    MeterWidth * scale, currentMeterHeight * scale);
                windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
                windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - 32f));
            }

            if (settingsVisible)
            {
                settingsRect = GUI.Window(887422, settingsRect, DrawSettingsWindow, string.Empty, windowStyle);
                settingsRect.x = Mathf.Clamp(settingsRect.x, 0f, Mathf.Max(0f, Screen.width - settingsRect.width));
                settingsRect.y = Mathf.Clamp(settingsRect.y, 0f, Mathf.Max(0f, Screen.height - 32f));
            }
        }

        private void DrawWindow(int id)
        {
            DrawRect(new Rect(0f, 0f, MeterWidth, currentMeterHeight), PanelColor);
            DrawRect(new Rect(0f, 0f, MeterWidth, 3f), AccentColor);
            Color border = new Color(0.2f, 0.32f, 0.45f, 0.8f);
            DrawRect(new Rect(0f, currentMeterHeight - 1f, MeterWidth, 1f), border);
            DrawRect(new Rect(0f, 0f, 1f, currentMeterHeight), border);
            DrawRect(new Rect(MeterWidth - 1f, 0f, 1f, currentMeterHeight), border);

            GUI.Label(new Rect(16f, 11f, 155f, 24f), "DPS METER", titleStyle);
            string roomText = hasRoomResult ? "房间  #" + roomSequence : "等待进入战斗房间";
            GUI.Label(new Rect(16f, 32f, 210f, 19f), roomText, subtitleStyle);

            string stateText;
            Color stateColor;
            if (roomActive)
            {
                stateText = "●  统计中";
                stateColor = ActiveColor;
            }
            else if (hasRoomResult)
            {
                stateText = "◆  房间结算";
                stateColor = FinishedColor;
            }
            else
            {
                stateText = "○  待机";
                stateColor = MutedColor;
            }
            statusStyle.normal.textColor = stateColor;
            GUI.Label(new Rect(318f, 17f, 104f, 24f), stateText, statusStyle);

            List<PlayerDamage> rows = GetSortedRows();
            long groupTotal = GetGroupTotal(rows);
            float elapsed = GetRoomElapsed();
            double groupDps = elapsed > 0.05f ? groupTotal / elapsed : 0.0;

            DrawRect(new Rect(12f, 58f, 416f, 48f), CardColor);
            float runElapsed = GetRunElapsed();
            DrawMetric(22f, 88f, "团队伤害", FormatNumber(groupTotal));
            DrawRect(new Rect(116f, 68f, 1f, 28f), new Color(0.25f, 0.30f, 0.38f, 0.65f));
            DrawMetric(127f, 88f, "团队 DPS", FormatNumber((long)Math.Round(groupDps)));
            DrawRect(new Rect(220f, 68f, 1f, 28f), new Color(0.25f, 0.30f, 0.38f, 0.65f));
            DrawMetric(231f, 84f, "房间用时", FormatTime(elapsed));
            DrawRect(new Rect(324f, 68f, 1f, 28f), new Color(0.25f, 0.30f, 0.38f, 0.65f));
            DrawMetric(335f, 88f, "本局用时", FormatRunTime(runElapsed));

            float tableContentWidth = rows.Count > 6 ? 400f : 416f;
            GUI.Label(new Rect(16f, 112f, 190f, 20f), "玩家 / 伤害占比", columnStyle);
            GUI.Label(new Rect(216f, 112f, 88f, 20f), "总伤害", columnCenterStyle);
            GUI.Label(new Rect(304f, 112f, 76f, 20f), "DPS", columnCenterStyle);
            GUI.Label(new Rect(380f, 112f, tableContentWidth - 368f, 20f), "命中", columnCenterStyle);

            Rect listRect = new Rect(12f, 135f, 416f, currentMeterHeight - 147f);
            if (rows.Count == 0)
            {
                string emptyText = roomActive ? "房间已开始，等待造成伤害…" : "进入战斗房间后自动开始统计";
                GUI.Label(new Rect(20f, 137f, 400f, 34f), emptyText, centerStyle);
            }
            else
            {
                bool hasScrollbar = rows.Count > 6;
                float rowWidth = tableContentWidth;
                Rect contentRect = new Rect(0f, 0f, rowWidth, rows.Count * 48f);
                scrollPosition = GUI.BeginScrollView(listRect, scrollPosition, contentRect, false, hasScrollbar);
                long leaderDamage = Math.Max(1L, rows[0].TotalDamage);
                for (int i = 0; i < rows.Count; i++)
                    DrawPlayerRow(new Rect(0f, i * 48f, rowWidth, 44f), rows[i], groupTotal, leaderDamage, elapsed, i + 1);
                GUI.EndScrollView();
            }

            if (!lockWindowPosition.Value)
                GUI.DragWindow(new Rect(0f, 0f, 310f, 54f));
        }

        private void DrawSettingsWindow(int id)
        {
            DrawSolidRect(new Rect(0f, 0f, settingsRect.width, settingsRect.height), new Color(0.045f, 0.055f, 0.075f, 0.75f));
            DrawSolidRect(new Rect(0f, 0f, settingsRect.width, 3f), AccentColor);
            DrawSolidRect(new Rect(0f, settingsRect.height - 1f, settingsRect.width, 1f), new Color(0.2f, 0.32f, 0.45f, 0.9f));

            GUI.Label(new Rect(16f, 12f, 190f, 26f), "DPS 面板设置", titleStyle);
            GUI.Label(new Rect(150f, 16f, 90f, 20f), "by G-Yoka", authorStyle);

            Rect closeRect = new Rect(260f, 10f, 26f, 26f);
            bool closeHovered = closeRect.Contains(Event.current.mousePosition);
            DrawSolidRect(closeRect, closeHovered
                ? new Color(0.78f, 0.25f, 0.32f, 0.95f)
                : new Color(0.10f, 0.14f, 0.19f, 1f));
            DrawSolidRect(new Rect(closeRect.x, closeRect.yMax - 2f, closeRect.width, 2f),
                closeHovered ? new Color(1f, 0.45f, 0.50f, 1f) : AccentColor);
            GUI.Label(closeRect, "×", closeStyle);
            if (GUI.Button(closeRect, string.Empty, GUIStyle.none))
                SetSettingsVisible(false);

            DrawSolidRect(new Rect(12f, 48f, 276f, 42f), CardColor);
            if (GUI.Button(new Rect(12f, 48f, 276f, 42f), string.Empty, GUIStyle.none))
                meterEnabled.Value = !meterEnabled.Value;
            GUI.Label(new Rect(24f, 58f, 190f, 22f), "显示 DPS 面板", nameStyle);
            DrawToggleIndicator(new Rect(246f, 59f, 28f, 20f), meterEnabled.Value);

            DrawSolidRect(new Rect(12f, 96f, 276f, 42f), CardAltColor);
            if (GUI.Button(new Rect(12f, 96f, 276f, 42f), string.Empty, GUIStyle.none))
                lockWindowPosition.Value = !lockWindowPosition.Value;
            GUI.Label(new Rect(24f, 106f, 190f, 22f), "固定面板位置", nameStyle);
            DrawToggleIndicator(new Rect(246f, 107f, 28f, 20f), lockWindowPosition.Value);

            GUI.Label(new Rect(20f, 150f, 150f, 20f), "面板不透明度", columnStyle);
            GUI.Label(new Rect(226f, 150f, 54f, 20f), Mathf.RoundToInt(panelOpacity.Value * 100f) + "%", columnCenterStyle);
            DrawOpacitySlider(new Rect(20f, 177f, 260f, 18f));

            GUI.Label(new Rect(20f, 202f, 150f, 20f), "DPS 面板缩放", columnStyle);
            GUI.Label(new Rect(226f, 202f, 54f, 20f), Mathf.RoundToInt(panelScale.Value * 100f) + "%", columnCenterStyle);
            DrawScaleSlider(new Rect(20f, 229f, 260f, 18f));

            GUI.DragWindow(new Rect(0f, 0f, 245f, 43f));
        }

        private void DrawOpacitySlider(Rect rect)
        {
            Rect interactionRect = new Rect(rect.x, rect.y - 6f, rect.width, rect.height + 12f);
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && interactionRect.Contains(current.mousePosition))
            {
                opacityDragging = true;
                SetOpacityFromMouse(rect, current.mousePosition.x);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && opacityDragging)
            {
                SetOpacityFromMouse(rect, current.mousePosition.x);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0 && opacityDragging)
            {
                SetOpacityFromMouse(rect, current.mousePosition.x);
                opacityDragging = false;
                current.Use();
            }

            float normalized = Mathf.InverseLerp(0.25f, 1f, panelOpacity.Value);
            float trackY = rect.y + 7f;
            DrawSolidRect(new Rect(rect.x, trackY, rect.width, 4f), new Color(0.12f, 0.15f, 0.20f, 1f));
            DrawSolidRect(new Rect(rect.x, trackY, rect.width * normalized, 4f), AccentColor);
            float knobX = rect.x + rect.width * normalized - 6f;
            DrawSolidRect(new Rect(knobX, rect.y + 2f, 12f, 14f), new Color(0.82f, 0.90f, 0.98f, 1f));
            DrawSolidRect(new Rect(knobX + 2f, rect.y + 4f, 8f, 10f), new Color(0.22f, 0.53f, 0.72f, 1f));
        }

        private void SetOpacityFromMouse(Rect rect, float mouseX)
        {
            float normalized = Mathf.Clamp01((mouseX - rect.x) / rect.width);
            panelOpacity.Value = Mathf.Lerp(0.25f, 1f, normalized);
        }

        private void DrawScaleSlider(Rect rect)
        {
            Rect interactionRect = new Rect(rect.x, rect.y - 6f, rect.width, rect.height + 12f);
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && interactionRect.Contains(current.mousePosition))
            {
                scaleDragging = true;
                SetScaleFromMouse(rect, current.mousePosition.x);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && scaleDragging)
            {
                SetScaleFromMouse(rect, current.mousePosition.x);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0 && scaleDragging)
            {
                SetScaleFromMouse(rect, current.mousePosition.x);
                scaleDragging = false;
                current.Use();
            }

            float normalized = Mathf.InverseLerp(0.60f, 1.20f, panelScale.Value);
            float trackY = rect.y + 7f;
            DrawSolidRect(new Rect(rect.x, trackY, rect.width, 4f), new Color(0.12f, 0.15f, 0.20f, 1f));
            DrawSolidRect(new Rect(rect.x, trackY, rect.width * normalized, 4f), AccentColor);
            float knobX = rect.x + rect.width * normalized - 6f;
            DrawSolidRect(new Rect(knobX, rect.y + 2f, 12f, 14f), new Color(0.82f, 0.90f, 0.98f, 1f));
            DrawSolidRect(new Rect(knobX + 2f, rect.y + 4f, 8f, 10f), new Color(0.22f, 0.53f, 0.72f, 1f));
        }

        private void SetScaleFromMouse(Rect rect, float mouseX)
        {
            float oldScale = Mathf.Clamp(panelScale.Value, 0.60f, 1.20f);
            float normalized = Mathf.Clamp01((mouseX - rect.x) / rect.width);
            float newScale = Mathf.Lerp(0.60f, 1.20f, normalized);
            if (Mathf.Abs(oldScale - newScale) < 0.0001f)
                return;

            panelScale.Value = newScale;
        }

        private void DrawToggleIndicator(Rect rect, bool enabled)
        {
            DrawSolidRect(rect, enabled ? new Color(0.18f, 0.65f, 0.48f, 1f) : new Color(0.22f, 0.25f, 0.30f, 1f));
            float knobX = enabled ? rect.x + rect.width - 16f : rect.x + 3f;
            DrawSolidRect(new Rect(knobX, rect.y + 3f, 13f, 14f), Color.white);
        }

        private void DrawMetric(float x, float width, string label, string value)
        {
            GUI.Label(new Rect(x, 62f, width, 17f), label, metricLabelStyle);
            GUI.Label(new Rect(x, 78f, width, 23f), value, metricValueStyle);
        }

        private void DrawPlayerRow(Rect rect, PlayerDamage row, long groupTotal, long leaderDamage, float elapsed, int rank)
        {
            Color rankColor = GetRankColor(rank);
            DrawRect(rect, rank % 2 == 0 ? CardColor : CardAltColor);
            float barWidth = Mathf.Clamp01((float)row.TotalDamage / leaderDamage) * rect.width;
            DrawRect(new Rect(rect.x, rect.y + rect.height - 3f, barWidth, 3f), new Color(rankColor.r, rankColor.g, rankColor.b, 0.85f));
            DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), rankColor);

            rankStyle.normal.textColor = rankColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 9f, 27f, 24f), rank.ToString("00"), rankStyle);
            GUI.Label(new Rect(rect.x + 43f, rect.y + 4f, 157f, 22f), row.Name, nameStyle);

            double share = groupTotal > 0 ? (double)row.TotalDamage * 100.0 / groupTotal : 0.0;
            GUI.Label(new Rect(rect.x + 43f, rect.y + 24f, 157f, 17f), share.ToString("0.0") + "% 伤害占比", detailStyle);
            GUI.Label(new Rect(rect.x + 204f, rect.y + 9f, 88f, 24f), FormatNumber(row.TotalDamage), damageStyle);

            double dps = elapsed > 0.05f ? row.TotalDamage / elapsed : 0.0;
            GUI.Label(new Rect(rect.x + 292f, rect.y + 7f, 76f, 26f), FormatNumber((long)Math.Round(dps)), dpsStyle);
            GUI.Label(new Rect(rect.x + 368f, rect.y + 10f, rect.width - 368f, 22f), row.HitCount.ToString(), columnCenterStyle);
        }

        internal void RecordDamage(UnitAvatar victim, DamageFeedback[] feedbacks)
        {
            if (feedbacks == null || feedbacks.Length == 0)
                return;

            if (!roomActive)
            {
                PlayerAvatar currentPlayer = GetCurrentPlayer();
                if (currentPlayer == null || !currentPlayer.IsInBattle)
                    return;

                observedBattleState = true;
                battleStateKnown = true;
                BeginRoom();
            }

            bool recordedAny = false;
            HashSet<uint> hitPlayers = new HashSet<uint>();
            for (int i = 0; i < feedbacks.Length; i++)
            {
                DamageFeedback feedback = feedbacks[i];
                if (feedback == null || feedback.damageValue <= 0 || feedback.msgType > 2)
                    continue;

                UnitAvatar actualVictim = feedback.self != null ? feedback.self : victim;
                if (ResolvePlayer(actualVictim) != null)
                    continue;

                PlayerAvatar player = ResolvePlayer(feedback.attacker);
                if (player == null)
                    continue;

                if (!countShieldDamage.Value && recordedAny)
                    continue;

                uint key = player.netId;
                if (key == 0)
                    key = unchecked((uint)player.GetInstanceID());

                PlayerDamage row;
                if (!damageByPlayer.TryGetValue(key, out row))
                {
                    row = new PlayerDamage();
                    damageByPlayer.Add(key, row);
                }

                string playerName = player.Name;
                row.Name = string.IsNullOrEmpty(playerName) ? "玩家 " + key : playerName;
                row.TotalDamage += feedback.damageValue;
                if (hitPlayers.Add(key))
                    row.HitCount++;
                recordedAny = true;
            }
        }

        private static PlayerAvatar ResolvePlayer(UnitAvatar unit)
        {
            UnitAvatar current = unit;
            for (int i = 0; current != null && i < 8; i++)
            {
                PlayerAvatar player = current as PlayerAvatar;
                if (player != null)
                    return player;

                UnitAvatar leader = current.NetworkLeader;
                if (leader == null || leader == current)
                    break;
                current = leader;
            }
            return null;
        }

        private List<PlayerDamage> GetSortedRows()
        {
            List<PlayerDamage> rows = new List<PlayerDamage>(damageByPlayer.Values);
            rows.Sort(delegate(PlayerDamage a, PlayerDamage b) { return b.TotalDamage.CompareTo(a.TotalDamage); });
            return rows;
        }

        private static long GetGroupTotal(List<PlayerDamage> rows)
        {
            long total = 0;
            for (int i = 0; i < rows.Count; i++)
                total += rows[i].TotalDamage;
            return total;
        }

        private float GetRoomElapsed()
        {
            if (roomStartedAt < 0f)
                return 0f;
            float end = roomActive ? Time.realtimeSinceStartup : roomEndedAt;
            return Mathf.Max(0f, end - roomStartedAt);
        }

        private float GetRunElapsed()
        {
            return Mathf.Max(0f, displayedRunElapsed);
        }

        private void SetSettingsVisible(bool visible)
        {
            settingsVisible = visible;
            opacityDragging = false;
            scaleDragging = false;
            if (visible)
                EnableSystemCursor();
            else
                RestoreSystemCursor();
        }

        private void EnableSystemCursor()
        {
            if (!systemCursorOverridden)
            {
                savedSystemCursorVisible = Cursor.visible;
                savedSystemCursorLockMode = Cursor.lockState;
                systemCursorOverridden = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreSystemCursor()
        {
            if (!systemCursorOverridden)
                return;
            Cursor.lockState = savedSystemCursorLockMode;
            Cursor.visible = savedSystemCursorVisible;
            systemCursorOverridden = false;
        }

        private void EnsureStyles()
        {
            if (windowStyle != null)
                return;

            transparentTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            transparentTexture.SetPixel(0, 0, Color.clear);
            transparentTexture.Apply();

            windowStyle = new GUIStyle(GUI.skin.window);
            SetStyleBackground(windowStyle.normal, transparentTexture);
            SetStyleBackground(windowStyle.hover, transparentTexture);
            SetStyleBackground(windowStyle.active, transparentTexture);
            SetStyleBackground(windowStyle.focused, transparentTexture);
            SetStyleBackground(windowStyle.onNormal, transparentTexture);
            SetStyleBackground(windowStyle.onHover, transparentTexture);
            SetStyleBackground(windowStyle.onActive, transparentTexture);
            SetStyleBackground(windowStyle.onFocused, transparentTexture);
            windowStyle.padding = new RectOffset(0, 0, 0, 0);
            windowStyle.border = new RectOffset(0, 0, 0, 0);

            titleStyle = MakeStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            subtitleStyle = MakeStyle(11, FontStyle.Normal, TextAnchor.MiddleLeft, MutedColor);
            statusStyle = MakeStyle(12, FontStyle.Bold, TextAnchor.MiddleRight, ActiveColor);
            metricLabelStyle = MakeStyle(10, FontStyle.Normal, TextAnchor.MiddleLeft, MutedColor);
            metricValueStyle = MakeStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            columnStyle = MakeStyle(10, FontStyle.Normal, TextAnchor.MiddleLeft, MutedColor);
            columnCenterStyle = MakeStyle(10, FontStyle.Normal, TextAnchor.MiddleCenter, MutedColor);
            rankStyle = MakeStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter, AccentColor);
            nameStyle = MakeStyle(13, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            detailStyle = MakeStyle(10, FontStyle.Normal, TextAnchor.MiddleLeft, MutedColor);
            damageStyle = MakeStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            dpsStyle = MakeStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, AccentColor);
            centerStyle = MakeStyle(12, FontStyle.Normal, TextAnchor.MiddleCenter, MutedColor);
            closeStyle = MakeStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            authorStyle = MakeStyle(10, FontStyle.Normal, TextAnchor.MiddleLeft, AccentColor);
        }

        private static void SetStyleBackground(GUIStyleState state, Texture2D background)
        {
            state.background = background;
        }

        private static GUIStyle MakeStyle(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = size;
            style.fontStyle = fontStyle;
            style.alignment = alignment;
            style.normal.textColor = color;
            style.clipping = TextClipping.Clip;
            return style;
        }

        private void DrawRect(Rect rect, Color color)
        {
            color.a *= Mathf.Clamp(panelOpacity.Value, 0.25f, 1f);
            DrawSolidRect(rect, color);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Color GetRankColor(int rank)
        {
            if (rank == 1)
                return new Color(1f, 0.76f, 0.25f, 1f);
            if (rank == 2)
                return new Color(0.35f, 0.83f, 1f, 1f);
            if (rank == 3)
                return new Color(0.72f, 0.48f, 1f, 1f);
            return new Color(0.31f, 0.55f, 0.78f, 1f);
        }

        private void SaveWindowPosition()
        {
            if (windowX == null || windowY == null)
                return;
            windowX.Value = windowRect.x;
            windowY.Value = windowRect.y;
            Config.Save();
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static string FormatRunTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int remainingSeconds = total % 60;
            return hours > 0
                ? hours.ToString("00") + ":" + minutes.ToString("00") + ":" + remainingSeconds.ToString("00")
                : minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
        }

        private static string FormatNumber(long value)
        {
            if (value >= 1000000000L)
                return (value / 1000000000.0).ToString("0.00") + "B";
            if (value >= 1000000L)
                return (value / 1000000.0).ToString("0.00") + "M";
            if (value >= 10000L)
                return (value / 1000.0).ToString("0.0") + "K";
            return value.ToString("N0");
        }

        private sealed class PlayerDamage
        {
            public string Name;
            public long TotalDamage;
            public int HitCount;
        }
    }

    [HarmonyPatch(typeof(UnitAvatar), "UserCode_RpcShowAllDamageParticles__DamageFeedback[]")]
    internal static class DamageFeedbackPatch
    {
        private static void Prefix(UnitAvatar __instance, DamageFeedback[] __0)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null)
                return;

            try
            {
                plugin.RecordDamage(__instance, __0);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError("Failed to record damage feedback: " + exception);
            }
        }
    }

}
