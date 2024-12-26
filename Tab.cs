namespace Community.PowerToys.Run.Plugin.TabPort;

public class MutedInfo
{
    public bool Muted { get; set; }
}

public class BrowserTab
{
    public bool Active { get; set; }
    public bool Audible { get; set; }
    public bool AutoDiscardable { get; set; }
    public bool Discarded { get; set; }
    public string FavIconUrl { get; set; }
    public int GroupId { get; set; }
    public int Height { get; set; }
    public bool Highlighted { get; set; }
    public long Id { get; set; }
    public bool Incognito { get; set; }
    public int Index { get; set; }
    public double LastAccessed { get; set; }
    public MutedInfo MutedInfo { get; set; }
    public bool Pinned { get; set; }
    public bool Selected { get; set; }
    public string Status { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public int Width { get; set; }
    public long WindowId { get; set; }
}
