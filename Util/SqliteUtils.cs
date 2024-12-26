using System;
using System.Data;
using System.IO;
using System.Linq;
using Community.PowerToys.Run.Plugin.TabPort.Properties;
using Microsoft.Data.Sqlite;
using Microsoft.Toolkit.Uwp.Notifications;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort.util;

public class SqliteConnectionUtils
{
    private static readonly Lazy<SqliteConnectionUtils> _instance =
        new(() => new SqliteConnectionUtils());

    private SqliteConnection _connection;
    
    private SqliteConnectionUtils()
    {
        RefreshConnection();
        OpenConnection();
    }

    private void RefreshConnection()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var databasePath = "";
        if (Main.Setting.CustomFaviconDbPath is not null && File.Exists(Main.Setting.CustomFaviconDbPath))
        {
            databasePath = Environment.ExpandEnvironmentVariables(Main.Setting.CustomFaviconDbPath);
        }
        else
        {
            switch (Main.Setting.FaviconDbPathPriority)
            {
                case Settings.FaviconDbPathPriorityItem.Chrome:
                    databasePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Favicons");
                    break;
                case Settings.FaviconDbPathPriorityItem.Edge:
                    databasePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Favicons");
                    break;
                case Settings.FaviconDbPathPriorityItem.Firefox:
                    if (Main.Setting.CustomFaviconDbPath is null || Main.Setting.CustomFaviconDbPath.Trim() == "")
                    {
                        databasePath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\Mozilla\Firefox\Profiles\");
                        if (Directory.Exists(databasePath))
                        {
                            databasePath = Directory.GetDirectories(databasePath)
                                .OrderByDescending(Directory.GetLastWriteTime).FirstOrDefault();
                            databasePath = Path.Combine(databasePath ?? "", "favicons.sqlite");
                        }
                    }

                    break;
                case Settings.FaviconDbPathPriorityItem.Opera:
                    databasePath =
                        Environment.ExpandEnvironmentVariables(
                            @"%AppData%\Opera Software\Opera Stable\Default\Favicons");
                    break;
                default:
                    Log.Info("Unknown FaviconDbPathPriorityItem", typeof(SqliteConnectionUtils));
                    throw new ArgumentOutOfRangeException();
            }
        }
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"file:{Uri.EscapeDataString(databasePath)}?mode=ro&immutable=1"
        }.ToString();
        _connection = new SqliteConnection(connectionString);
    }

    // Public static property to access the single instance
    public static SqliteConnectionUtils Instance => _instance.Value;

    // Method to get the connection
    public SqliteConnection GetConnection()
    {
        if (_connection.State == ConnectionState.Closed)
        {
            OpenConnection();
        }

        return _connection;
    }

    // Cleanup method to close the connection
    private void CloseConnection()
    {
        if (_connection != null && _connection.State != ConnectionState.Closed)
        {
            _connection.Close();
        }
    }

    // Method to safely reopen the connection
    public void ReopenConnection()
    {
        if (_connection.State is ConnectionState.Open)
        {
            CloseConnection();
        }

        RefreshConnection();
        OpenConnection();
    }

    private void OpenConnection()
    {
        try
        {
            _connection.Open();
            FaviconFetcher.TemporarilyDisabled = false;
        }
        catch (SqliteException e)
        {
            FaviconFetcher.TemporarilyDisabled = true;
            Log.Info($"[SqliteUtils] Failed to open connection: {e.Message}", typeof(SqliteConnectionUtils));
            new ToastContentBuilder()
                .AddText(Resources.open_connection_sqlite_exception_toast_title)
                .AddText(Resources.open_connection_sqlite_exception_toast_description)
                .Show();
        }
    }
}