using System;
using System.Drawing;
using System.Drawing.Imaging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using static Vortice.Direct3D11.D3D11;

namespace RemoteDesktop.ServerAgent.Services
{
    public static class DxgiCapture
    {
        public static Bitmap CaptureScreenDXGI(uint outputIndex = 0)
        {
            // Tạo thiết bị Direct3D11
            D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out ID3D11Device device);

            // Lấy adapter và output theo index
            using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();
            adapter.EnumOutputs(outputIndex, out IDXGIOutput output);
            using var output1 = output.QueryInterface<IDXGIOutput1>();

            // Tạo duplication
            using var duplication = output1.DuplicateOutput(device);

            // Lấy frame
            duplication.AcquireNextFrame(1000, out _, out IDXGIResource desktopResource);
            using var texture = desktopResource.QueryInterface<ID3D11Texture2D>();
            var desc = texture.Description;

            // Tạo staging texture để đọc dữ liệu
            var stagingDesc = new Texture2DDescription
            {
                Width = desc.Width,
                Height = desc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = desc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read
            };

            using var staging = device.CreateTexture2D(stagingDesc);
            device.ImmediateContext.CopyResource(texture, staging);

            // Map dữ liệu sang Bitmap
            var dataBox = device.ImmediateContext.Map(staging, 0);
            var bmp = new Bitmap((int)desc.Width, (int)desc.Height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                       ImageLockMode.WriteOnly, bmp.PixelFormat);

            unsafe
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    IntPtr srcPtr = (nint)(dataBox.DataPointer + y * dataBox.RowPitch);
                    IntPtr destPtr = bmpData.Scan0 + y * bmpData.Stride;
                    Buffer.MemoryCopy((void*)srcPtr, (void*)destPtr, bmpData.Stride, bmp.Width * 4);
                }
            }

            bmp.UnlockBits(bmpData);
            device.ImmediateContext.Unmap(staging, 0);
            duplication.ReleaseFrame();

            return bmp;
        }
    }
}
