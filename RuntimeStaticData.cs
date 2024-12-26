using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Community.PowerToys.Run.Plugin.TabPort.util;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort;

public static class RuntimeStaticData
{
    public static readonly ConcurrentDictionary<WebSocket, List<BrowserTab>> BrowserTabData = new();
    public static readonly ConcurrentDictionary<long, IntPtr> WindowIdHWndMap = new();
    public static readonly ConcurrentDictionary<IntPtr, bool> SavedHWnd = new();

    public static void SwitchToTabAction(BrowserTab tab)
    {
        if (!WindowIdHWndMap.ContainsKey(tab.WindowId)) RefreshMap();
#if DEBUG
        Log.Info("[SwitchToTabAction] Dealing tab: " + JsonSerializer.Serialize(tab), typeof(NativeUtils));
        Log.Info("[SwitchToTabAction] Tab Title: " + tab.Title, typeof(NativeUtils));
#endif
        var hWnd = WindowIdHWndMap.TryGetValue(tab.WindowId, out var result) ? result : IntPtr.Zero;
        if (hWnd == IntPtr.Zero)
        {
            Log.Info("[SwitchToTabAction] Failed to find window: " + tab.Title, typeof(NativeUtils));
            return;
        }

        NativeUtils.DoShowWindow(hWnd, tab);
    }

    private static void RefreshMap()
    {
        var duplicate = CheckIfHasDuplicateActiveTitle();
        if (duplicate.Count > 0)
        {
            Log.Info("[RefreshMap] Same Title Found", typeof(NativeUtils));
            foreach (var ws in duplicate.Keys)
            {
                Log.Info(
                    $"[RefreshMap] Duplicate Found Send Message reNameDuplicate {string.Join(" ", duplicate[ws].Select(id => id.ToString()))}",
                    typeof(NativeUtils));
                WebSocketUtils
                    .SendMessageAsync(
                        $"reNameDuplicate {string.Join(" ", duplicate[ws].Select(id => id.ToString()))}",
                        ws).GetAwaiter().GetResult();
            }
        }

        var possibleWindows = WindowUtils.FetchWindows();
        var activeTabs = GetActiveTabTitleWithWindowId();

        var json = JsonSerializer.Serialize(activeTabs);
        Log.Info($"[RefreshMap] Active tabs: {json}", typeof(NativeUtils));

        foreach (var tab in activeTabs)
        {
            try
            {
                possibleWindows.TryGetValue(tab.Item1, out var hWnd);
                if (SavedHWnd.ContainsKey(hWnd))
                {
                    continue;
                }

                WindowIdHWndMap[tab.Item2] = hWnd;
            }
            catch (ArgumentNullException e)
            {
                Log.Error("[RefreshMap] Failed to get WindowId With Title: " + tab.Item1 + " Error: " + e.Message,
                    typeof(NativeUtils));
            }
        }

        if (duplicate.Count <= 0) return;
        foreach (var (ws, ids) in duplicate)
        {
            WebSocketUtils.SendMessageAsync($"restore {string.Join(" ", ids)}", ws).GetAwaiter().GetResult();
        }
    }

    /// <returns>A list of tuples where each tuple contains the title of an active tab and its windowId.</returns>
    private static List<Tuple<string, long>> GetActiveTabTitleWithWindowId()
    {
        var result = new List<Tuple<string, long>>();
        foreach (var tabs in BrowserTabData)
        {
            result.AddRange(from tab in tabs.Value
                where tab.Active && !WindowIdHWndMap.ContainsKey(tab.WindowId)
                select new Tuple<string, long>(tab.Title, tab.WindowId));
        }

        return result;
    }

    private static Dictionary<WebSocket, List<long>> CheckIfHasDuplicateActiveTitle()
    {
        var result = new Dictionary<WebSocket, List<long>>();
        var activeData = BrowserTabData.Where(x => x.Value.Any(tab => tab.Active));
        var filteredActiveData = new Dictionary<WebSocket, List<BrowserTab>>();
        foreach (var tabs in activeData)
        {
            foreach (var tab in tabs.Value)
            {
                if (WindowIdHWndMap.ContainsKey(tab.WindowId))
                {
                    continue;
                }

                filteredActiveData[tabs.Key] = filteredActiveData.TryGetValue(tabs.Key, out var value)
                    ? value.Append(tab).ToList()
                    : [tab];
            }
        }

        var titleGroups = filteredActiveData
            .SelectMany(x => x.Value.Where(tab => tab.Active).Select(tab => new { tab.Title, tab.Id, x.Key }))
            .GroupBy(x => x.Title);

        foreach (var group in titleGroups)
        {
            if (group.Count() <= 1) continue;
            foreach (var obj in group)
            {
                if (result.TryGetValue(obj.Key, out var value))
                {
                    value.Add(obj.Id);
                }
                else
                {
                    result[obj.Key] = [obj.Id];
                }
            }
        }

        return result;
    }
}