using System;
using Community.PowerToys.Run.Plugin.TabPort.util;
using Microsoft.Data.Sqlite;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort;

public class FaviconFetcher
{
    private const int MaxRetryCount = 1;

    private const string FirefoxQuery =
        """
            SELECT data FROM moz_pages_w_icons
            INNER JOIN moz_icons_to_pages ON moz_pages_w_icons.id = moz_icons_to_pages.page_id
            INNER JOIN moz_icons ON moz_icons_to_pages.icon_id = moz_icons.id
            WHERE moz_pages_w_icons.page_url LIKE @domain
            ORDER BY width DESC LIMIT 1;
        """;

    private const string ChromiumQuery =
        """
            SELECT image_data FROM icon_mapping
            INNER JOIN favicon_bitmaps ON icon_mapping.icon_id = favicon_bitmaps.icon_id
            WHERE icon_mapping.page_url LIKE @domain
            ORDER BY width DESC LIMIT 1;
        """;

    public static bool TemporarilyDisabled { get; set; }

    public static byte[] FetchFaviconLocalDatabase(string domain, int retryCount = 0)
    {
        if (TemporarilyDisabled) return null;
        if (retryCount > MaxRetryCount) return null;

        try
        {
            var connection = SqliteConnectionUtils.Instance.GetConnection();
            var query = Main.Setting.FaviconDbPathPriority == Settings.FaviconDbPathPriorityItem.Firefox
                ? FirefoxQuery
                : ChromiumQuery;

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@domain", $"%{GetHostName(domain)}%");

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var bufferSize = reader.GetBytes(0, 0, null, 0, 0);
                var icoBlob = new byte[bufferSize];
                long bytesRead = 0;
                var offset = 0;
                while (bytesRead < bufferSize)
                {
                    bytesRead += reader.GetBytes(0, offset, icoBlob, offset, (int)(bufferSize - bytesRead));
                    offset += (int)bytesRead;
                }

                return icoBlob;
            }
        }
        catch (SqliteException ex)
        {
            Log.Info(
                $"[FaviconFetcher] SQLite error: {ex.Message}, Database Path: {SqliteConnectionUtils.Instance.GetConnection().DataSource}",
                typeof(FaviconFetcher));

            SqliteConnectionUtils.Instance.ReopenConnection();
            return retryCount < MaxRetryCount ? FetchFaviconLocalDatabase(domain, retryCount + 1) : null;
        }
        catch (Exception ex)
        {
            Log.Info($"[FaviconFetcher] General error: {ex.Message}", typeof(FaviconFetcher));
        }
        return null;
    }

    private static string GetHostName(string url)
    {
        return new Uri(url).Host;
    }
}