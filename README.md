# 📹 DVR Lab

A modern, lightweight DVR (Digital Video Recorder) web application built with ASP.NET Core Razor Pages that allows you to view and manage RTSP camera streams through a clean web interface with low-latency HLS streaming.

## ✨ Features

### 🎥 Video Streaming
- **Low-Latency HLS Streaming**: Converts RTSP streams to HLS with minimal buffering (1-second segments, 3-second buffer)
- **Multi-Channel Support**: Configure and view multiple camera channels (up to 8 channels)
- **Channel Switching**: Easily switch between configured camera feeds with automatic stream cleanup
- **Auto-Timeout**: Automatically stops streams after a configurable timeout period to save resources (default: 5 minutes)
- **Timestamp Fix**: Handles camera timestamp inconsistencies with wallclock timestamps and audio resampling

### 🖥️ System Tray Integration
- **Background Operation**: Runs silently in the Windows system tray
- **Quick Access**: Double-click the tray icon to open the web interface
- **Application Output Viewer**: View live console output and logs in a convenient window with auto-refresh
- **Custom Icon Support**: Displays a custom icon in the system tray (place `dvr lab.ico` in the application directory)
- **Simple Menu**: Right-click menu with text-only options for quick access

### ⚙️ Configuration
- **Fully Configurable RTSP Settings**: Configure your camera server details via `appsettings.json` or web interface
- **Flexible URL Templates**: Customize RTSP URL format to match your camera system with variable placeholders
- **Named Channels**: Give friendly names to your camera channels for easy identification
- **Adjustable Timeout**: Set how long streams stay active before auto-stopping
- **Port Configuration**: Run on any port (default: 5000, configurable to any port or localhost only)
- **Web-Based Settings Editor**: Change settings through the web interface and save to `appsettings.json`

### 🌐 Web Interface
- **Clean Dark Theme**: Modern dark UI optimized for viewing video feeds
- **Responsive Design**: Works on desktop and mobile browsers
- **Settings Page**: Easily configure RTSP settings, channels, and timeouts through the web interface
- **Channel Dropdown**: Visual channel selector with custom names
- **Real-time Status**: Live status indicator showing streaming state and remaining time
- **Immediate Feedback**: Visual feedback when starting/stopping streams

## 🚀 Getting Started

### Prerequisites

- **Windows 10/11** (for system tray functionality)
- **.NET 8.0 Runtime** or SDK
- **FFmpeg**: Required for RTSP to HLS conversion
  - Download from [ffmpeg.org](https://ffmpeg.org/download.html)
  - Place `ffmpeg.exe` in the application directory, or add to your system PATH

### Installation

1. **Clone or download** this repository

2. **Install FFmpeg**:
   - Download FFmpeg from [ffmpeg.org](https://ffmpeg.org/download.html)
   - Extract and place `ffmpeg.exe` in the same folder as `DVR Lab.exe`
   - OR add FFmpeg to your system PATH

3. **Configure your cameras** in `appsettings.json`:

```json
{
  "Urls": "http://0.0.0.0:5000",
  "Rtsp": {
    "IpAddress": "192.168.1.100",
    "Port": 554,
    "Username": "admin",
    "Password": "your-password",
    "UrlTemplate": "rtsp://{username}:{password}@{ip}:{port}/chID={channel}"
  },
  "Stream": {
    "TimeoutMinutes": 5,
    "OutputPath": "wwwroot/stream"
  },
  "Channels": [
    { "Number": 1, "Name": "Front Door" },
    { "Number": 2, "Name": "Backyard" },
    { "Number": 3, "Name": "Driveway" },
    { "Number": 4, "Name": "Side Gate" }
  ]
}
```

4. **Optional**: Add a custom icon by placing `dvr lab.ico` in the application directory

5. **Run** the application:
   ```bash
   dotnet run
   ```
   Or double-click `DVR Lab.exe`

## ⚙️ Configuration

### RTSP Settings

All RTSP connection settings are configured in `appsettings.json`:

| Setting | Description | Example |
|---------|-------------|---------|
| `IpAddress` | IP address of your camera/DVR system | `192.168.1.100` |
| `Port` | RTSP port (usually 554) | `554` |
| `Username` | RTSP authentication username | `admin` |
| `Password` | RTSP authentication password | `your-password` |
| `UrlTemplate` | RTSP URL format with placeholders | `rtsp://{username}:{password}@{ip}:{port}/chID={channel}` |

**URL Template Placeholders:**
- `{username}` - Replaced with your RTSP username
- `{password}` - Replaced with your RTSP password
- `{ip}` - Replaced with the camera IP address
- `{port}` - Replaced with the RTSP port
- `{channel}` - Replaced with the channel number when streaming

### Web Server Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `Urls` | Web server binding address and port | `http://0.0.0.0:5000` |

To expose on a different port, change the `Urls` setting:
```json
"Urls": "http://0.0.0.0:8080"
```

To expose only on localhost:
```json
"Urls": "http://localhost:5000"
```

### Stream Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `TimeoutMinutes` | Auto-stop stream after this many minutes | `5` |
| `OutputPath` | Where HLS segments are stored | `wwwroot/stream` |

### Channel Configuration

Define your camera channels with friendly names:

```json
"Channels": [
  { "Number": 1, "Name": "Front Door Camera" },
  { "Number": 2, "Name": "Backyard Camera" },
  { "Number": 3, "Name": "Garage Camera" }
]
```

## 🎯 Usage

1. **Launch** the application - it will start in the system tray
2. **Access** the web interface:
   - Double-click the tray icon, or
   - Navigate to `http://localhost:5000` in your browser
3. **Select** a channel to start streaming
4. **Switch** channels as needed - the previous stream stops automatically
5. **View logs** by right-clicking the tray icon and selecting "View app output"
6. **Exit** by right-clicking the tray icon and selecting "Exit"

## 🛠️ Technical Details

### Architecture
- **Frontend**: ASP.NET Core Razor Pages with JavaScript
- **Backend**: .NET 8.0 with C# 12
- **Streaming**: FFmpeg with HLS (HTTP Live Streaming)
- **UI**: Windows Forms for system tray integration

### Low-Latency Streaming
The application uses optimized FFmpeg settings for minimal latency:
- 1-second HLS segments
- 3-segment playlist (3-second buffer)
- Ultra-fast encoding preset
- Zero-latency tuning
- TCP transport for reliability
- Wallclock timestamps to fix camera timing issues
- Audio resampling for synchronization

### File Structure
```
DVR Lab/
├── Controllers/        # MVC controllers for API endpoints
├── Views/             # Razor Pages views
│   ├── Home/         # Main pages (Index, Settings)
│   └── Shared/       # Layout templates
├── wwwroot/          # Static files (CSS, JS, images)
│   └── stream/       # Generated HLS segments (auto-created)
├── StreamManager.cs  # Handles FFmpeg streaming
├── TrayIconService.cs # System tray functionality
├── Program.cs        # Application entry point
└── appsettings.json  # Configuration file
```

## 🔒 Security Notes

- **RTSP credentials** are stored in `appsettings.json` - keep this file secure
- By default, the web interface is exposed on all network interfaces (`0.0.0.0`)
- Consider using `localhost` only if you don't need remote access
- For production use, implement proper authentication and HTTPS

## 🐛 Troubleshooting

### Stream Won't Start
- Verify FFmpeg is installed and accessible
- Check RTSP credentials in `appsettings.json`
- Ensure your camera/DVR is accessible on the network
- Check the application output window for detailed errors (right-click tray icon → "View app output")
- Verify the RTSP URL template matches your camera's format

### Video Appears Black or Frozen
- Camera may have timestamp issues - the app includes fixes for this
- Try stopping and restarting the stream
- Check if the camera is streaming to other devices
- Verify network bandwidth is sufficient

### High CPU Usage
- Try increasing HLS segment time (edit FFmpeg args in `StreamManager.cs`)
- Reduce video quality by adjusting FFmpeg encoding preset
- Consider using a different preset like `fast` or `medium` instead of `ultrafast`

### Settings Won't Save
- Ensure the application has write permissions to `appsettings.json`
- Restart the application after saving settings for changes to take effect
- Check the application output for error messages

### Tray Icon Not Showing
- Ensure Windows is not hiding the icon in the overflow area
- Check that `dvr lab.ico` is in the correct location (optional)
- Application uses system default icon if custom icon is not found

## 📝 License

This project is provided as-is for personal use.

## 🙏 Acknowledgments

- **FFmpeg** for powerful multimedia processing
- **HLS.js** for browser-based HLS playback
- **ASP.NET Core** for the web framework

---

**Built with ❤️ for home surveillance and security**
