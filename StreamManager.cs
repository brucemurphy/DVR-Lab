using System.Diagnostics;

public class StreamManager
{
    private Process? _streamProcess;
    private int _currentChannel = -1;
    private readonly string _outputPath;
    private DateTime _streamStartTime;
    private System.Threading.Timer? _timeoutTimer;
    private readonly int _timeoutMinutes;
    private readonly AppSettings _settings;

    public StreamManager(IConfiguration configuration)
    {
        _settings = new AppSettings();
        configuration.Bind(_settings);

        _outputPath = _settings.Stream.OutputPath;

        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
            Console.WriteLine($"Created stream output directory: {Path.GetFullPath(_outputPath)}");
        }

        _timeoutMinutes = _settings.Stream.TimeoutMinutes;
        Console.WriteLine($"Stream timeout set to {_timeoutMinutes} minutes");
        Console.WriteLine($"Stream output path: {Path.GetFullPath(_outputPath)}");
        Console.WriteLine($"RTSP Server: {_settings.Rtsp.IpAddress}:{_settings.Rtsp.Port}");
    }

    public void StartStream(int channel = 1)
    {
        // If channel changed, stop current stream and clean up
        if (_streamProcess != null && _currentChannel != channel)
        {
            Console.WriteLine($"Channel change detected: {_currentChannel} -> {channel}. Stopping and cleaning...");
            StopStream();
            CleanupStreamFiles();
        }

        // If already running same channel, do nothing
        if (_streamProcess != null && _currentChannel == channel) 
        {
            Console.WriteLine($"Channel {channel} already streaming.");
            return;
        }

        _currentChannel = channel;
        _streamStartTime = DateTime.Now;

        // Reset timeout timer
        _timeoutTimer?.Dispose();
        _timeoutTimer = new System.Threading.Timer(OnStreamTimeout, null, TimeSpan.FromMinutes(_timeoutMinutes), Timeout.InfiniteTimeSpan);
        Console.WriteLine($"Stream timeout will trigger in {_timeoutMinutes} minutes at {_streamStartTime.AddMinutes(_timeoutMinutes):HH:mm:ss}");

        // Build RTSP URL from template
        var rtspUrl = _settings.Rtsp.UrlTemplate
            .Replace("{username}", _settings.Rtsp.Username)
            .Replace("{password}", _settings.Rtsp.Password)
            .Replace("{ip}", _settings.Rtsp.IpAddress)
            .Replace("{port}", _settings.Rtsp.Port.ToString())
            .Replace("{channel}", channel.ToString());

        // Use forward slashes for web compatibility
        var playlistPath = $"{_outputPath}/stream.m3u8";
        var segmentPath = $"{_outputPath}/segment%d.ts";

        // MINIMAL LATENCY SETTINGS
        // hls_list_size 3 = only 3 segments (3 seconds buffer)
        // hls_time 1 = 1 second segments for faster refresh
        // use_wallclock_as_timestamps 1 = fix timestamp issues from camera
        var ffmpegArgs = $"-rtsp_transport tcp -fflags nobuffer -flags low_delay -use_wallclock_as_timestamps 1 -i \"{rtspUrl}\" " +
                        $"-c:v libx264 -preset ultrafast -tune zerolatency -g 30 " +
                        $"-c:a aac -b:a 128k -af aresample=async=1 " +
                        $"-f hls -hls_time 1 -hls_list_size 3 " +
                        $"-hls_flags delete_segments+append_list+omit_endlist " +
                        $"-start_number 0 " +
                        $"-hls_segment_filename \"{segmentPath}\" " +
                        $"\"{playlistPath}\"";

        Console.WriteLine($"Starting low-latency stream for channel {channel}...");

        // Look for ffmpeg in application directory first, then PATH
        var ffmpegPath = "ffmpeg";
        var localFfmpeg = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe");
        if (File.Exists(localFfmpeg))
        {
            ffmpegPath = localFfmpeg;
            Console.WriteLine($"Using local ffmpeg: {localFfmpeg}");
        }
        else
        {
            Console.WriteLine($"Using ffmpeg from PATH (or place ffmpeg.exe in: {Directory.GetCurrentDirectory()})");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _streamProcess = new Process { StartInfo = startInfo };
        _streamProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };

        try
        {
            _streamProcess.Start();
            _streamProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting ffmpeg: {ex.Message}");
            Console.WriteLine($"💡 Solution: Download ffmpeg.exe and place it in: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"   Or add ffmpeg to your system PATH");
            throw;
        }
    }

    public void StopStream()
    {
        _timeoutTimer?.Dispose();
        _timeoutTimer = null;

        if (_streamProcess != null && !_streamProcess.HasExited)
        {
            Console.WriteLine($"Stopping stream for channel {_currentChannel}...");
            _streamProcess.Kill();
            _streamProcess.Dispose();
            _streamProcess = null;
        }
        _currentChannel = -1;
    }

    private void OnStreamTimeout(object? state)
    {
        Console.WriteLine($"Stream timeout reached after {_timeoutMinutes} minutes. Stopping stream...");
        StopStream();
        CleanupStreamFiles();
    }

    private void CleanupStreamFiles()
    {
        try
        {
            if (Directory.Exists(_outputPath))
            {
                var files = Directory.GetFiles(_outputPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                Console.WriteLine($"Cleaned up {files.Length} stream files");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error cleaning stream files: {ex.Message}");
        }
    }

    public int GetCurrentChannel() => _currentChannel;

    public bool IsStreaming() => _streamProcess != null && !_streamProcess.HasExited;

    public TimeSpan? GetRemainingTime()
    {
        if (!IsStreaming()) return null;
        var elapsed = DateTime.Now - _streamStartTime;
        var remaining = TimeSpan.FromMinutes(_timeoutMinutes) - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
