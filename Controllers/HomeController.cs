using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly StreamManager _streamManager;
    private readonly IConfiguration _configuration;

    public HomeController(StreamManager streamManager, IConfiguration configuration)
    {
        _streamManager = streamManager;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        var settings = new AppSettings();
        _configuration.Bind(settings);
        return View(settings);
    }

    public IActionResult Settings()
    {
        var settings = new AppSettings();
        _configuration.Bind(settings);
        return View(settings);
    }

    [HttpPost]
    public IActionResult StartStream(int channel)
    {
        _streamManager.StartStream(channel);
        return Ok(new { success = true, channel });
    }

    [HttpPost]
    public IActionResult Start(int channel)
    {
        _streamManager.StartStream(channel);
        return Ok(new { success = true, channel });
    }

    [HttpPost]
    public IActionResult StopStream()
    {
        _streamManager.StopStream();
        return Ok(new { success = true });
    }

    [HttpPost]
    public IActionResult Stop()
    {
        _streamManager.StopStream();
        return Ok(new { success = true });
    }

    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isStreaming = _streamManager.IsStreaming(),
            currentChannel = _streamManager.GetCurrentChannel(),
            remainingTime = _streamManager.GetRemainingTime()?.ToString(@"mm\:ss")
        });
    }

    [HttpGet]
    public IActionResult GetChannels()
    {
        var settings = new AppSettings();
        _configuration.Bind(settings);
        return Ok(settings.Channels);
    }

    [HttpGet]
    public IActionResult Status()
    {
        return Ok(new
        {
            running = _streamManager.IsStreaming(),
            channel = _streamManager.GetCurrentChannel()
        });
    }

    [HttpPost]
    public IActionResult SaveSettings([FromBody] AppSettings settings)
    {
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = "Warning",
                        Microsoft = "Warning",
                        MicrosoftAspNetCore = "Warning"
                    }
                },
                Urls = _configuration["Urls"],
                settings.Rtsp,
                settings.Stream,
                settings.Channels
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            System.IO.File.WriteAllText(configPath, json);

            return Ok(new { success = true, message = "Settings saved! Please restart the application for changes to take effect." });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = $"Error saving settings: {ex.Message}" });
        }
    }
}
