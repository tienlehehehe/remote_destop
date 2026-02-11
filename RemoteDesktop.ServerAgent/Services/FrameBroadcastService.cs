using Microsoft.AspNetCore.SignalR;
using RemoteDesktop.Shared.Models;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class FrameBroadcastService : BackgroundService
{
    private readonly Channel<FramePacket> _channel;
    private readonly IHubContext<RemoteHub> _hubContext;

    public FrameBroadcastService(Channel<FramePacket> channel, IHubContext<RemoteHub> hubContext)
    {
        _channel = channel;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Broadcast] FrameBroadcastService started...");

        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_channel.Reader.TryRead(out var frame))
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveFrame", frame, cancellationToken: stoppingToken);
                    Console.WriteLine($"[Broadcast] Frame {frame.Sequence}, size={frame.JpegBytes?.Length}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Broadcast] Lỗi gửi frame {frame.Sequence}: {ex.Message}");
                }
            }
        }
    }
}
