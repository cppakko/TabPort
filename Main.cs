using ManagedCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Community.PowerToys.Run.Plugin.TabPort.Properties;
using Microsoft.PowerToys.Settings.UI.Library;
using Community.PowerToys.Run.Plugin.TabPort.util;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort;

public class Main : IPlugin, IContextMenu, IDisposable, ISettingProvider, IDelayedExecutionPlugin, IPluginI18n
{
    public static string PluginID => "CEFA2A750B2D4D56A166EE27BE3DBBD8";
    public string Name => Resources.plugin_name;
    public string Description => Resources.plugin_description;
    private PluginInitContext Context { get; set; }
    private string IconPath { get; set; }
    private bool Disposed { get; set; }

    public static readonly Settings Setting = new();
    private WebSocketUtils WsUtils { get; set; }

    public void Init(PluginInitContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(Context.API.GetCurrentTheme());
        WsUtils = new WebSocketUtils();
        Log.Info("Start At Port: " + Setting.Port, GetType());
        WsUtils.StartWebSocketServer(Setting.Port);
    }

    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.ContextData is not Tuple<WebSocket, BrowserTab> tuple) return [];
        var contextMenuResult = new List<ContextMenuResult>
        {
            new()
            {
                PluginName = Name,
                Title = "Copy to clipboard (Ctrl+C)",
                FontFamily = "Segoe MDL2 Assets",
                Glyph = "\xE8C8", // Copy
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ =>
                {
                    Clipboard.SetDataObject(tuple.Item2.Title);
                    return true;
                }
            },
            new()
            {
                PluginName = Name,
                Title = "Close tab (Ctrl+D)",
                FontFamily = "Segoe MDL2 Assets",
                Glyph = "\xE74D", // Delete
                AcceleratorKey = Key.D,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = actionContext =>
                {
                    _ = WebSocketUtils.SendMessageAsync($"close {tuple.Item2.Id}", tuple.Item1);
                    return true;
                }
            },
            new()
            {
                PluginName = Name,
                Title = "Copy Link (Ctrl+L)",
                FontFamily = "Segoe MDL2 Assets",
                Glyph = "\xE71B", // Link
                AcceleratorKey = Key.L,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ =>
                {
                    Clipboard.SetDataObject(tuple.Item2.Url);
                    return true;
                }
            }
        };

        return contextMenuResult;
    }

    private void UpdateIconPath(Theme theme) => IconPath = theme is Theme.Light or Theme.HighContrastWhite
        ? "Images/website.light.png"
        : "Images/website.dark.png";

    private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

    public void UpdateSettings(PowerLauncherPluginSettings settings)
    {
        if (settings.AdditionalOptions == null) return;
        Setting.Port = Convert.ToInt32(settings.AdditionalOptions.FirstOrDefault(x =>
            x.Key == nameof(Setting.Port))?.NumberValue);
        Setting.UrlWeight =
            Convert.ToDouble(settings.AdditionalOptions.FirstOrDefault(x =>
                x.Key == nameof(Setting.UrlWeight))?.NumberValue ?? 0.5d);
        Setting.TitleWeight =
            Convert.ToDouble(settings.AdditionalOptions.FirstOrDefault(x =>
                x.Key == nameof(Setting.TitleWeight))?.NumberValue ?? 0.5d);
        Setting.CustomFaviconDbPath = settings.AdditionalOptions.FirstOrDefault(x =>
            x.Key == nameof(Setting.CustomFaviconDbPath))?.TextValue;
        SqliteConnectionUtils.Instance.ReopenConnection();
        var comboBoxValue = settings.AdditionalOptions
            .FirstOrDefault(x =>
                x.Key == nameof(Setting.FaviconDbPathPriority))?.ComboBoxValue;
        if (comboBoxValue != null)
            Setting.FaviconDbPathPriority = (Settings.FaviconDbPathPriorityItem)comboBoxValue;
        /*if (Setting.Port != null)
        {
            WsUtils.RestartWebSocketServer(Setting.Port);
        }*/
        
    }

    public IEnumerable<PluginAdditionalOption> AdditionalOptions => Setting.AdditionalOptions;

    public List<Result> Query(Query query, bool delayedExecution)
    {
        var search = query.Search;
        var keySet = RuntimeStaticData.BrowserTabData.Keys.ToList();
        var browserTabs = new List<Tuple<WebSocket, BrowserTab>>();
        foreach (var key in keySet)
        {
            browserTabs.AddRange(RuntimeStaticData.BrowserTabData[key].Select(tab => Tuple.Create(key, tab)));
        }
        return browserTabs.Select(tuple =>
            {
                var browserTab = tuple.Item2;
                var browser = tuple.Item1;
                var faviconBin = FaviconFetcher.FetchFaviconLocalDatabase(browserTab.Url);
                var result = new Result
                {
                    QueryTextDisplay = browserTab.Title,
                    Title = browserTab.Title,
                    SubTitle = browserTab.Url,
                    ToolTipData = new ToolTipData(browserTab.Title, browserTab.Status),
                    Action = context =>
                    {
                        Clipboard.SetDataObject(browserTab.Title);
                        RuntimeStaticData.SwitchToTabAction(browserTab);
                        _ = WebSocketUtils.SendMessageAsync($"switch {browserTab.Id} {browserTab.WindowId}",
                            browser);
                        return true;
                    },
                    ContextData = new Tuple<WebSocket, BrowserTab>(browser, browserTab),
                    Score =
                        (int)(StringMatcher.FuzzySearch(search, browserTab.Title).Score * Setting.TitleWeight +
                              StringMatcher.FuzzySearch(search, browserTab.Url).Score * Setting.UrlWeight),
                    Glyph = "\xE838"
                };
                if (faviconBin != null && faviconBin.Length > 0)
                {
                    result.Icon = () => GetImageSourceFromRawPngData(faviconBin);
                }
                else
                {
                    result.IcoPath = IconPath;
                }

                return result;
            })
            .ToList();
    }

    private static BitmapImage GetImageSourceFromRawPngData(byte[] rawPngData)
    {
        try
        {
            using var memoryStream = new MemoryStream(rawPngData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (NotSupportedException ex)
        {
            // This exception is thrown when the image is not a valid PNG image
            Log.Info("Error in GetImageSourceFromRawPngData: NotSupportedException, Message: " + ex.Message,
                typeof(Main));
        }
        catch (Exception ex)
        {
            Log.Info($"Error in GetImageSourceFromRawPngData: {ex.Message}", typeof(Main));
        }

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed || !disposing)
        {
            return;
        }

        if (Context?.API != null)
        {
            Context.API.ThemeChanged -= OnThemeChanged;
        }

        Disposed = true;
    }

    public List<Result> Query(Query query) => [];
    public string GetTranslatedPluginTitle() => Resources.plugin_name;
    public string GetTranslatedPluginDescription() => Resources.plugin_description;
    public Control CreateSettingPanel() => throw new NotImplementedException();
}