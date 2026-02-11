using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteDesktop.ServerAgent.Services
{
    public static class GDICapture
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        static GDICapture()
        {
            // Khai báo process DPI aware ngay khi class được load
            try
            {
                SetProcessDPIAware();
                Console.WriteLine("[GDICapture] Process set to DPI aware.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GDICapture] SetProcessDPIAware failed: {ex.Message}");
            }
        }

        public static Bitmap CaptureScreen(int screenIndex = 0)
        {
            var screens = Screen.AllScreens;
            if (screenIndex < 0 || screenIndex >= screens.Length)
                screenIndex = 0;

            var screen = screens[screenIndex];
            var bounds = screen.Bounds;

            var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            }

            return bmp;
        }
    }
}
