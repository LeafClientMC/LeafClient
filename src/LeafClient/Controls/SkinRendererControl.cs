using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AvaloniaPoint = Avalonia.Point;

namespace LeafClient.Controls;

public class SkinRendererControl : UserControl
{
    private WriteableBitmap? _renderTarget;
    private Avalonia.Controls.Image? _renderedImage;
    private Image<Rgba32>? _skinImage;
    private float _rotationY = 25f;
    private float _rotationX = -10f;
    private bool _isDragging;
    private AvaloniaPoint _lastMouse;
    private float _autoRotation;
    private bool _hasSkin;
    private bool _isRendering;
    private Image<Rgba32>? _capeImage;
    private Image<Rgba32>? _hatImage;
    private readonly DispatcherTimer _animTimer;

    // ── Cape physics ──────────────────────────────────────────────
    // Spring-damper: cape bottom swings laterally, lifts via pendulum effect,
    // and has a slow idle flutter so it never looks completely dead.
    // Cape ONLY responds to the user's manual drag — auto-rotation is ignored.
    private float _capeSwingX    = 0f;   // lateral bottom offset (world units)
    private float _capeSwingXVel = 0f;   // lateral velocity
    private float _idlePhase     = 0f;   // phase for the always-on gentle idle sway

    // ── Wing flap animation ─────────────────────────────────────
    private float _wingFlapPhase = 0f;   // radians, advances each frame

    // ── Zoom ──────────────────────────────────────────────────────
    private float _zoom = 1.0f;

    public SkinRendererControl()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _animTimer.Tick += (_, _) =>
        {
            if (!_isDragging)
            {
                _autoRotation += 0.8f;
                _rotationY = _autoRotation;
            }
            _wingFlapPhase += 0.10f;
            _auraPhase += 0.05f;
            UpdateCapePhysics(0f);
            RenderAsync();
        };

        PointerPressed += OnPointerPress;
        PointerMoved += OnPointerMove;
        PointerReleased += OnPointerRelease;
        PointerCaptureLost += (_, _) => _isDragging = false;
        PointerWheelChanged += OnPointerWheel;

        LayoutUpdated += (_, _) =>
        {
            if (Bounds.Width > 10 && Bounds.Height > 10 && _hasSkin)
            {
                RenderAsync();
                if (!_animTimer.IsEnabled) _animTimer.Start();
            }
        };
    }

    public void UpdateSkinTexture(byte[] pngData)
    {
        try
        {
            _skinImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            _hasSkin = true;
            _autoRotation = _rotationY;
            if (!_animTimer.IsEnabled) _animTimer.Start();
            RenderAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinRenderer] Failed to load skin: {ex.Message}");
        }
    }

    public void UpdateCapeTexture(byte[] pngData)
    {
        try
        {
            _capeImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            RenderAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinRenderer] Failed to load cape: {ex.Message}");
        }
    }

    public void ClearCape()
    {
        _capeImage = null;
        _capeSwingX    = 0f;
        _capeSwingXVel = 0f;
        RenderAsync();
    }

    /// <summary>
    /// Spring-damper physics for the cape.  Call every animation frame.
    /// rotDeltaDeg = how many degrees the avatar rotated this frame (signed).
    /// </summary>
    private void UpdateCapePhysics(float rotDeltaDeg)
    {
        // 1. Rotation velocity drives lateral swing — cape lags opposite to spin direction.
        //    0.20f gives a moderate lean during auto-rotation (~2 units steady-state),
        //    noticeable and natural without being excessive.
        _capeSwingXVel -= rotDeltaDeg * 0.20f;

        // 2. Idle flutter — slow sinusoidal nudge so cape never looks dead when still
        _idlePhase += 0.07f;
        _capeSwingXVel += MathF.Sin(_idlePhase) * 0.022f;

        // 3. Spring — pulls bottom back toward neutral
        _capeSwingXVel -= _capeSwingX * 0.08f;

        // 4. Damping — gentle, keeps nice oscillation before settling
        _capeSwingXVel *= 0.88f;

        // 5. Integrate
        _capeSwingX += _capeSwingXVel;
        _capeSwingX  = Math.Clamp(_capeSwingX, -6f, 6f);
    }

    public void UpdateHatTexture(byte[] pngData, bool isHorns = false)
    {
        try
        {
            _isHorns = isHorns;
            _hatImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            RenderAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinRenderer] Failed to load hat: {ex.Message}");
        }
    }

    public void ClearHat()
    {
        _hatImage = null;
        _isHorns = false;
        RenderAsync();
    }

    private Image<Rgba32>? _wingsImage;
    private bool _isAngelWings;  // false = demon (pointed fan), true = angel (curved feathered)
    private bool _isHorns;       // true = render hat as horns instead of crown

    public void UpdateWingsTexture(byte[] pngData, bool isAngel = false)
    {
        try
        {
            _isAngelWings = isAngel;
            _wingsImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            RenderAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinRenderer] Failed to load wings: {ex.Message}");
        }
    }

    public void ClearWings()
    {
        _wingsImage = null;
        RenderAsync();
    }

    // ── Preview light ─────────────────────────────────────────────
    private bool _previewLit;

    public void SetPreviewLight(bool lit)
    {
        _previewLit = lit;
        RenderAsync();
    }

    // ── Aura cosmetic ─────────────────────────────────────────────
    private string? _auraType;       // null = no aura, "darkness", "hearts", "flames"
    private float _auraPhase = 0f;   // advances each frame for orbit + float animation

    public void SetAura(string? auraType)
    {
        _auraType = auraType;
        RenderAsync();
    }

    public void ClearAura()
    {
        _auraType = null;
        RenderAsync();
    }

    private void OnPointerPress(object? s, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _lastMouse = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMove(object? s, PointerEventArgs e)
    {
        if (!_isDragging) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = false;
            return;
        }
        var pos = e.GetPosition(this);
        float dx = (float)(pos.X - _lastMouse.X);
        float dy = (float)(pos.Y - _lastMouse.Y);
        float prevY = _rotationY;
        _rotationY += dx * 0.8f;
        _rotationX = Math.Clamp(_rotationX - dy * 0.5f, -45f, 45f);
        _autoRotation = _rotationY;
        _lastMouse = pos;
        UpdateCapePhysics(_rotationY - prevY);
        RenderAsync();
        e.Handled = true;
    }


    private void OnPointerRelease(object? s, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerWheel(object? s, PointerWheelEventArgs e)
    {
        // Scroll up = zoom in, scroll down = zoom out
        _zoom = Math.Clamp(_zoom + (float)e.Delta.Y * 0.12f, 0.35f, 2.8f);
        RenderAsync();
        e.Handled = true;
    }

    private async void RenderAsync()
    {
        if (_skinImage == null || Bounds.Width < 10 || Bounds.Height < 10) return;
        if (_isRendering) return;
        _isRendering = true;

        try
        {
            int w = (int)Bounds.Width;
            int h = (int)Bounds.Height;
            float rotY = _rotationY;
            float rotX = _rotationX;
            var skinImage    = _skinImage;
            var capeImage    = _capeImage;
            var hatImage     = _hatImage;
            var wingsImage   = _wingsImage;
            float capeSwingX = _capeSwingX;
            float zoom       = _zoom;
            float wingFlap   = _wingFlapPhase;
            bool isAngel     = _isAngelWings;
            bool isHorns     = _isHorns;
            string? auraType = _auraType;
            float auraPhase  = _auraPhase;
            bool previewLit  = _previewLit;

            byte[]? buf = await Task.Run(() => RenderFrame(skinImage, capeImage, hatImage, wingsImage, w, h, rotY, rotX, zoom, capeSwingX, wingFlap, isAngel, isHorns, auraType, auraPhase, previewLit));

            if (buf != null)
            {
                if (_renderTarget == null || _renderTarget.PixelSize.Width != w || _renderTarget.PixelSize.Height != h)
                {
                    _renderTarget = new WriteableBitmap(
                        new PixelSize(w, h),
                        new Avalonia.Vector(96, 96),
                        Avalonia.Platform.PixelFormat.Bgra8888,
                        Avalonia.Platform.AlphaFormat.Premul);
                }

                using var fb = _renderTarget.Lock();
                Marshal.Copy(buf, 0, fb.Address, buf.Length);

                if (_renderedImage == null)
                {
                    _renderedImage = new Avalonia.Controls.Image
                    {
                        Source              = _renderTarget,
                        Stretch             = Stretch.None,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    };
                    Content = _renderedImage;
                }
                else
                {
                    _renderedImage.Source = _renderTarget;
                }
                InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SkinRenderer] RenderAsync error: {ex.Message}");
        }
        finally
        {
            _isRendering = false;
        }
    }

    // Exposed as internal so the cosmetic preview baker (Services/CosmeticPreviewBaker.cs)
    // can produce PNGs headlessly without instantiating the control or a window.
    internal static byte[]? RenderFrame(Image<Rgba32> skinImage, Image<Rgba32>? capeImage, Image<Rgba32>? hatImage, Image<Rgba32>? wingsImage, int w, int h, float rotationY, float rotationX, float zoom = 1f, float capeSwingX = 0f, float wingFlapPhase = 0f, bool isAngelWings = false, bool isHorns = false, string? auraType = null, float auraPhase = 0f, bool previewLit = false)
    {
        if (w <= 0 || h <= 0) return null;

        int stride = w * 4;
        var buf = new byte[h * stride];

        float radY = rotationY * MathF.PI / 180f;
        float radX = rotationX * MathF.PI / 180f;
        float cosY = MathF.Cos(radY), sinY = MathF.Sin(radY);
        float cosX = MathF.Cos(radX), sinX = MathF.Sin(radX);

        float scale = (h / 48f) * zoom;
        float cx = w / 2f;
        float cy = h * 0.48f;   // slight vertical centre shift so wings have room above

        Vector2 Project(Vector3 p)
        {
            float x1 = p.X * cosY - p.Z * sinY;
            float z1 = p.X * sinY + p.Z * cosY;
            float y1 = p.Y;
            float y2 = y1 * cosX - z1 * sinX;
            float z2 = y1 * sinX + z1 * cosX;
            float dist = 60f;
            float perspScale = dist / (dist + z2);
            return new Vector2(cx + x1 * scale * perspScale, cy - y2 * scale * perspScale);
        }

        float ProjectZ(Vector3 p)
        {
            float z1 = p.X * sinY + p.Z * cosY;
            float z2 = p.Y * sinX + z1 * cosX;
            return z2;
        }

        var zBuf = new float[h * w];
        Array.Fill(zBuf, float.MaxValue);

        Rgba32 GetSkinPixel(int sx, int sy)
        {
            if (sx < 0 || sx >= skinImage.Width || sy < 0 || sy >= skinImage.Height)
                return new Rgba32(0, 0, 0, 0);
            return skinImage[sx, sy];
        }

        void DrawFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                     int uvX, int uvY, int uvW, int uvH, float shade)
        {
            var stl = Project(tl); var str = Project(tr);
            var sbr = Project(br); var sbl = Project(bl);

            // Per-vertex Z for pixel-accurate depth interpolation
            float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
            float zbr = ProjectZ(br), zbl = ProjectZ(bl);

            float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
            float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
            float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
            float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));

            int x0 = Math.Max(0, (int)minX);
            int x1 = Math.Min(w - 1, (int)maxX + 1);
            int y0 = Math.Max(0, (int)minY);
            int y1 = Math.Min(h - 1, (int)maxY + 1);

            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    var pt = new Vector2(px + 0.5f, py + 0.5f);
                    float u, v;
                    float texU = 0, texV = 0;
                    float pixelZ = 0;
                    bool hit = false;
                    if (PointInTriangle(pt, stl, str, sbr, out u, out v))
                    {
                        // Triangle 1 (tl, tr, br): vertex UVs are (0,0), (1,0), (1,1)
                        // Barycentric weights: w_tl=1-u-v, w_tr=u, w_br=v
                        texU = u + v;
                        texV = v;
                        pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                        hit = true;
                    }
                    else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                    {
                        // Triangle 2 (tl, br, bl): vertex UVs are (0,0), (1,1), (0,1)
                        // Barycentric weights: w_tl=1-u-v, w_br=u, w_bl=v
                        texU = u;
                        texV = u + v;
                        pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                        hit = true;
                    }

                    if (hit)
                    {
                        int zIdx = py * w + px;
                        if (pixelZ > zBuf[zIdx]) continue;

                        int su = uvX + (int)(texU * (uvW - 0.01f));
                        int sv = uvY + (int)(texV * (uvH - 0.01f));
                        var color = GetSkinPixel(su, sv);
                        if (color.A > 0)
                        {
                            zBuf[zIdx] = pixelZ;
                            SetPixel(buf, stride, w, h, px, py, color, shade);
                        }
                    }
                }
            }
        }

        // Base layers
        DrawBodyPart(DrawFace, -4, -24, -2, 4, 12, 4, 0, 16, 4, 12, 4);     // right leg
        DrawBodyPart(DrawFace, 0, -24, -2, 4, 12, 4, 16, 48, 4, 12, 4);      // left leg
        // Body: skip side x-faces (hidden behind the arms, would z-fight with arm inner faces)
        DrawBodyPart(DrawFace, -4, -12, -2, 8, 12, 4, 16, 16, 8, 12, 4, 16 | 32);
        DrawBodyPart(DrawFace, -8, -12, -2, 4, 12, 4, 40, 16, 4, 12, 4);     // right arm
        DrawBodyPart(DrawFace, 4, -12, -2, 4, 12, 4, 32, 48, 4, 12, 4);      // left arm
        DrawBodyPart(DrawFace, -4, 0, -4, 8, 8, 8, 0, 0, 8, 8, 8);           // head

        // Overlay / second layers (slightly inflated to sit on top of base)
        float inf = 0.5f;
        // Head overlay (exists in both 64x32 and 64x64 skins)
        DrawBodyPart(DrawFace, -4-inf, -inf, -4-inf, 8+2*inf, 8+2*inf, 8+2*inf, 32, 0, 8, 8, 8);

        if (skinImage.Height >= 64)
        {
            DrawBodyPart(DrawFace, -4-inf, -24-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 0, 32, 4, 12, 4);     // right leg overlay
            DrawBodyPart(DrawFace, -inf, -24-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 0, 48, 4, 12, 4);        // left leg overlay
            DrawBodyPart(DrawFace, -4-inf, -12-inf, -2-inf, 8+2*inf, 12+2*inf, 4+2*inf, 16, 32, 8, 12, 4, 16 | 32);  // body overlay (skip side x-faces)
            DrawBodyPart(DrawFace, -8-inf, -12-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 40, 32, 4, 12, 4);     // right arm overlay
            DrawBodyPart(DrawFace, 4-inf, -12-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 48, 48, 4, 12, 4);      // left arm overlay
        }

        // ── Solid-color quad renderer (for wing bones, horns, etc.) ──────────────
        void DrawSolidFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                           byte r, byte g, byte b, float shade)
        {
            var stl = Project(tl); var str = Project(tr);
            var sbr = Project(br); var sbl = Project(bl);
            float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
            float zbr = ProjectZ(br), zbl = ProjectZ(bl);
            float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
            float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
            float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
            float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));
            int x0 = Math.Max(0, (int)minX);
            int x1 = Math.Min(w - 1, (int)maxX + 1);
            int y0 = Math.Max(0, (int)minY);
            int y1 = Math.Min(h - 1, (int)maxY + 1);
            var solidColor = new Rgba32(r, g, b, 255);
            for (int py = y0; py <= y1; py++)
            {
                for (int px2 = x0; px2 <= x1; px2++)
                {
                    var pt = new Vector2(px2 + 0.5f, py + 0.5f);
                    float u, v;
                    float pixelZ = 0;
                    bool hit = false;
                    if (PointInTriangle(pt, stl, str, sbr, out u, out v))
                    {
                        pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                        hit = true;
                    }
                    else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                    {
                        pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                        hit = true;
                    }
                    if (hit)
                    {
                        int zIdx = py * w + px2;
                        if (pixelZ > zBuf[zIdx]) continue;
                        zBuf[zIdx] = pixelZ;
                        SetPixel(buf, stride, w, h, px2, py, solidColor, shade);
                    }
                }
            }
        }

        // Helper: draw a 3D bone box between two endpoints with given thickness
        void DrawBoneBox(Vector3 from, Vector3 to, float thickness, byte br, byte bg, byte bb)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y, dz = to.Z - from.Z;
            float len = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 0.01f) return;
            dx /= len; dy /= len; dz /= len;
            float p1x = dy * 1f - dz * 0f;
            float p1y = dz * 0f - dx * 1f;
            float p1z = dx * 0f - dy * 0f;
            float p1len = MathF.Sqrt(p1x * p1x + p1y * p1y + p1z * p1z);
            if (p1len < 0.01f) { p1x = 1; p1y = 0; p1z = 0; p1len = 1; }
            p1x /= p1len; p1y /= p1len; p1z /= p1len;
            float p2x = dy * p1z - dz * p1y;
            float p2y = dz * p1x - dx * p1z;
            float p2z = dx * p1y - dy * p1x;
            float ht = thickness * 0.5f;
            var f0 = new Vector3(from.X + (-p1x - p2x) * ht, from.Y + (-p1y - p2y) * ht, from.Z + (-p1z - p2z) * ht);
            var f1 = new Vector3(from.X + ( p1x - p2x) * ht, from.Y + ( p1y - p2y) * ht, from.Z + ( p1z - p2z) * ht);
            var f2 = new Vector3(from.X + ( p1x + p2x) * ht, from.Y + ( p1y + p2y) * ht, from.Z + ( p1z + p2z) * ht);
            var f3 = new Vector3(from.X + (-p1x + p2x) * ht, from.Y + (-p1y + p2y) * ht, from.Z + (-p1z + p2z) * ht);
            var t0 = new Vector3(to.X + (-p1x - p2x) * ht, to.Y + (-p1y - p2y) * ht, to.Z + (-p1z - p2z) * ht);
            var t1 = new Vector3(to.X + ( p1x - p2x) * ht, to.Y + ( p1y - p2y) * ht, to.Z + ( p1z - p2z) * ht);
            var t2 = new Vector3(to.X + ( p1x + p2x) * ht, to.Y + ( p1y + p2y) * ht, to.Z + ( p1z + p2z) * ht);
            var t3 = new Vector3(to.X + (-p1x + p2x) * ht, to.Y + (-p1y + p2y) * ht, to.Z + (-p1z + p2z) * ht);
            DrawSolidFace(f0, f1, t1, t0, br, bg, bb, 0.70f);
            DrawSolidFace(f1, f2, t2, t1, br, bg, bb, 0.85f);
            DrawSolidFace(f2, f3, t3, t2, br, bg, bb, 0.70f);
            DrawSolidFace(f3, f0, t0, t3, br, bg, bb, 0.55f);
            DrawSolidFace(f0, f1, f2, f3, br, bg, bb, 0.60f);
            DrawSolidFace(t1, t0, t3, t2, br, bg, bb, 0.60f);
        }

        // ── Dragon wings (drawn before hat/cape so the body occludes wing edges) ──
        if (wingsImage != null)
        {
            int wingTexW = wingsImage.Width;   // 64
            int wingTexH = wingsImage.Height;  // 32
            int wingHalf = wingTexW / 2;       // 32 — left half = outer face design

            Rgba32 GetWingPixel(int sx, int sy)
            {
                if (sx < 0 || sx >= wingsImage.Width || sy < 0 || sy >= wingsImage.Height)
                    return new Rgba32(0, 0, 0, 0);
                return wingsImage[sx, sy];
            }

            void DrawWingFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                              int uvX, int uvY, int uvW, int uvH, float shade)
            {
                var stl = Project(tl); var str = Project(tr);
                var sbr = Project(br); var sbl = Project(bl);
                float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
                float zbr = ProjectZ(br), zbl = ProjectZ(bl);
                float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
                float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
                float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
                float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));
                int x0 = Math.Max(0, (int)minX);
                int x1 = Math.Min(w - 1, (int)maxX + 1);
                int y0 = Math.Max(0, (int)minY);
                int y1 = Math.Min(h - 1, (int)maxY + 1);
                for (int py = y0; py <= y1; py++)
                {
                    for (int px2 = x0; px2 <= x1; px2++)
                    {
                        var pt = new Vector2(px2 + 0.5f, py + 0.5f);
                        float u, v;
                        float texU = 0, texV = 0, pixelZ = 0;
                        bool hit = false;
                        if (PointInTriangle(pt, stl, str, sbr, out u, out v))
                        {
                            texU = u + v; texV = v;
                            pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                            hit = true;
                        }
                        else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                        {
                            texU = u; texV = u + v;
                            pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                            hit = true;
                        }
                        if (hit)
                        {
                            int zIdx = py * w + px2;
                            if (pixelZ > zBuf[zIdx]) continue;
                            int su = uvX + (int)(texU * (uvW - 0.01f));
                            int sv = uvY + (int)(texV * (uvH - 0.01f));
                            var color = GetWingPixel(su, sv);
                            if (color.A > 0) { zBuf[zIdx] = pixelZ; SetPixel(buf, stride, w, h, px2, py, color, shade); }
                        }
                    }
                }
            }


            // ── Wing cosmetics ─────────────────────────────────────────────
            // Two shapes: Demon (pointed fan) and Angel (smooth curved feathers)

            byte boneR = 45, boneG = 8, boneB = 8;

            void RenderWing(float sideSign)
            {
                float s = sideSign;

                // ── Flap animation (shared by both shapes) ──
                float t = wingFlapPhase;
                float wristWave = MathF.Sin(t) * 1.8f;
                float tipWave   = MathF.Sin(t - 0.6f) * 3.5f;
                float tipBendZ  = MathF.Cos(t - 0.4f) * 2.5f;

                if (!isAngelWings)
                {
                    // ══════ DEMON WINGS — pointed bat-hand fan ══════
                    var back  = new Vector3(s * 1.5f,  -1.0f, -2.5f);
                    var wrist = new Vector3(s * 9.0f,  2.5f + wristWave, -4.5f);
                    var f1 = new Vector3(s * 17.0f,   9.0f + tipWave,        -2.0f + tipBendZ);
                    var f2 = new Vector3(s * 20.0f,   3.0f + tipWave,        -3.0f + tipBendZ);
                    var f3 = new Vector3(s * 18.0f,  -4.0f + tipWave * 0.7f, -4.0f + tipBendZ * 0.6f);
                    var f4 = new Vector3(s * 13.0f, -10.5f + tipWave * 0.4f, -4.5f + tipBendZ * 0.3f);
                    var waist = new Vector3(s * 3.0f, -8.5f, -2.5f);

                    // Membrane fan sections
                    DrawWingFace(wrist, f1, f2, wrist, 0, 0, wingHalf, wingTexH, 0.95f);
                    DrawWingFace(wrist, f2, f3, wrist, 0, 0, wingHalf, wingTexH, 0.88f);
                    DrawWingFace(wrist, f3, f4, wrist, 0, 0, wingHalf, wingTexH, 0.80f);
                    DrawWingFace(back, wrist, f4, waist, 0, 0, wingHalf, wingTexH, 0.72f);

                    // Bones
                    DrawBoneBox(back, wrist, 1.8f, boneR, boneG, boneB);
                    DrawBoneBox(wrist, f1, 1.2f, boneR, boneG, boneB);
                    DrawBoneBox(wrist, f2, 1.2f, boneR, boneG, boneB);
                    DrawBoneBox(wrist, f3, 1.2f, boneR, boneG, boneB);
                    DrawBoneBox(wrist, f4, 1.2f, boneR, boneG, boneB);
                }
                else
                {
                    // ══════ ANGEL WINGS — curved feathered shape ══════
                    // Approximates curved edges by using many small quads
                    // arranged along a smooth arc. 3 feather layers, each
                    // built from 6 arc segments creating a rounded outline.

                    var back = new Vector3(s * 1.5f, -1.0f, -2.5f);

                    // Arc helper: generates points along an elliptical curve
                    // from startAngle to endAngle (in radians), centered at cx,cy
                    // radiusX/radiusY define the ellipse size
                    int arcSegs = 6;

                    // ── LAYER 1: Primary flight feathers (outermost, largest) ──
                    // Arc from top (60°) sweeping down to bottom (-70°)
                    float l1_cx = s * 3.0f, l1_cy = -1.0f;
                    float l1_rx = 15.0f, l1_ry = 10.0f;
                    float l1_startA = 1.05f, l1_endA = -1.22f; // ~60° to ~-70°
                    for (int i = 0; i < arcSegs; i++)
                    {
                        float a0 = l1_startA + (l1_endA - l1_startA) * i / arcSegs;
                        float a1 = l1_startA + (l1_endA - l1_startA) * (i + 1) / arcSegs;
                        // Outer arc points
                        float ox0 = l1_cx + s * MathF.Cos(a0) * l1_rx;
                        float oy0 = l1_cy + MathF.Sin(a0) * l1_ry;
                        float ox1 = l1_cx + s * MathF.Cos(a1) * l1_rx;
                        float oy1 = l1_cy + MathF.Sin(a1) * l1_ry;
                        // Inner arc points (40% of radius — creates feather width)
                        float ix0 = l1_cx + s * MathF.Cos(a0) * l1_rx * 0.4f;
                        float iy0 = l1_cy + MathF.Sin(a0) * l1_ry * 0.4f;
                        float ix1 = l1_cx + s * MathF.Cos(a1) * l1_rx * 0.4f;
                        float iy1 = l1_cy + MathF.Sin(a1) * l1_ry * 0.4f;
                        // Flap animation (outer moves more)
                        float anim = tipWave * (0.5f + 0.5f * (float)i / arcSegs);
                        float animZ = tipBendZ * (0.3f + 0.4f * (float)i / arcSegs);
                        var it = new Vector3(ix0, iy0 + wristWave * 0.3f, -2.8f);
                        var ot = new Vector3(ox0, oy0 + anim, -3.5f + animZ);
                        var ob = new Vector3(ox1, oy1 + anim, -3.5f + animZ);
                        var ib = new Vector3(ix1, iy1 + wristWave * 0.3f, -2.8f);
                        float shade = 0.95f - i * 0.02f;
                        DrawWingFace(it, ot, ob, ib, 0, 0, wingHalf, wingTexH, shade);
                    }

                    // ── LAYER 2: Secondary feathers (middle, slightly smaller) ──
                    float l2_rx = 11.0f, l2_ry = 8.0f;
                    float l2_startA = 0.85f, l2_endA = -1.05f;
                    for (int i = 0; i < arcSegs; i++)
                    {
                        float a0 = l2_startA + (l2_endA - l2_startA) * i / arcSegs;
                        float a1 = l2_startA + (l2_endA - l2_startA) * (i + 1) / arcSegs;
                        float ox0 = l1_cx + s * MathF.Cos(a0) * l2_rx;
                        float oy0 = l1_cy + MathF.Sin(a0) * l2_ry;
                        float ox1 = l1_cx + s * MathF.Cos(a1) * l2_rx;
                        float oy1 = l1_cy + MathF.Sin(a1) * l2_ry;
                        float ix0 = l1_cx + s * MathF.Cos(a0) * l2_rx * 0.3f;
                        float iy0 = l1_cy + MathF.Sin(a0) * l2_ry * 0.35f;
                        float ix1 = l1_cx + s * MathF.Cos(a1) * l2_rx * 0.3f;
                        float iy1 = l1_cy + MathF.Sin(a1) * l2_ry * 0.35f;
                        float anim = tipWave * (0.3f + 0.3f * (float)i / arcSegs);
                        float animZ = tipBendZ * (0.2f + 0.2f * (float)i / arcSegs);
                        var it = new Vector3(ix0, iy0 + wristWave * 0.2f, -2.6f);
                        var ot = new Vector3(ox0, oy0 + anim, -3.2f + animZ);
                        var ob = new Vector3(ox1, oy1 + anim, -3.2f + animZ);
                        var ib = new Vector3(ix1, iy1 + wristWave * 0.2f, -2.6f);
                        DrawWingFace(it, ot, ob, ib, 0, 0, wingHalf, wingTexH, 0.88f - i * 0.01f);
                    }

                    // ── LAYER 3: Covert feathers (innermost, small, close to body) ──
                    float l3_rx = 7.0f, l3_ry = 5.5f;
                    float l3_startA = 0.7f, l3_endA = -0.8f;
                    for (int i = 0; i < 4; i++)
                    {
                        float a0 = l3_startA + (l3_endA - l3_startA) * i / 4;
                        float a1 = l3_startA + (l3_endA - l3_startA) * (i + 1) / 4;
                        float ox0 = l1_cx + s * MathF.Cos(a0) * l3_rx;
                        float oy0 = l1_cy + MathF.Sin(a0) * l3_ry;
                        float ox1 = l1_cx + s * MathF.Cos(a1) * l3_rx;
                        float oy1 = l1_cy + MathF.Sin(a1) * l3_ry;
                        float ix0 = l1_cx + s * MathF.Cos(a0) * l3_rx * 0.25f;
                        float iy0 = l1_cy + MathF.Sin(a0) * l3_ry * 0.3f;
                        float ix1 = l1_cx + s * MathF.Cos(a1) * l3_rx * 0.25f;
                        float iy1 = l1_cy + MathF.Sin(a1) * l3_ry * 0.3f;
                        var it = new Vector3(ix0, iy0 + wristWave * 0.1f, -2.5f);
                        var ot = new Vector3(ox0, oy0 + tipWave * 0.15f, -2.9f + tipBendZ * 0.1f);
                        var ob = new Vector3(ox1, oy1 + tipWave * 0.15f, -2.9f + tipBendZ * 0.1f);
                        var ib = new Vector3(ix1, iy1 + wristWave * 0.1f, -2.5f);
                        DrawWingFace(it, ot, ob, ib, 0, 0, wingHalf, wingTexH, 0.80f);
                    }
                }
            }

            RenderWing(+1f);
            RenderWing(-1f);
        }

        // ── Hat cosmetic (drawn after skin overlays so it sits on top of the head) ──
        if (hatImage != null)
        {
            Rgba32 GetHatPixel(int sx, int sy)
            {
                if (sx < 0 || sx >= hatImage.Width || sy < 0 || sy >= hatImage.Height)
                    return new Rgba32(0, 0, 0, 0);
                return hatImage[sx, sy];
            }

            // Same rasterizer as DrawFace, but sampling from hatImage
            void DrawHatFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                             int uvX, int uvY, int uvW, int uvH, float shade)
            {
                var stl = Project(tl); var str = Project(tr);
                var sbr = Project(br); var sbl = Project(bl);
                float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
                float zbr = ProjectZ(br), zbl = ProjectZ(bl);
                float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
                float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
                float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
                float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));
                int x0 = Math.Max(0, (int)minX);
                int x1 = Math.Min(w - 1, (int)maxX + 1);
                int y0 = Math.Max(0, (int)minY);
                int y1 = Math.Min(h - 1, (int)maxY + 1);
                for (int py = y0; py <= y1; py++)
                {
                    for (int px = x0; px <= x1; px++)
                    {
                        var pt = new Vector2(px + 0.5f, py + 0.5f);
                        float u, v;
                        float texU = 0, texV = 0, pixelZ = 0;
                        bool hit = false;
                        if (PointInTriangle(pt, stl, str, sbr, out u, out v))
                        {
                            texU = u + v; texV = v;
                            pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                            hit = true;
                        }
                        else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                        {
                            texU = u; texV = u + v;
                            pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                            hit = true;
                        }
                        if (hit)
                        {
                            int zIdx = py * w + px;
                            if (pixelZ > zBuf[zIdx]) continue;
                            int su = uvX + (int)(texU * (uvW - 0.01f));
                            int sv = uvY + (int)(texV * (uvH - 0.01f));
                            var color = GetHatPixel(su, sv);
                            if (color.A > 0) { zBuf[zIdx] = pixelZ; SetPixel(buf, stride, w, h, px, py, color, shade); }
                        }
                    }
                }
            }

            if (!isHorns)
            {
                // Hat = slightly inflated head box using standard Minecraft head UV
                float hatInf = 0.8f;
                DrawBodyPart(DrawHatFace,
                    -4 - hatInf, 5,           -4 - hatInf,
                    8 + 2 * hatInf, 3 + hatInf, 8 + 2 * hatInf,
                    0, 0, 8, 8, 8);
            }
            else
            {
                // ══════ SHADOW HORNS — two curved horns rising from the head ══════
                // Each horn is built from 4 connected bone segments curving outward and up,
                // with a slight forward tilt. Horns start at the sides of the head top.
                // Head top is at y=8, head spans x=-4..+4, z=-4..+4

                // Sample average colors from the horn texture for base/mid/tip
                byte hBaseR = 35, hBaseG = 30, hBaseB = 35;   // dark charcoal base
                byte hMidR  = 50, hMidG  = 40, hMidB  = 45;   // slightly lighter mid
                byte hTipR  = 90, hTipG  = 30, hTipB  = 25;   // reddish glow tip

                // Try to sample actual colors from the texture
                if (hatImage.Width >= 8 && hatImage.Height >= 8)
                {
                    var cBase = hatImage[4, 28]; // bottom of texture = base
                    hBaseR = cBase.R; hBaseG = cBase.G; hBaseB = cBase.B;
                    var cMid = hatImage[4, 16];  // middle
                    hMidR = cMid.R; hMidG = cMid.G; hMidB = cMid.B;
                    var cTip = hatImage[4, 4];   // top = tip
                    hTipR = cTip.R; hTipG = cTip.G; hTipB = cTip.B;
                }

                void RenderHorn(float sideSign)
                {
                    float s = sideSign;
                    // Horn starts at the side-top of the head and curves outward+up
                    // 4 segments: base→s1→s2→s3→tip
                    var p0 = new Vector3(s * 3.0f,  7.5f, -0.5f);  // base (on head top, slightly back)
                    var p1 = new Vector3(s * 4.5f, 10.0f, -1.0f);  // first curve out
                    var p2 = new Vector3(s * 5.5f, 13.0f, -0.5f);  // mid curve
                    var p3 = new Vector3(s * 5.0f, 15.5f,  0.5f);  // upper curve (starts bending inward)
                    var p4 = new Vector3(s * 4.0f, 17.5f,  1.0f);  // tip (curves slightly forward+inward)

                    // Thicknesses taper from base to tip
                    DrawBoneBox(p0, p1, 2.2f, hBaseR, hBaseG, hBaseB);
                    DrawBoneBox(p1, p2, 1.8f, hBaseR, hBaseG, hBaseB);
                    DrawBoneBox(p2, p3, 1.3f, hMidR,  hMidG,  hMidB);
                    DrawBoneBox(p3, p4, 0.8f, hTipR,  hTipG,  hTipB);
                }

                RenderHorn(+1f);
                RenderHorn(-1f);
            }
        }

        // ── Cape (drawn last so z-buffer lets it appear behind the body's back face) ──
        if (capeImage != null)
        {
            Rgba32 GetCapePixel(int sx, int sy)
            {
                if (sx < 0 || sx >= capeImage.Width || sy < 0 || sy >= capeImage.Height)
                    return new Rgba32(0, 0, 0, 0);
                return capeImage[sx, sy];
            }

            // Reuse the same rasterization logic as DrawFace, but sampling from capeImage
            void DrawCapeFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                             int uvX, int uvY, int uvW, int uvH, float shade)
            {
                var stl = Project(tl); var str = Project(tr);
                var sbr = Project(br); var sbl = Project(bl);
                float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
                float zbr = ProjectZ(br), zbl = ProjectZ(bl);
                float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
                float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
                float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
                float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));
                int x0 = Math.Max(0, (int)minX);
                int x1 = Math.Min(w - 1, (int)maxX + 1);
                int y0 = Math.Max(0, (int)minY);
                int y1 = Math.Min(h - 1, (int)maxY + 1);
                for (int py = y0; py <= y1; py++)
                {
                    for (int px = x0; px <= x1; px++)
                    {
                        var pt = new Vector2(px + 0.5f, py + 0.5f);
                        float u, v;
                        float texU = 0, texV = 0, pixelZ = 0;
                        bool hit = false;
                        if (PointInTriangle(pt, stl, str, sbr, out u, out v))
                        {
                            texU = u + v; texV = v;
                            pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                            hit = true;
                        }
                        else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                        {
                            texU = u; texV = u + v;
                            pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                            hit = true;
                        }
                        if (hit)
                        {
                            int zIdx = py * w + px;
                            if (pixelZ > zBuf[zIdx]) continue;
                            int su = uvX + (int)(texU * (uvW - 0.01f));
                            int sv = uvY + (int)(texV * (uvH - 0.01f));
                            var color = GetCapePixel(su, sv);
                            if (color.A > 0) { zBuf[zIdx] = pixelZ; SetPixel(buf, stride, w, h, px, py, color, shade); }
                        }
                    }
                }
            }

            // Cape UV layout (custom — not standard Minecraft):
            //   Left  half of texture → outer face (design visible from behind)
            //   Right half of texture → inner face (visible from front, darker)
            //
            // Physics: the cape is split into two segments at y=-4 (¼ of the way down).
            // The top segment is rigidly attached to the player's back.
            // The bottom segment's lower edge swings laterally by capeSwingX units and
            // billows slightly backward (more negative z) when the swing is large.

            int halfW = capeImage.Width  / 2;
            int texH  = capeImage.Height;

            // UV splits proportional to 3D segment heights (4 / 6 / 6 of 16 units)
            int uv1 = texH * 4 / 16;          // rows for seg 1 (y=0  → y=-4)
            int uv2 = texH * 6 / 16;          // rows for seg 2 (y=-4 → y=-10)
            int uv3 = texH - uv1 - uv2;       // rows for seg 3 (y=-10 → y=-16)
            int uv12 = uv1 + uv2;

            // Swing amounts at each bend point (quadratic distribution for natural wave)
            float s1 = capeSwingX * 0.10f;    // y=-4   almost attached
            float s2 = capeSwingX * 0.52f;    // y=-10  mid wave
            float s3 = capeSwingX;             // y=-16  full swing

            // Pendulum Y-lift: bottom rises as it swings out (physically accurate)
            float liftY = MathF.Abs(capeSwingX) * 0.38f;

            // Backward billow: cape bows away from body when swinging hard
            float b1 = MathF.Abs(capeSwingX) * 0.10f;
            float b2 = MathF.Abs(capeSwingX) * 0.22f;

            // ── Outer face — 3 segments ──────────────────────────────────────────
            DrawCapeFace(   // Seg 1: nearly rigid (shoulder attachment)
                new Vector3(-5f,     0f,  -2.5f),
                new Vector3( 5f,     0f,  -2.5f),
                new Vector3( 5f+s1, -4f,  -2.5f),
                new Vector3(-5f+s1, -4f,  -2.5f),
                0, 0, halfW, uv1, 0.85f);

            DrawCapeFace(   // Seg 2: mid wave
                new Vector3(-5f+s1,  -4f,  -2.5f),
                new Vector3( 5f+s1,  -4f,  -2.5f),
                new Vector3( 5f+s2, -10f,  -2.5f - b1),
                new Vector3(-5f+s2, -10f,  -2.5f - b1),
                0, uv1, halfW, uv2, 0.85f);

            DrawCapeFace(   // Seg 3: full swing + pendulum lift
                new Vector3(-5f+s2,       -10f,  -2.5f - b1),
                new Vector3( 5f+s2,       -10f,  -2.5f - b1),
                new Vector3( 5f+s3, -16f+liftY,  -2.5f - b2),
                new Vector3(-5f+s3, -16f+liftY,  -2.5f - b2),
                0, uv12, halfW, uv3, 0.85f);

            // ── Inner face — 3 segments (reversed winding) ───────────────────────
            DrawCapeFace(   // Seg 1
                new Vector3( 5f,     0f,  -3.5f),
                new Vector3(-5f,     0f,  -3.5f),
                new Vector3(-5f+s1, -4f,  -3.5f),
                new Vector3( 5f+s1, -4f,  -3.5f),
                halfW, 0, halfW, uv1, 0.30f);

            DrawCapeFace(   // Seg 2
                new Vector3( 5f+s1,  -4f,  -3.5f),
                new Vector3(-5f+s1,  -4f,  -3.5f),
                new Vector3(-5f+s2, -10f,  -3.5f - b1),
                new Vector3( 5f+s2, -10f,  -3.5f - b1),
                halfW, uv1, halfW, uv2, 0.30f);

            DrawCapeFace(   // Seg 3
                new Vector3( 5f+s2,       -10f,  -3.5f - b1),
                new Vector3(-5f+s2,       -10f,  -3.5f - b1),
                new Vector3(-5f+s3, -16f+liftY,  -3.5f - b2),
                new Vector3( 5f+s3, -16f+liftY,  -3.5f - b2),
                halfW, uv12, halfW, uv3, 0.30f);
        }

        // ── Aura cosmetic — animated particles orbiting the character ──────
        if (auraType != null)
        {
            int particleCount = 12;
            float t = auraPhase;

            if (auraType == "darkness")
            {
                // Dark floating shards orbiting the body — like Lunar's Darkness Aura
                for (int i = 0; i < particleCount; i++)
                {
                    float angle = t * 1.2f + i * (MathF.PI * 2f / particleCount);
                    float yOff = MathF.Sin(t * 0.8f + i * 0.7f) * 8f;
                    float radius = 8f + MathF.Sin(t * 0.5f + i * 1.1f) * 3f;
                    float px = MathF.Cos(angle) * radius;
                    float pz = MathF.Sin(angle) * radius;
                    float py = -2f + yOff; // centered around torso

                    // Each shard is a small tilted quad
                    float shardSize = 1.2f + MathF.Sin(t + i * 2.3f) * 0.5f;
                    float tiltA = angle + 0.5f;
                    float sx = MathF.Cos(tiltA) * shardSize;
                    float sz = MathF.Sin(tiltA) * shardSize;
                    float sy = shardSize * 0.7f;

                    var c  = new Vector3(px, py, pz);
                    var p0 = new Vector3(px - sx, py - sy, pz - sz);
                    var p1 = new Vector3(px + sz * 0.5f, py - sy * 0.3f, pz - sx * 0.5f);
                    var p2 = new Vector3(px + sx, py + sy, pz + sz);
                    var p3 = new Vector3(px - sz * 0.5f, py + sy * 0.3f, pz + sx * 0.5f);

                    // Dark charcoal to deep purple shards
                    byte sr = (byte)(25 + (i * 7) % 20);
                    byte sg = (byte)(10 + (i * 3) % 15);
                    byte sb = (byte)(30 + (i * 11) % 25);
                    float shade = 0.6f + MathF.Sin(t * 2f + i) * 0.2f;
                    DrawSolidFace(p0, p1, p2, p3, sr, sg, sb, shade);
                }
            }
            else if (auraType == "hearts")
            {
                // Broken heart — NO gap between halves. The crack is shown
                // purely by darkening the pixels at the zigzag boundary.
                // 1 = left half, 2 = right half. They sit directly adjacent.
                // 8 wide x 7 tall.
                int[,] brokenHeart = {
                    {0,1,1,0,0,2,2,0},  // row 0: top bumps
                    {1,1,1,1,2,2,2,2},  // row 1: boundary at col 3|4
                    {1,1,1,1,1,2,2,2},  // row 2: zig RIGHT col 4|5
                    {0,1,1,1,2,2,2,0},  // row 3: zag LEFT col 3|4
                    {0,0,1,1,1,2,2,0},  // row 4: zig RIGHT col 4|5
                    {0,0,0,1,2,2,0,0},  // row 5: zag LEFT col 3|4
                    {0,0,0,1,2,0,0,0},  // row 6: bottom tips
                };
                int hRows = 7, hCols = 8;
                float centerCol = 3.5f;

                particleCount = 6;
                for (int i = 0; i < particleCount; i++)
                {
                    float angle = t * 0.8f + i * (MathF.PI * 2f / particleCount);
                    float yOff = MathF.Sin(t * 0.55f + i * 0.85f) * 7f;
                    float radius = 9f + MathF.Sin(t * 0.4f + i * 1.3f) * 2.5f;
                    float cx2 = MathF.Cos(angle) * radius;
                    float cz2 = MathF.Sin(angle) * radius;
                    float cy2 = -1f + yOff;

                    float pulse = 1.0f + MathF.Sin(t * 3f + i * 1.2f) * 0.12f;
                    float pxSize = 0.42f * pulse;

                    byte hr = (byte)(200 + (i * 11) % 50);
                    byte hg = (byte)(15 + (i * 3) % 20);
                    byte hb = (byte)(25 + (i * 5) % 20);
                    byte drk = (byte)(hr * 0.45f);
                    byte dkg = (byte)(hg * 0.3f);
                    byte dkb = (byte)(hb * 0.3f);
                    float shade = 0.85f + MathF.Sin(t * 1.8f + i * 0.5f) * 0.1f;

                    for (int row = 0; row < hRows; row++)
                    {
                        for (int col = 0; col < hCols; col++)
                        {
                            int val = brokenHeart[row, col];
                            if (val == 0) continue;

                            float lx = (col - centerCol) * pxSize;
                            float ly = (3f - row) * pxSize;

                            // Darken pixels right at the crack boundary
                            bool isCrackEdge = false;
                            if (val == 1 && col + 1 < hCols && brokenHeart[row, col + 1] == 2) isCrackEdge = true;
                            if (val == 2 && col - 1 >= 0 && brokenHeart[row, col - 1] == 1) isCrackEdge = true;

                            byte pr = isCrackEdge ? drk : hr;
                            byte pg = isCrackEdge ? dkg : hg;
                            byte pb = isCrackEdge ? dkb : hb;

                            var q0 = new Vector3(cx2 + lx,          cy2 + ly + pxSize, cz2);
                            var q1 = new Vector3(cx2 + lx + pxSize, cy2 + ly + pxSize, cz2);
                            var q2 = new Vector3(cx2 + lx + pxSize, cy2 + ly,          cz2);
                            var q3 = new Vector3(cx2 + lx,          cy2 + ly,          cz2);
                            DrawSolidFace(q0, q1, q2, q3, pr, pg, pb, shade);
                        }
                    }
                }
            }
            else if (auraType == "flames")
            {
                // Fire emoji 🔥 shapes orbiting the player + orange ember particles
                // rising from the body surface.

                // Pixel-art fire shape (8 wide x 10 tall), 1=orange, 2=yellow core, 3=red base
                int[,] fireGrid = {
                    {0,0,0,2,0,0,0,0},
                    {0,0,2,2,0,0,0,0},
                    {0,0,2,2,2,0,0,0},
                    {0,2,2,2,2,0,1,0},
                    {0,1,2,2,1,0,1,0},
                    {1,1,1,2,1,1,1,0},
                    {1,1,1,1,1,1,1,0},
                    {1,3,1,1,1,3,1,0},
                    {0,3,3,1,3,3,0,0},
                    {0,0,3,3,3,0,0,0},
                };

                // 5 fire emoji shapes orbiting the player
                int fireCount = 5;
                for (int i = 0; i < fireCount; i++)
                {
                    float angle = t * 1.2f + i * (MathF.PI * 2f / fireCount);
                    float radius = 9f + MathF.Sin(t * 0.5f + i * 1.1f) * 2f;
                    float fx = MathF.Cos(angle) * radius;
                    float fz = MathF.Sin(angle) * radius;
                    float fy = -4f + MathF.Sin(t * 0.7f + i * 0.9f) * 5f;

                    float flicker = 1.0f + MathF.Sin(t * 4f + i * 2.3f) * 0.08f;
                    float pxSize = 0.4f * flicker;

                    for (int row = 0; row < 10; row++)
                    {
                        for (int col = 0; col < 8; col++)
                        {
                            int val = fireGrid[row, col];
                            if (val == 0) continue;

                            float lx = (col - 4f) * pxSize;
                            float ly = (5f - row) * pxSize; // top of fire points up

                            var q0 = new Vector3(fx + lx,          fy + ly + pxSize, fz);
                            var q1 = new Vector3(fx + lx + pxSize, fy + ly + pxSize, fz);
                            var q2 = new Vector3(fx + lx + pxSize, fy + ly,          fz);
                            var q3 = new Vector3(fx + lx,          fy + ly,          fz);

                            byte cr, cg, cb;
                            float sh;
                            if (val == 2) { cr = 255; cg = 220; cb = 50; sh = 0.95f; }      // yellow core
                            else if (val == 3) { cr = 200; cg = 50; cb = 10; sh = 0.75f; }   // red base
                            else { cr = 255; cg = 130; cb = 20; sh = 0.88f; }                 // orange body

                            DrawSolidFace(q0, q1, q2, q3, cr, cg, cb, sh);
                        }
                    }
                }

                // Orange ember particles rising from the body surface
                for (int e = 0; e < 12; e++)
                {
                    // Each ember has a looping lifecycle
                    float life = (t * 1.6f + e * 0.52f) % 2.5f;
                    float frac = life / 2.5f; // 0..1

                    // Start position: random spot on body surface
                    float seed = e * 137.5f; // golden angle for spread
                    float startX = MathF.Sin(seed) * 3.5f;
                    float startZ = MathF.Cos(seed) * 2f;
                    float startY = -10f + (e % 5) * 4.5f;

                    // Rise upward and drift outward
                    float ex = startX + MathF.Sin(t * 0.8f + e * 1.4f) * frac * 3f;
                    float ez = startZ + MathF.Cos(t * 0.6f + e * 1.7f) * frac * 2f;
                    float ey = startY + frac * 12f;

                    float emberSize = (1f - frac) * 0.6f + 0.15f;
                    float dim = 1f - frac * 0.7f;

                    // Color: bright orange-yellow fading to red
                    byte er = 255;
                    byte eg = (byte)(180 * dim);
                    byte eb = (byte)(30 * dim);

                    var p0 = new Vector3(ex - emberSize, ey - emberSize, ez);
                    var p1 = new Vector3(ex + emberSize, ey - emberSize, ez);
                    var p2 = new Vector3(ex + emberSize, ey + emberSize, ez);
                    var p3 = new Vector3(ex - emberSize, ey + emberSize, ez);
                    DrawSolidFace(p0, p1, p2, p3, er, eg, eb, 0.8f * dim);
                }
            }
        }

        // ── Preview light — top-down spotlight brightening ──────────────
        if (previewLit)
        {
            // Simulate a soft overhead light:
            // - Strongest at the top of the frame, fading toward the bottom
            // - Only affects pixels that have been drawn (alpha > 0)
            for (int py = 0; py < h; py++)
            {
                // Light intensity: 1.0 at top, fading to ~0.35 boost at bottom
                float vertFrac = 1f - (float)py / h;
                float boost = 1.35f + vertFrac * 0.65f;  // 1.35 at bottom, 2.0 at top

                for (int px = 0; px < w; px++)
                {
                    int off = py * stride + px * 4;
                    byte a = buf[off + 3];
                    if (a == 0) continue;  // skip transparent pixels

                    buf[off + 0] = (byte)Math.Min(255, (int)(buf[off + 0] * boost)); // B
                    buf[off + 1] = (byte)Math.Min(255, (int)(buf[off + 1] * boost)); // G
                    buf[off + 2] = (byte)Math.Min(255, (int)(buf[off + 2] * boost)); // R
                }
            }
        }

        return buf;
    }

    // skipFaces bitmask: 1=top, 2=bottom, 4=front, 8=back, 16=right(x=x0), 32=left(x=x1)
    private static void DrawBodyPart(
        Action<Vector3, Vector3, Vector3, Vector3, int, int, int, int, float> drawFace,
        float ox, float oy, float oz, float sx, float sy, float sz,
        int uvX, int uvY, int uvW, int uvH, int uvD, int skipFaces = 0)
    {
        float x0 = ox, x1 = ox + sx;
        float y0 = oy, y1 = oy + sy;
        float z0 = oz, z1 = oz + sz;

        if ((skipFaces & 1) == 0)
            drawFace(
                new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
                uvX + uvD, uvY, uvW, uvD, 0.85f);

        if ((skipFaces & 2) == 0)
            drawFace(
                new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
                new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
                uvX + uvD + uvW, uvY, uvW, uvD, 0.5f);

        if ((skipFaces & 4) == 0)
            drawFace(
                new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
                uvX + uvD, uvY + uvD, uvW, uvH, 1.0f);

        if ((skipFaces & 8) == 0)
            drawFace(
                new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
                new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
                uvX + uvD + uvW + uvD, uvY + uvD, uvW, uvH, 0.55f);

        if ((skipFaces & 16) == 0)
            drawFace(
                new Vector3(x0, y1, z0), new Vector3(x0, y1, z1),
                new Vector3(x0, y0, z1), new Vector3(x0, y0, z0),
                uvX, uvY + uvD, uvD, uvH, 0.7f);

        if ((skipFaces & 32) == 0)
            drawFace(
                new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
                new Vector3(x1, y0, z0), new Vector3(x1, y0, z1),
                uvX + uvD + uvW, uvY + uvD, uvD, uvH, 0.7f);
    }
    private static void DrawBodyPart(
        Action<Vector3, Vector3, Vector3, Vector3, int, int, int, int, float> drawFace,
        float ox, float oy, float oz, float sx, float sy, float sz,
        int uvX, int uvY, int uvW, int uvH, int uvD,
        bool facingRight, bool facingFront)
    {
        float x0 = ox, x1 = ox + sx;
        float y0 = oy, y1 = oy + sy;
        float z0 = oz, z1 = oz + sz;

        // UV coordinates for each face, relative to the part's UV block (uvX, uvY)
        // Minecraft skin texture layout:
        //      -X  +Z  +X  -Z
        // Top:  (uvX + D,      uvY)
        // Bot:  (uvX + D + W,  uvY)
        // Right: (uvX,         uvY + D)
        // Front: (uvX + D,     uvY + D)
        // Left: (uvX + D + W,  uvY + D)
        // Back: (uvX + D*2 + W,uvY + D)

        // Top face
        drawFace(
            new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
            new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
            uvX + uvD, uvY, uvW, uvD, 0.85f);

        // Bottom face
        drawFace(
            new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
            new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
            uvX + uvD + uvW, uvY, uvW, uvD, 0.5f);

        // Front face (z = z1)
        drawFace(
            new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
            new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
            uvX + uvD, uvY + uvD, uvW, uvH, 1.0f);

        // Back face (z = z0)
        drawFace(
            new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
            new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
            uvX + uvD + uvW + uvD, uvY + uvD, uvW, uvH, 0.55f);

        // Right face (x = x0)
        drawFace(
            new Vector3(x0, y1, z0), new Vector3(x0, y1, z1),
            new Vector3(x0, y0, z1), new Vector3(x0, y0, z0),
            uvX, uvY + uvD, uvD, uvH, 0.7f);

        // Left face (x = x1)
        drawFace(
            new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
            new Vector3(x1, y0, z0), new Vector3(x1, y0, z1),
            uvX + uvD + uvW, uvY + uvD, uvD, uvH, 0.7f);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c,
        out float u, out float v)
    {
        u = 0; v = 0;
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float inv = dot00 * dot11 - dot01 * dot01;
        if (MathF.Abs(inv) < 0.0001f) return false;
        inv = 1f / inv;

        float uu = (dot11 * dot02 - dot01 * dot12) * inv;
        float vv = (dot00 * dot12 - dot01 * dot02) * inv;

        if (uu >= 0 && vv >= 0 && uu + vv <= 1)
        {
            u = vv;
            v = uu;
            return true;
        }
        return false;
    }

    private static void SetPixel(byte[] buf, int stride, int w, int h,
        int x, int y, Rgba32 color, float shade)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        int offset = y * stride + x * 4;
        if (offset + 3 >= buf.Length) return;
        byte r = (byte)(color.R * shade);
        byte g = (byte)(color.G * shade);
        byte b = (byte)(color.B * shade);
        byte a = color.A;

        if (a == 255)
        {
            buf[offset + 0] = b;
            buf[offset + 1] = g;
            buf[offset + 2] = r;
            buf[offset + 3] = a;
        }
        else if (a > 0)
        {
            float af = a / 255f;
            buf[offset + 0] = (byte)(b * af + buf[offset + 0] * (1 - af));
            buf[offset + 1] = (byte)(g * af + buf[offset + 1] * (1 - af));
            buf[offset + 2] = (byte)(r * af + buf[offset + 2] * (1 - af));
            buf[offset + 3] = (byte)Math.Min(255, a + buf[offset + 3] * (1 - af));
        }
    }
}
