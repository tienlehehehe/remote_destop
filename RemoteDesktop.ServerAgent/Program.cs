using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteDesktop.ServerAgent.Services;
using RemoteDesktop.Shared.Models;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Tăng giới hạn message size và giữ kết nối lâu dài
builder.Services.AddSignalR(options =>
{
    // Cho phép tối đa 50 MB mỗi message
    options.MaximumReceiveMessageSize = 50 * 1024 * 1024;

    // Giữ kết nối lâu hơn (mặc định 15s)
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
});

builder.Services.AddSingleton(Channel.CreateUnbounded<FramePacket>());

// Đọc cấu hình từ appsettings.json
var captureSection = builder.Configuration.GetSection("CaptureSettings");
var modeString = captureSection.GetValue<string>("Mode") ?? "GDI";
var outputIndex = captureSection.GetValue<uint>("OutputIndex");

var mode = modeString.Equals("DXGI", StringComparison.OrdinalIgnoreCase)
    ? CaptureMode.DXGI
    : CaptureMode.GDI;

// Đăng ký CaptureService với cấu hình
builder.Services.AddHostedService(sp =>
{
    var channel = sp.GetRequiredService<Channel<FramePacket>>();
    return new CaptureService(channel, mode, outputIndex);
});

builder.Services.AddHostedService<FrameBroadcastService>();

// Cấu hình Kestrel để yêu cầu client certificate
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

        httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
        {
            if (errors != System.Net.Security.SslPolicyErrors.None)
                return false;

            return chain.ChainElements
                        .Any(e => e.Certificate.Issuer.Contains("CN=MyDevCA"));
        };
    });
});

var app = builder.Build();

app.UseRouting();
app.MapHub<RemoteHub>("/remote");
app.MapHub<FileTransferHub>("/fileTransferHub");

app.Run();
