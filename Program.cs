using System.Diagnostics;
using System.Windows.Forms;

// Enable Windows visual styles
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var builder = WebApplication.CreateBuilder(args);

// Reduce Kestrel logging noise
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Error);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Error);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<StreamManager>();

var app = builder.Build();

// Clean up old stream files on startup (good hygiene!)
var config = app.Services.GetRequiredService<IConfiguration>();
var settings = new AppSettings();
config.Bind(settings);
var streamPath = settings.Stream.OutputPath;

if (Directory.Exists(streamPath))
{
    try
    {
        Directory.Delete(streamPath, true);
        Console.WriteLine($"Cleaned up old stream files at: {Path.GetFullPath(streamPath)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not clean stream folder: {ex.Message}");
    }
}
Directory.CreateDirectory(streamPath);

// Log the working directory
Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"wwwroot exists: {Directory.Exists("wwwroot")}");
Console.WriteLine($"stream folder exists: {Directory.Exists("wwwroot/stream")}");

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    app.Services.GetRequiredService<StreamManager>().StopStream();
});

// Configure static files with proper MIME types for HLS
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
provider.Mappings[".ts"] = "video/mp2t";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}");

// Get the URL for the tray icon
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
var url = urls.Split(';')[0];

// Create tray icon (must be created on the UI thread)
var trayIcon = new TrayIconService(lifetime, builder.Configuration);

// Run web app in background thread
var webAppTask = Task.Run(async () =>
{
    await app.StartAsync();

    // Open browser after a short delay
    await Task.Delay(2000);
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to open browser: {ex.Message}");
    }

    await app.WaitForShutdownAsync();
});

// Run Windows Forms message loop on main thread
Application.Run(new ApplicationContext());

// Cleanup
trayIcon.Dispose();
