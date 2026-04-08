public class AppSettings
{
    public RtspSettings Rtsp { get; set; } = new();
    public StreamSettings Stream { get; set; } = new();
    public List<Channel> Channels { get; set; } = new();
}

public class RtspSettings
{
    public string IpAddress { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string UrlTemplate { get; set; } = "";
}

public class StreamSettings
{
    public int TimeoutMinutes { get; set; }
    public string OutputPath { get; set; } = "";
}

public class Channel
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
}
