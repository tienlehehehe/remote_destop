using RemoteDesktop.ServerAgent.Services;
using RemoteDesktop.Shared.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public enum CaptureMode
{
    GDI,
    DXGI
}

public class CaptureService : BackgroundService
{
    private readonly Channel<FramePacket> _channel;
    private readonly CaptureMode _mode;
    private readonly uint _outputIndex;
    private int _seq = 0;

    public CaptureService(Channel<FramePacket> channel, CaptureMode mode = CaptureMode.GDI, uint outputIndex = 0)
    {
        _channel = channel;
        _mode = mode;
        _outputIndex = outputIndex;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fps = 10;
        var intervalMs = 1000 / fps;

        Console.WriteLine($"[CaptureService] started... Mode={_mode}, OutputIndex={_outputIndex}");

        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        var encParams = new EncoderParameters(1);
        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 40L);

        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Bitmap bmp = null;
            Bitmap resized = null;

            try
            {
                // Chọn phương thức capture
                bmp = _mode switch
                {
                    CaptureMode.GDI => GDICapture.CaptureScreen(),
                    CaptureMode.DXGI => SafeDxgiCapture(_outputIndex),
                    _ => null
                };

                if (bmp == null)
                {
                    Console.WriteLine("[WARN] Capture trả về null, bỏ qua frame.");
                    continue;
                }

                Console.WriteLine($"[Capture] bmp size={bmp.Width}x{bmp.Height}");

                // Resize luôn về 1920x1080
                //resized = new Bitmap(bmp, new Size(1920, 1080));
                resized = ResizeWithLetterbox(bmp, new Size(1920, 1080));

                // Encode JPEG
                byte[] jpegBytes;
                using (var ms = new MemoryStream())
                {
                    resized.Save(ms, jpegEncoder, encParams);
                    jpegBytes = ms.ToArray();
                }

                var packet = new FramePacket
                {
                    Sequence = Interlocked.Increment(ref _seq),
                    TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    JpegBytes = jpegBytes,
                    Width = resized.Width,
                    Height = resized.Height
                };

                Console.WriteLine($"[OK] Frame {packet.Sequence}, jpeg size={jpegBytes.Length}, target={resized.Width}x{resized.Height}");
                _channel.Writer.TryWrite(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
            finally
            {
                bmp?.Dispose();
                resized?.Dispose();
            }

            var delay = intervalMs - (int)sw.ElapsedMilliseconds;
            if (delay > 0)
                await Task.Delay(delay, stoppingToken);
        }
    }

    private static Bitmap SafeDxgiCapture(uint outputIndex)
    {
        try
        {
            return DxgiCapture.CaptureScreenDXGI(outputIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DXGI] Capture lỗi: {ex.Message}");
            return null!;
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        return ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == format.Guid);
    }

    private static Bitmap ResizeWithLetterbox(Bitmap source, Size target)
    {
        // Tính tỷ lệ scale theo Uniform (giữ nguyên tỷ lệ gốc)
        double ratioX = (double)target.Width / source.Width;
        double ratioY = (double)target.Height / source.Height;
        double ratio = Math.Min(ratioX, ratioY);

        int newWidth = (int)(source.Width * ratio);
        int newHeight = (int)(source.Height * ratio);

        // Tạo bitmap khung 1920x1080 với nền đen
        Bitmap result = new Bitmap(target.Width, target.Height);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.Clear(Color.Black);

            // Tính vị trí để ảnh nằm giữa khung
            int posX = (target.Width - newWidth) / 2;
            int posY = (target.Height - newHeight) / 2;

            g.DrawImage(source, posX, posY, newWidth, newHeight);
        }

        return result;
    }
}
