using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LeafClient.Services;

internal static class ScreenshotCaptureService
{
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr hBitmap, uint uStartScan, uint cScanLines, byte[]? lpvBits, ref BitmapInfo lpbmi, uint uUsage);

    private const uint SrcCopy = 0xCC0020;
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;
    private const int TargetWidth = 960;
    private const int TargetHeight = 540;

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader bmiHeader;
        public uint bmiColors; // unused for BI_RGB 24bpp
    }

    /// <summary>
    /// Captures the primary screen and returns a downscaled PNG as a byte array.
    /// Returns null if capture fails or if not running on Windows.
    /// </summary>
    public static byte[]? CaptureScreenAsPng()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        IntPtr hDesktop = IntPtr.Zero;
        IntPtr hdc = IntPtr.Zero;
        IntPtr hdcMem = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr hOldBitmap = IntPtr.Zero;

        try
        {
            int screenWidth = GetSystemMetrics(SmCxScreen);
            int screenHeight = GetSystemMetrics(SmCyScreen);
            if (screenWidth <= 0 || screenHeight <= 0)
                return null;

            hDesktop = GetDesktopWindow();
            hdc = GetDC(hDesktop);
            hdcMem = CreateCompatibleDC(hdc);
            hBitmap = CreateCompatibleBitmap(hdc, screenWidth, screenHeight);
            hOldBitmap = SelectObject(hdcMem, hBitmap);

            if (!BitBlt(hdcMem, 0, 0, screenWidth, screenHeight, hdc, 0, 0, SrcCopy))
                return null;

            var bmi = new BitmapInfo
            {
                bmiHeader = new BitmapInfoHeader
                {
                    biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    biWidth = screenWidth,
                    biHeight = -screenHeight, // negative = top-down scan order
                    biPlanes = 1,
                    biBitCount = 24,
                    biCompression = 0, // BI_RGB
                },
                bmiColors = 0
            };

            // Stride must be DWORD-aligned (4 bytes)
            int stride = ((screenWidth * 3) + 3) & ~3;
            byte[] pixelData = new byte[stride * screenHeight];
            GetDIBits(hdc, hBitmap, 0, (uint)screenHeight, pixelData, ref bmi, 0);

            // Strip stride padding to get tight BGR24 pixel data
            int rowSize = screenWidth * 3;
            byte[] tightData;
            if (stride == rowSize)
            {
                tightData = pixelData;
            }
            else
            {
                tightData = new byte[rowSize * screenHeight];
                for (int y = 0; y < screenHeight; y++)
                    Buffer.BlockCopy(pixelData, y * stride, tightData, y * rowSize, rowSize);
            }

            using var image = Image.LoadPixelData<Bgr24>(tightData, screenWidth, screenHeight);
            image.Mutate(ctx => ctx.Resize(TargetWidth, TargetHeight));

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ScreenshotCapture] Failed: {ex.Message}");
            return null;
        }
        finally
        {
            if (hOldBitmap != IntPtr.Zero && hdcMem != IntPtr.Zero)
                SelectObject(hdcMem, hOldBitmap);
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            if (hdcMem != IntPtr.Zero) DeleteDC(hdcMem);
            if (hdc != IntPtr.Zero && hDesktop != IntPtr.Zero)
                ReleaseDC(hDesktop, hdc);
        }
    }
}
