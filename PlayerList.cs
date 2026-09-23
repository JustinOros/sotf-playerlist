using System;
using System.Collections.Generic;
using System.IO;
using RedLoader;
using RedLoader.Utils;
using SonsSdk;
using SonsSdk.Attributes;
using TheForest.Utils;
using UnityEngine;

namespace PlayerList;

public class PlayerList : SonsMod
{
    internal static KeyCode ListKey = KeyCode.Tab;

    private static string _configPath;
    private static readonly List<string> Names = new();
    private static bool _showing;
    private static float _nextRefresh;
    private static GUIStyle _titleStyle;
    private static GUIStyle _rowStyle;
    private static GUIStyle _boxStyle;
    private static Texture2D _bg;

    public PlayerList()
    {
        OnUpdateCallback = OnUpdate;
        OnGUICallback = OnGui;
    }

    protected override void OnSdkInitialized()
    {
        _configPath = Path.Combine(LoaderEnvironment.UserDataDirectory, "PlayerList.txt");
        Load();
        Save();
        RLog.Msg($"PlayerList 1.0.1 loaded. Hold {ListKey} to show connected players. Console: playerlist");
    }

    private static bool InMultiplayer => BoltNetwork.isRunning && LocalPlayer.Entity != null;

    private void OnUpdate()
    {
        _showing = InMultiplayer && Input.GetKey(ListKey);
        if (!_showing)
            return;

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }
    }

    private static void Refresh()
    {
        Names.Clear();
        var seen = new HashSet<IntPtr>();

        var local = LocalPlayer.Entity;
        if (local != null)
        {
            seen.Add(local.Pointer);
            Names.Add((GetName(local) ?? "You") + "  (you)");
        }

        var tracker = PlayerTracker.Instance;
        if (tracker == null || tracker.AllPlayerEntities == null)
            return;

        var others = new List<string>();
        foreach (var entity in tracker.AllPlayerEntities)
        {
            if (entity == null || !seen.Add(entity.Pointer))
                continue;

            var name = GetName(entity);
            if (name != null)
                others.Add(name);
        }
        others.Sort(StringComparer.OrdinalIgnoreCase);
        Names.AddRange(others);
    }

    private static string GetName(BoltEntity entity)
    {
        try
        {
            if (entity.TryFindState<IPlayerState>(out var state) && !string.IsNullOrWhiteSpace(state.name) && !state.name.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return state.name;
        }
        catch (Exception e)
        {
            RLog.Error($"PlayerList: could not read player name: {e.Message}");
        }
        return null;
    }

    private static void EnsureStyles()
    {
        if (_boxStyle != null)
            return;

        _bg = new Texture2D(1, 1);
        _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        _bg.Apply();
        _bg.hideFlags = HideFlags.HideAndDontSave;

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = _bg;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _titleStyle.normal.textColor = Color.white;

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft
        };
        _rowStyle.normal.textColor = Color.white;
    }

    private void OnGui()
    {
        if (!_showing)
            return;

        EnsureStyles();

        const float width = 420f;
        const float rowHeight = 28f;
        const float titleHeight = 40f;
        const float pad = 12f;

        var height = titleHeight + Names.Count * rowHeight + pad * 2;
        var x = (Screen.width - width) / 2f;
        var y = Screen.height * 0.15f;

        GUI.Box(new Rect(x, y, width, height), GUIContent.none, _boxStyle);
        GUI.Label(new Rect(x, y + pad, width, titleHeight), $"Players ({Names.Count})", _titleStyle);

        for (var i = 0; i < Names.Count; i++)
        {
            var rowY = y + pad + titleHeight + i * rowHeight;
            GUI.Label(new Rect(x + pad * 2, rowY, width - pad * 4, rowHeight), Names[i], _rowStyle);
        }
    }

    [DebugCommand("playerlist")]
    private void PlayerListCommand(string args)
    {
        var parts = (args ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && parts[0].Equals("key", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse(parts[1], true, out KeyCode key))
            {
                ListKey = key;
                Save();
                SonsTools.ShowMessage($"PlayerList key: {ListKey}");
            }
            else
            {
                SonsTools.ShowMessage($"Unknown key: {parts[1]}");
            }
            return;
        }

        if (!InMultiplayer)
        {
            SonsTools.ShowMessage("Not in a multiplayer game");
            return;
        }

        Refresh();
        DumpEntities();
        RLog.Msg($"PlayerList: {Names.Count} player(s)");
        foreach (var name in Names)
            RLog.Msg($"  {name}");
        SonsTools.ShowMessage($"Players ({Names.Count}): {string.Join(", ", Names)}");
    }

    private static void DumpEntities()
    {
        var tracker = PlayerTracker.Instance;
        if (tracker == null || tracker.AllPlayerEntities == null)
        {
            RLog.Msg("PlayerList: no PlayerTracker");
            return;
        }

        RLog.Msg($"PlayerList: {tracker.AllPlayerEntities.Count} tracked entities, local {LocalPlayer.Entity?.Pointer}");
        foreach (var entity in tracker.AllPlayerEntities)
        {
            if (entity == null)
            {
                RLog.Msg("  null entity");
                continue;
            }

            var raw = entity.TryFindState<IPlayerState>(out var state) ? state.name : "(no state)";
            RLog.Msg($"  ptr={entity.Pointer} go={entity.gameObject.name} name='{raw}' owner={entity.isOwner} attached={entity.isAttached} source={(entity.source == null ? "none" : entity.source.ToString())}");
        }
    }

    private static void Load()
    {
        if (!File.Exists(_configPath))
            return;

        foreach (var line in File.ReadAllLines(_configPath))
        {
            var kv = line.Split('=', 2);
            if (kv.Length != 2)
                continue;

            var k = kv[0].Trim().ToLowerInvariant();
            var v = kv[1].Trim();

            if (k == "key" && Enum.TryParse(v, true, out KeyCode key))
                ListKey = key;
        }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(_configPath, $"key={ListKey}\n");
        }
        catch (Exception e)
        {
            RLog.Error($"PlayerList: could not save config: {e.Message}");
        }
    }
}