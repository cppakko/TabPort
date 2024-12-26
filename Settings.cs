using System;
using System.Collections.Generic;
using System.Linq;
using Community.PowerToys.Run.Plugin.TabPort.Properties;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.TabPort;

public class Settings
{
    public int Port { get; set; } = 8899;
    public string CustomFaviconDbPath { get; set; }
    public double TitleWeight { get; set; } = 1;
    public double UrlWeight { get; set; } = 0;
    public FaviconDbPathPriorityItem FaviconDbPathPriority { get; set; }

    public enum FaviconDbPathPriorityItem
    {
        Chrome,
        Edge,
        Firefox,
        Opera
    }

    public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
    [
        new()
        {
            Key = nameof(Port),
            DisplayDescription = Resources.custom_port_description,
            DisplayLabel = Resources.custom_port,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            NumberValue = Port
        },
        new()
        {
            Key = nameof(TitleWeight),
            DisplayDescription = Resources.title_weight_description,
            DisplayLabel = Resources.title_weight,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            NumberValue = TitleWeight
        },
        new()
        {
            Key = nameof(UrlWeight),
            DisplayDescription = Resources.url_weight_description,
            DisplayLabel = Resources.url_weight,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            NumberValue = UrlWeight
        },
        new()
        {
            Key = nameof(FaviconDbPathPriority),
            DisplayDescription = Resources.favicon_db_path_priority_description,
            DisplayLabel = Resources.favicon_db_path_priority,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
            ComboBoxItems = Enum.GetValues(typeof(FaviconDbPathPriorityItem)).Cast<int>()
                .Select(v =>
                    new KeyValuePair<string, string>(((FaviconDbPathPriorityItem)v).ToString(), v + string.Empty))
                .ToList(),
            ComboBoxValue = (int)FaviconDbPathPriority
        },
        new()
        {
            Key = nameof(CustomFaviconDbPath),
            DisplayDescription = Resources.custom_favicon_db_path_description,
            DisplayLabel = Resources.custom_favicon_db_path,
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
            TextValue = CustomFaviconDbPath
        }
    ];
}