using Microsoft.AspNetCore.SignalR;
using RemoteDesktop.Shared.Models;
using System.Threading.Channels;

public class RemoteHub : Hub
{
    private readonly Channel<FramePacket> _channel;

    public RemoteHub(Channel<FramePacket> channel)
    {
        _channel = channel;
    }

    private static readonly Dictionary<string, string> _users = new()
    {
        { "demo", "123" },
        { "demo2", "321" }
    };

    public Task<AuthResponse> Authenticate(AuthRequest request)
    {
        if (_users.TryGetValue(request.Username, out var pwd) && pwd == request.Password)
        {
            return Task.FromResult(new AuthResponse { Success = true, Message = "Authenticated" });
        }
        return Task.FromResult(new AuthResponse { Success = false, Message = "Invalid credentials" });
    }

    // Nhận sự kiện input từ client
    public async Task SendInputEvent(InputEvent evt)
    {
        string logMessage = evt.Type == InputType.Keyboard
            ? $"[Input] Key {(evt.IsKeyUp ? "Up" : "Down")}: {evt.KeyCode}"
            : $"[Input] Mouse event: Flags={evt.MouseFlags}, X={evt.X}, Y={evt.Y}";

        Console.WriteLine(logMessage);
        InputLogger.Log(logMessage);

        await Clients.Caller.SendAsync("AckInputEvent", $"Received {evt.Type} event");
    }

    public static class InputLogger
    {
        // Đặt file log ngay trong thư mục chạy Program.cs
        private static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "input.log");

        public static void Log(string message)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InputLogger] Error writing log: {ex.Message}");
            }
        }
    }

    private static readonly Dictionary<string, List<FileChunk>> _uploadSessions = new();

    public async Task UploadChunk(FileChunk chunk)
    {
        if (!_uploadSessions.ContainsKey(chunk.FileName))
            _uploadSessions[chunk.FileName] = new List<FileChunk>();

        _uploadSessions[chunk.FileName].Add(chunk);

        Console.WriteLine($"[Upload] Received chunk {chunk.Index}/{chunk.Total} for {chunk.FileName}");

        if (_uploadSessions[chunk.FileName].Count == chunk.Total)
        {
            var ordered = _uploadSessions[chunk.FileName].OrderBy(c => c.Index).ToList();
            var folder = Path.Combine(AppContext.BaseDirectory, "uploads");
            Directory.CreateDirectory(folder);

            using var fs = new FileStream(Path.Combine(folder, chunk.FileName), FileMode.Create);
            foreach (var c in ordered)
                fs.Write(c.Data, 0, c.Data.Length);

            Console.WriteLine($"[Upload] File {chunk.FileName} completed");
            _uploadSessions.Remove(chunk.FileName);
        }

        await Clients.Caller.SendAsync("AckChunk", $"Received chunk {chunk.Index} of {chunk.FileName}");
    }

    public Task<List<string>> ListServerFiles()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(folder);

        var files = Directory.GetFiles(folder)
                             .Select(Path.GetFileName)
                             .ToList();

        return Task.FromResult(files);
    }
}
