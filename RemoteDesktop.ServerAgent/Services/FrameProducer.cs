using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using System;
using RemoteDesktop.Shared.Models;

namespace RemoteDesktop.ServerAgent.Services
{
    public class FrameProducer : BackgroundService
    {
        private readonly ChannelReader<FramePacket> _reader;
        private readonly IHubContext<RemoteHub> _hubContext;

        public FrameProducer(Channel<FramePacket> channel, IHubContext<RemoteHub> hubContext)
        {
            _reader = channel.Reader;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("FrameProducer started...");

            try
            {
                await foreach (var packet in _reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        // Gửi packet tới tất cả client qua SignalR Hub
                        await _hubContext.Clients.All.SendAsync("StreamFrames", packet, stoppingToken);

                        // Log chi tiết
                        Console.WriteLine($"[OK] Sent Frame {packet.Sequence} ({packet.Width}x{packet.Height}) size={packet.JpegBytes?.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to send frame {packet.Sequence}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] FrameProducer loop exception: {ex.Message}");
            }
        }
    }
}
