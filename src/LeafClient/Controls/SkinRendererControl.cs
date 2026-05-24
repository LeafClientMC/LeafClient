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
using LeafClient.Services;

namespace LeafClient.Controls;

public class SkinRendererControl : UserControl
{
    private WriteableBitmap? _renderTarget;
    private Avalonia.Controls.Image? _renderedImage;
    private Image<Rgba32>? _skinImage;
    private float _rotationY = 25f;
    private float _rotationX = -10f;
    private bool _isDragging;
    private DateTime _lastInteractionTime = DateTime.MinValue;
    private AvaloniaPoint _lastMouse;
    private float _autoRotation;
    private bool _hasSkin;
    private bool _isRendering;
    private Image<Rgba32>? _capeImage;
    private bool _isMojangCape;
    private Image<Rgba32>? _hatImage;
    private readonly DispatcherTimer _animTimer;

    private float _capeSwingX    = 0f;
    private float _capeSwingXVel = 0f;
    private float _idlePhase     = 0f;

    private float _wingFlapPhase = 0f;

    private float _zoom = 1.0f;

    public SkinRendererControl()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _animTimer.Tick += (_, _) =>
        {
            bool userActive = (DateTime.UtcNow - _lastInteractionTime).TotalMilliseconds < 500;
            if (!userActive)
            {
                if (_isDragging) _isDragging = false;
                _autoRotation += 0.8f;
                _rotationY = _autoRotation;
            }
            _wingFlapPhase += 0.10f;
            _auraPhase += 0.05f;
            _bbmodelTime += 0.033f;
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
            LeafClient.Services.LeafLog.Error("SkinRenderer", $"Failed to load skin: {ex.Message}");
        }
    }

    public void UpdateCapeTexture(byte[] pngData, bool isMojang = false)
    {
        try
        {
            _capeImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            _isMojangCape = isMojang;
            RenderAsync();
        }
        catch (Exception ex)
        {
            LeafClient.Services.LeafLog.Error("SkinRenderer", $"Failed to load cape: {ex.Message}");
        }
    }

    public void ClearCape()
    {
        _capeImage = null;
        _isMojangCape = false;
        _capeSwingX    = 0f;
        _capeSwingXVel = 0f;
        RenderAsync();
    }

    private void UpdateCapePhysics(float rotDeltaDeg)
    {
        _capeSwingXVel -= rotDeltaDeg * 0.20f;

        _idlePhase += 0.07f;
        _capeSwingXVel += MathF.Sin(_idlePhase) * 0.022f;

        _capeSwingXVel -= _capeSwingX * 0.08f;

        _capeSwingXVel *= 0.88f;

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
            LeafClient.Services.LeafLog.Error("SkinRenderer", $"Failed to load hat: {ex.Message}");
        }
    }

    public void ClearHat()
    {
        _hatImage = null;
        _isHorns = false;
        RenderAsync();
    }

    private Image<Rgba32>? _wingsImage;
    private bool _isHorns;

    public void UpdateWingsTexture(byte[] pngData)
    {
        try
        {
            _wingsImage = SixLabors.ImageSharp.Image.Load<Rgba32>(pngData);
            RenderAsync();
        }
        catch (Exception ex)
        {
            LeafClient.Services.LeafLog.Error("SkinRenderer", $"Failed to load wings: {ex.Message}");
        }
    }

    public void ClearWings()
    {
        _wingsImage = null;
        RenderAsync();
    }

    private bool _previewLit;

    public void SetPreviewLight(bool lit)
    {
        _previewLit = lit;
        RenderAsync();
    }

    private string? _auraType;
    private float _auraPhase = 0f;

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

    public sealed class BBSlot
    {
        public LeafClient.Services.BBModel.BBModel Model;
        public System.Collections.Generic.List<Image<Rgba32>?> Textures;
        public string Attachment;
        public float OffsetX, OffsetY, OffsetZ, RotationY, Scale;
        public BBSlot(LeafClient.Services.BBModel.BBModel m, System.Collections.Generic.List<Image<Rgba32>?> t, string a)
        {
            Model = m; Textures = t; Attachment = a;
            OffsetX = 0f; OffsetY = 0f; OffsetZ = 0f; RotationY = 0f; Scale = 1f;
        }
    }
    private readonly System.Collections.Generic.Dictionary<string, BBSlot> _bbmodelSlots = new();
    private float _bbmodelTime;

    public void SetBBModelSlot(string key, LeafClient.Services.BBModel.BBModel model, System.Collections.Generic.List<Image<Rgba32>?> textures, string attachment)
    {
        _bbmodelSlots[key] = new BBSlot(model, textures, attachment);
        RenderAsync();
    }

    public void SetBBModelSlot(string key, LeafClient.Services.BBModel.BBModel model, System.Collections.Generic.List<Image<Rgba32>?> textures, string attachment,
        float offsetX, float offsetY, float offsetZ, float rotationY, float scale)
    {
        var slot = new BBSlot(model, textures, attachment)
        {
            OffsetX = offsetX, OffsetY = offsetY, OffsetZ = offsetZ, RotationY = rotationY, Scale = scale <= 0f ? 1f : scale,
        };
        _bbmodelSlots[key] = slot;
        RenderAsync();
    }

    public void ClearBBModelSlot(string key)
    {
        if (_bbmodelSlots.Remove(key)) RenderAsync();
    }

    public void ClearAllBBModelSlots()
    {
        if (_bbmodelSlots.Count > 0) { _bbmodelSlots.Clear(); RenderAsync(); }
    }

    public void UpdateBBModel(LeafClient.Services.BBModel.BBModel model, System.Collections.Generic.List<Image<Rgba32>?> textures, string attachment)
        => SetBBModelSlot("default", model, textures, attachment);

    public void ClearBBModel() => ClearBBModelSlot("default");

    private void OnPointerPress(object? s, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _lastInteractionTime = DateTime.UtcNow;
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
        _lastInteractionTime = DateTime.UtcNow;
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
            bool isHorns     = _isHorns;
            string? auraType = _auraType;
            float auraPhase  = _auraPhase;
            bool previewLit  = _previewLit;
            float bbmodelTime = _bbmodelTime;
            bool isMojangCape = _isMojangCape;
            var bbmodelSlotsSnapshot = _bbmodelSlots.Count == 0
                ? null
                : new System.Collections.Generic.List<BBSlot>(_bbmodelSlots.Values);

            byte[]? buf = await Task.Run(() => RenderFrame(skinImage, capeImage, hatImage, wingsImage, w, h, rotY, rotX, zoom, capeSwingX, wingFlap, isHorns, auraType, auraPhase, previewLit, null, null, null, bbmodelTime, bbmodelSlotsSnapshot, isMojangCape));

            if (buf != null)
            {
                bool sizeChanged = _renderTarget == null ||
                                   _renderTarget.PixelSize.Width != w ||
                                   _renderTarget.PixelSize.Height != h;

                if (sizeChanged)
                {
                    _renderTarget = new WriteableBitmap(
                        new PixelSize(w, h),
                        new Avalonia.Vector(96, 96),
                        Avalonia.Platform.PixelFormat.Bgra8888,
                        Avalonia.Platform.AlphaFormat.Premul);
                }

                using (var fb = _renderTarget!.Lock())
                {
                    Marshal.Copy(buf, 0, fb.Address, buf.Length);
                }

                if (_renderedImage == null)
                {
                    _renderedImage = new Avalonia.Controls.Image
                    {
                        Stretch             = Stretch.None,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    };
                    Content = _renderedImage;
                }

                _renderedImage.Source = null;
                _renderedImage.Source = _renderTarget;
                _renderedImage.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            LeafClient.Services.LeafLog.Info("SkinRenderer", $"RenderAsync error: {ex.Message}");
        }
        finally
        {
            _isRendering = false;
        }
    }

    internal static byte[]? RenderFrame(Image<Rgba32> skinImage, Image<Rgba32>? capeImage, Image<Rgba32>? hatImage, Image<Rgba32>? wingsImage, int w, int h, float rotationY, float rotationX, float zoom = 1f, float capeSwingX = 0f, float wingFlapPhase = 0f, bool isHorns = false, string? auraType = null, float auraPhase = 0f, bool previewLit = false, LeafClient.Services.BBModel.BBModel? bbmodel = null, System.Collections.Generic.List<Image<Rgba32>?>? bbmodelTextures = null, string? bbmodelAttachment = null, float bbmodelTime = 0f, System.Collections.Generic.IReadOnlyList<BBSlot>? bbmodelSlots = null, bool isMojangCape = false)
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
        float cy = h * 0.48f;

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

            float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
            float zbr = ProjectZ(br), zbl = ProjectZ(bl);

            float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
            float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
            float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
            float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));

            int x0 = Math.Max(0, (int)MathF.Floor(minX));
            int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX));
            int y0 = Math.Max(0, (int)MathF.Floor(minY));
            int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));

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
                        texU = u + v;
                        texV = v;
                        pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                        hit = true;
                    }
                    else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                    {
                        texU = u;
                        texV = u + v;
                        pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                        hit = true;
                    }

                    if (hit)
                    {
                        int zIdx = py * w + px;
                        if (pixelZ >= zBuf[zIdx]) continue;

                        int su = uvX + (int)(texU * uvW);
                        int sv = uvY + (int)(texV * uvH);
                        if (su >= uvX + uvW) su = uvX + uvW - 1;
                        if (sv >= uvY + uvH) sv = uvY + uvH - 1;
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

        bool classic = skinImage.Height < 64;
        bool slim = false;
        if (!classic && skinImage.Width > 54 && skinImage.Height > 20)
        {
            slim = skinImage[54, 20].A == 0;
        }
        int armW = slim ? 3 : 4;
        float armWf = armW;

        DrawBodyPart(DrawFace, -4, -24, -2, 4, 12, 4, 0, 16, 4, 12, 4);
        if (classic)
            DrawBodyPart(DrawFace, 0, -24, -2, 4, 12, 4, 0, 16, 4, 12, 4);
        else
            DrawBodyPart(DrawFace, 0, -24, -2, 4, 12, 4, 16, 48, 4, 12, 4);
        DrawBodyPart(DrawFace, -4, -12, -2, 8, 12, 4, 16, 16, 8, 12, 4, 16 | 32);
        DrawBodyPart(DrawFace, -4 - armWf, -12, -2, armWf, 12, 4, 40, 16, armW, 12, 4);
        if (classic)
            DrawBodyPart(DrawFace, 4, -12, -2, armWf, 12, 4, 40, 16, armW, 12, 4);
        else
            DrawBodyPart(DrawFace, 4, -12, -2, armWf, 12, 4, 32, 48, armW, 12, 4);
        DrawBodyPart(DrawFace, -4, 0, -4, 8, 8, 8, 0, 0, 8, 8, 8);

        float inf = 0.5f;
        DrawBodyPart(DrawFace, -4-inf, -inf, -4-inf, 8+2*inf, 8+2*inf, 8+2*inf, 32, 0, 8, 8, 8);

        if (skinImage.Height >= 64)
        {
            DrawBodyPart(DrawFace, -4-inf, -24-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 0, 32, 4, 12, 4);
            DrawBodyPart(DrawFace, -inf, -24-inf, -2-inf, 4+2*inf, 12+2*inf, 4+2*inf, 0, 48, 4, 12, 4);
            DrawBodyPart(DrawFace, -4-inf, -12-inf, -2-inf, 8+2*inf, 12+2*inf, 4+2*inf, 16, 32, 8, 12, 4, 16 | 32);
            DrawBodyPart(DrawFace, -4 - armWf - inf, -12-inf, -2-inf, armWf + 2*inf, 12+2*inf, 4+2*inf, 40, 32, armW, 12, 4);
            DrawBodyPart(DrawFace, 4-inf, -12-inf, -2-inf, armWf + 2*inf, 12+2*inf, 4+2*inf, 48, 48, armW, 12, 4);
        }

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
            int x0 = Math.Max(0, (int)MathF.Floor(minX));
            int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX));
            int y0 = Math.Max(0, (int)MathF.Floor(minY));
            int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));
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
                        if (pixelZ >= zBuf[zIdx]) continue;
                        zBuf[zIdx] = pixelZ;
                        SetPixel(buf, stride, w, h, px2, py, solidColor, shade);
                    }
                }
            }
        }

        void RenderDarknessAuraBlob(float tt)
        {
            const int strandCount = 5;
            const int segsPerStrand = 16;
            const int tubeSides = 6;
            const float baseR = 17.0f;
            const float arcSpan = MathF.PI * 0.55f;

            float[] csCos = new float[tubeSides];
            float[] csSin = new float[tubeSides];
            for (int k = 0; k < tubeSides; k++)
            {
                float a = MathF.PI * 2f * k / tubeSides;
                csCos[k] = MathF.Cos(a);
                csSin[k] = MathF.Sin(a);
            }

            for (int s = 0; s < strandCount; s++)
            {
                float driftSpeed = 0.18f + (s % 3) * 0.05f;
                float anchor = s * (MathF.PI * 2f / strandCount) + tt * driftSpeed;
                float yCenter = MathF.Sin(tt * 0.45f + s * 1.13f) * 6.0f - 5.0f;
                float yArch   = 5.0f + MathF.Sin(tt * 0.62f + s * 0.71f) * 2.0f;

                float spikeMag = StrandSpike(tt, s);
                float spikeU   = 0.45f + 0.35f * MathF.Sin(tt * 0.85f + s * 1.73f);

                var centers = new Vector3[segsPerStrand];
                var radii = new float[segsPerStrand];
                var spikeBoosts = new float[segsPerStrand];

                for (int i = 0; i < segsPerStrand; i++)
                {
                    float u = (float)i / (segsPerStrand - 1);
                    float theta = anchor + (u - 0.5f) * arcSpan;

                    float radWobble =
                          MathF.Sin(u * 8.0f + tt * 1.40f + s * 1.5f) * 0.9f
                        + MathF.Sin(u * 4.0f + tt * 0.90f + s * 0.7f) * 0.6f;
                    float r = baseR + radWobble;

                    float spikeDist = u - spikeU;
                    float spikeFalloff = MathF.Exp(-spikeDist * spikeDist * 24.0f);
                    float spikeBoost = spikeMag * spikeFalloff;
                    spikeBoosts[i] = spikeBoost;
                    r += spikeBoost * 11.0f;

                    float cx = r * MathF.Cos(theta);
                    float cz = r * MathF.Sin(theta);

                    float yWobble =
                          MathF.Sin(u * 5.5f + tt * 1.50f + s) * 1.7f
                        + MathF.Sin(u * 11.0f + tt * 2.20f + s * 0.5f) * 0.8f;
                    float cy = yCenter + yArch * MathF.Sin(u * MathF.PI) + yWobble;

                    centers[i] = new Vector3(cx, cy, cz);

                    float taper = MathF.Sin(u * MathF.PI);
                    float widthBreath = 0.85f + 0.25f * MathF.Sin(u * 7.0f + tt * 2.50f + s);
                    radii[i] = (0.55f + taper * 0.85f) * widthBreath + spikeBoost * 0.8f;
                }

                var e1s = new Vector3[segsPerStrand];
                var e2s = new Vector3[segsPerStrand];
                for (int i = 0; i < segsPerStrand; i++)
                {
                    Vector3 tan;
                    if (i == 0)                       tan = centers[1] - centers[0];
                    else if (i == segsPerStrand - 1)  tan = centers[i] - centers[i - 1];
                    else                              tan = centers[i + 1] - centers[i - 1];

                    float tLenSq = tan.X * tan.X + tan.Y * tan.Y + tan.Z * tan.Z;
                    if (tLenSq < 0.00001f) tan = new Vector3(1, 0, 0);
                    else { float tl = MathF.Sqrt(tLenSq); tan = new Vector3(tan.X / tl, tan.Y / tl, tan.Z / tl); }

                    Vector3 e1;
                    if (MathF.Abs(tan.Y) < 0.9f)
                        e1 = Vector3.Cross(new Vector3(0, 1, 0), tan);
                    else
                        e1 = Vector3.Cross(new Vector3(1, 0, 0), tan);
                    float e1LenSq = e1.X * e1.X + e1.Y * e1.Y + e1.Z * e1.Z;
                    if (e1LenSq < 0.00001f) e1 = new Vector3(1, 0, 0);
                    else { float el = MathF.Sqrt(e1LenSq); e1 = new Vector3(e1.X / el, e1.Y / el, e1.Z / el); }

                    Vector3 e2 = Vector3.Cross(tan, e1);
                    float e2LenSq = e2.X * e2.X + e2.Y * e2.Y + e2.Z * e2.Z;
                    if (e2LenSq < 0.00001f) e2 = new Vector3(0, 1, 0);
                    else { float el = MathF.Sqrt(e2LenSq); e2 = new Vector3(e2.X / el, e2.Y / el, e2.Z / el); }

                    e1s[i] = e1;
                    e2s[i] = e2;
                }

                var csVerts = new Vector3[segsPerStrand, tubeSides];
                for (int i = 0; i < segsPerStrand; i++)
                {
                    float r = radii[i];
                    Vector3 c = centers[i];
                    Vector3 e1 = e1s[i];
                    Vector3 e2 = e2s[i];
                    for (int k = 0; k < tubeSides; k++)
                    {
                        float a = csCos[k] * r;
                        float b = csSin[k] * r;
                        csVerts[i, k] = new Vector3(
                            c.X + e1.X * a + e2.X * b,
                            c.Y + e1.Y * a + e2.Y * b,
                            c.Z + e1.Z * a + e2.Z * b);
                    }
                }

                for (int i = 0; i < segsPerStrand - 1; i++)
                {
                    float boost = (spikeBoosts[i] + spikeBoosts[i + 1]) * 0.5f;
                    float u = ((float)i + 0.5f) / (segsPerStrand - 1);
                    float bodyFactor = MathF.Sin(u * MathF.PI);

                    int rCol = Math.Clamp(0 + (int)MathF.Round(bodyFactor * 4f)  + (int)MathF.Round(boost * 28f), 0, 255);
                    int gCol = Math.Clamp(0 + (int)MathF.Round(bodyFactor * 1f)  + (int)MathF.Round(boost * 4f),  0, 255);
                    int bCol = Math.Clamp(6 + (int)MathF.Round(bodyFactor * 12f) + (int)MathF.Round(boost * 55f), 0, 255);
                    float shade = 0.85f + bodyFactor * 0.10f + boost * 0.15f;

                    for (int k = 0; k < tubeSides; k++)
                    {
                        int k2 = (k + 1) % tubeSides;
                        Vector3 q0 = csVerts[i, k];
                        Vector3 q1 = csVerts[i + 1, k];
                        Vector3 q2 = csVerts[i + 1, k2];
                        Vector3 q3 = csVerts[i, k2];
                        DrawSolidFace(q0, q1, q2, q3, (byte)rCol, (byte)gCol, (byte)bCol, shade);
                    }
                }
            }
        }

        static float StrandSpike(float tt, int strandIdx)
        {
            float cycle = 3.2f + (strandIdx % 3) * 0.55f;
            float offset = strandIdx * 1.27f;
            float local = ((tt + offset) % cycle) / cycle;
            const float window = 0.16f;
            if (local > window) return 0f;
            return MathF.Sin((local / window) * MathF.PI);
        }

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

        if (wingsImage != null)
        {
            int wingTexW = wingsImage.Width;
            int wingTexH = wingsImage.Height;
            int wingHalf = wingTexW / 2;

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
                int x0 = Math.Max(0, (int)MathF.Floor(minX) - 1);
                int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX) + 1);
                int y0 = Math.Max(0, (int)MathF.Floor(minY) - 1);
                int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY) + 1);
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

            byte boneR = 45, boneG = 8, boneB = 8;

            void RenderWing(float sideSign)
            {
                float s = sideSign;

                float t = wingFlapPhase;
                float wristWave = MathF.Sin(t) * 1.8f;
                float tipWave   = MathF.Sin(t - 0.6f) * 3.5f;
                float tipBendZ  = MathF.Cos(t - 0.4f) * 2.5f;

                var back  = new Vector3(s * 1.5f,  -1.0f, -2.5f);
                var wrist = new Vector3(s * 9.0f,  2.5f + wristWave, -4.5f);
                var f1 = new Vector3(s * 17.0f,   9.0f + tipWave,        -2.0f + tipBendZ);
                var f2 = new Vector3(s * 20.0f,   3.0f + tipWave,        -3.0f + tipBendZ);
                var f3 = new Vector3(s * 18.0f,  -4.0f + tipWave * 0.7f, -4.0f + tipBendZ * 0.6f);
                var f4 = new Vector3(s * 13.0f, -10.5f + tipWave * 0.4f, -4.5f + tipBendZ * 0.3f);
                var waist = new Vector3(s * 3.0f, -8.5f, -2.5f);

                DrawWingFace(wrist, f1, f2, wrist, 0, 0, wingHalf, wingTexH, 0.95f);
                DrawWingFace(wrist, f2, f3, wrist, 0, 0, wingHalf, wingTexH, 0.88f);
                DrawWingFace(wrist, f3, f4, wrist, 0, 0, wingHalf, wingTexH, 0.80f);
                DrawWingFace(back, wrist, f4, waist, 0, 0, wingHalf, wingTexH, 0.72f);

                DrawBoneBox(back, wrist, 1.8f, boneR, boneG, boneB);
                DrawBoneBox(wrist, f1, 1.2f, boneR, boneG, boneB);
                DrawBoneBox(wrist, f2, 1.2f, boneR, boneG, boneB);
                DrawBoneBox(wrist, f3, 1.2f, boneR, boneG, boneB);
                DrawBoneBox(wrist, f4, 1.2f, boneR, boneG, boneB);
            }

            RenderWing(+1f);
            RenderWing(-1f);
        }

        if (hatImage != null)
        {
            Rgba32 GetHatPixel(int sx, int sy)
            {
                if (sx < 0 || sx >= hatImage.Width || sy < 0 || sy >= hatImage.Height)
                    return new Rgba32(0, 0, 0, 0);
                return hatImage[sx, sy];
            }

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
                int x0 = Math.Max(0, (int)MathF.Floor(minX));
                int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX));
                int y0 = Math.Max(0, (int)MathF.Floor(minY));
                int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));
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
                            if (pixelZ >= zBuf[zIdx]) continue;
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
                float hatInf = 0.8f;
                DrawBodyPart(DrawHatFace,
                    -4 - hatInf, 5,           -4 - hatInf,
                    8 + 2 * hatInf, 3 + hatInf, 8 + 2 * hatInf,
                    0, 0, 8, 8, 8);
            }
            else
            {

                byte hBaseR = 35, hBaseG = 30, hBaseB = 35;
                byte hMidR  = 50, hMidG  = 40, hMidB  = 45;
                byte hTipR  = 90, hTipG  = 30, hTipB  = 25;

                if (hatImage.Width >= 8 && hatImage.Height >= 8)
                {
                    var cBase = hatImage[4, 28];
                    hBaseR = cBase.R; hBaseG = cBase.G; hBaseB = cBase.B;
                    var cMid = hatImage[4, 16];
                    hMidR = cMid.R; hMidG = cMid.G; hMidB = cMid.B;
                    var cTip = hatImage[4, 4];
                    hTipR = cTip.R; hTipG = cTip.G; hTipB = cTip.B;
                }

                void RenderHorn(float sideSign)
                {
                    float s = sideSign;
                    var p0 = new Vector3(s * 3.0f,  7.5f, -0.5f);
                    var p1 = new Vector3(s * 4.5f, 10.0f, -1.0f);
                    var p2 = new Vector3(s * 5.5f, 13.0f, -0.5f);
                    var p3 = new Vector3(s * 5.0f, 15.5f,  0.5f);
                    var p4 = new Vector3(s * 4.0f, 17.5f,  1.0f);

                    DrawBoneBox(p0, p1, 2.2f, hBaseR, hBaseG, hBaseB);
                    DrawBoneBox(p1, p2, 1.8f, hBaseR, hBaseG, hBaseB);
                    DrawBoneBox(p2, p3, 1.3f, hMidR,  hMidG,  hMidB);
                    DrawBoneBox(p3, p4, 0.8f, hTipR,  hTipG,  hTipB);
                }

                RenderHorn(+1f);
                RenderHorn(-1f);
            }
        }

        if (capeImage != null)
        {
            Rgba32 GetCapePixel(int sx, int sy)
            {
                if (sx < 0 || sx >= capeImage.Width || sy < 0 || sy >= capeImage.Height)
                    return new Rgba32(0, 0, 0, 0);
                return capeImage[sx, sy];
            }

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
                int x0 = Math.Max(0, (int)MathF.Floor(minX));
                int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX));
                int y0 = Math.Max(0, (int)MathF.Floor(minY));
                int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));
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
                            if (pixelZ >= zBuf[zIdx]) continue;
                            int su = uvX + (int)(texU * (uvW - 0.01f));
                            int sv = uvY + (int)(texV * (uvH - 0.01f));
                            var color = GetCapePixel(su, sv);
                            if (color.A > 0) { zBuf[zIdx] = pixelZ; SetPixel(buf, stride, w, h, px, py, color, shade); }
                        }
                    }
                }
            }

            if (isMojangCape)
            {
                int texW = capeImage.Width;
                int texH = capeImage.Height;
                int frontStart = (texW * 1) / 64;
                int backStart  = (texW * 11) / 64;
                int segW       = (texW * 10) / 64;
                int segY1      = (texH * 1) / 32;
                int segH1      = (texH * 4) / 32;
                int segY2      = (texH * 5) / 32;
                int segH2      = (texH * 6) / 32;
                int segY3      = (texH * 11) / 32;
                int segH3      = (texH * 6) / 32;
                int sideStripW = Math.Max(1, texW / 64);
                int rightStripX = backStart - sideStripW;
                int leftStripX  = backStart;
                int topStripY    = 0;
                int topStripH    = Math.Max(1, texH / 32);
                int bottomStripY = 0;
                int bottomStripH = Math.Max(1, texH / 32);
                int topStripX    = frontStart;
                int bottomStripX = backStart;

                float s1 = capeSwingX * 0.10f;
                float s2 = capeSwingX * 0.52f;
                float s3 = capeSwingX;
                float liftY = MathF.Abs(capeSwingX) * 0.38f;
                float b1 = MathF.Abs(capeSwingX) * 0.10f;
                float b2 = MathF.Abs(capeSwingX) * 0.22f;

                DrawCapeFace(
                    new Vector3(-5f,     0f,  -2.5f),
                    new Vector3( 5f,     0f,  -2.5f),
                    new Vector3( 5f+s1, -4f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -2.5f),
                    backStart, segY1, segW, segH1, 0.45f);

                DrawCapeFace(
                    new Vector3(-5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s2, -10f,  -2.5f - b1),
                    new Vector3(-5f+s2, -10f,  -2.5f - b1),
                    backStart, segY2, segW, segH2, 0.45f);

                DrawCapeFace(
                    new Vector3(-5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s3, -16f+liftY,  -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY,  -2.5f - b2),
                    backStart, segY3, segW, segH3, 0.45f);

                DrawCapeFace(
                    new Vector3( 5f,     0f,  -3.5f),
                    new Vector3(-5f,     0f,  -3.5f),
                    new Vector3(-5f+s1, -4f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -3.5f),
                    frontStart, segY1, segW, segH1, 0.95f);

                DrawCapeFace(
                    new Vector3( 5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s2, -10f,  -3.5f - b1),
                    new Vector3( 5f+s2, -10f,  -3.5f - b1),
                    frontStart, segY2, segW, segH2, 0.95f);

                DrawCapeFace(
                    new Vector3( 5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s3, -16f+liftY,  -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY,  -3.5f - b2),
                    frontStart, segY3, segW, segH3, 0.95f);

                DrawCapeFace(
                    new Vector3( 5f,     0f,  -2.5f),
                    new Vector3( 5f,     0f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -2.5f),
                    rightStripX, segY1, sideStripW, segH1, 0.70f);

                DrawCapeFace(
                    new Vector3( 5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s1,  -4f,  -3.5f),
                    new Vector3( 5f+s2, -10f,  -3.5f - b1),
                    new Vector3( 5f+s2, -10f,  -2.5f - b1),
                    rightStripX, segY2, sideStripW, segH2, 0.70f);

                DrawCapeFace(
                    new Vector3( 5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s2,       -10f,  -3.5f - b1),
                    new Vector3( 5f+s3, -16f+liftY,  -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY,  -2.5f - b2),
                    rightStripX, segY3, sideStripW, segH3, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f,     0f,  -3.5f),
                    new Vector3(-5f,     0f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -3.5f),
                    leftStripX, segY1, sideStripW, segH1, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s1,  -4f,  -2.5f),
                    new Vector3(-5f+s2, -10f,  -2.5f - b1),
                    new Vector3(-5f+s2, -10f,  -3.5f - b1),
                    leftStripX, segY2, sideStripW, segH2, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s2,       -10f,  -2.5f - b1),
                    new Vector3(-5f+s3, -16f+liftY,  -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY,  -3.5f - b2),
                    leftStripX, segY3, sideStripW, segH3, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f, 0f, -2.5f),
                    new Vector3( 5f, 0f, -2.5f),
                    new Vector3( 5f, 0f, -3.5f),
                    new Vector3(-5f, 0f, -3.5f),
                    topStripX, topStripY, segW, topStripH, 0.80f);

                DrawCapeFace(
                    new Vector3(-5f+s3, -16f+liftY, -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY, -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY, -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY, -2.5f - b2),
                    bottomStripX, bottomStripY, segW, bottomStripH, 0.55f);
            }
            else
            {
                int halfW = capeImage.Width  / 2;
                int texH  = capeImage.Height;

                int uv1 = texH * 4 / 16;
                int uv2 = texH * 6 / 16;
                int uv3 = texH - uv1 - uv2;
                int uv12 = uv1 + uv2;

                float s1 = capeSwingX * 0.10f;
                float s2 = capeSwingX * 0.52f;
                float s3 = capeSwingX;

                float liftY = MathF.Abs(capeSwingX) * 0.38f;

                float b1 = MathF.Abs(capeSwingX) * 0.10f;
                float b2 = MathF.Abs(capeSwingX) * 0.22f;

                DrawCapeFace(
                    new Vector3(-5f,     0f,  -2.5f),
                    new Vector3( 5f,     0f,  -2.5f),
                    new Vector3( 5f+s1, -4f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -2.5f),
                    0, 0, halfW, uv1, 0.85f);

                DrawCapeFace(
                    new Vector3(-5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s2, -10f,  -2.5f - b1),
                    new Vector3(-5f+s2, -10f,  -2.5f - b1),
                    0, uv1, halfW, uv2, 0.85f);

                DrawCapeFace(
                    new Vector3(-5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s3, -16f+liftY,  -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY,  -2.5f - b2),
                    0, uv12, halfW, uv3, 0.85f);

                DrawCapeFace(
                    new Vector3( 5f,     0f,  -3.5f),
                    new Vector3(-5f,     0f,  -3.5f),
                    new Vector3(-5f+s1, -4f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -3.5f),
                    halfW, 0, halfW, uv1, 0.30f);

                DrawCapeFace(
                    new Vector3( 5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s2, -10f,  -3.5f - b1),
                    new Vector3( 5f+s2, -10f,  -3.5f - b1),
                    halfW, uv1, halfW, uv2, 0.30f);

                DrawCapeFace(
                    new Vector3( 5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s3, -16f+liftY,  -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY,  -3.5f - b2),
                    halfW, uv12, halfW, uv3, 0.30f);

                int sideStripW = Math.Max(1, halfW / 12);
                int rightStripX = halfW - sideStripW;
                int leftStripX  = 0;
                int topStripH    = Math.Max(1, texH / 16);
                int bottomStripH = Math.Max(1, texH / 16);
                int topBottomStripW = halfW;

                DrawCapeFace(
                    new Vector3( 5f,     0f,  -2.5f),
                    new Vector3( 5f,     0f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -3.5f),
                    new Vector3( 5f+s1, -4f,  -2.5f),
                    rightStripX, 0, sideStripW, uv1, 0.70f);
                DrawCapeFace(
                    new Vector3( 5f+s1,  -4f,  -2.5f),
                    new Vector3( 5f+s1,  -4f,  -3.5f),
                    new Vector3( 5f+s2, -10f,  -3.5f - b1),
                    new Vector3( 5f+s2, -10f,  -2.5f - b1),
                    rightStripX, uv1, sideStripW, uv2, 0.70f);
                DrawCapeFace(
                    new Vector3( 5f+s2,       -10f,  -2.5f - b1),
                    new Vector3( 5f+s2,       -10f,  -3.5f - b1),
                    new Vector3( 5f+s3, -16f+liftY,  -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY,  -2.5f - b2),
                    rightStripX, uv12, sideStripW, uv3, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f,     0f,  -3.5f),
                    new Vector3(-5f,     0f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -2.5f),
                    new Vector3(-5f+s1, -4f,  -3.5f),
                    leftStripX, 0, sideStripW, uv1, 0.70f);
                DrawCapeFace(
                    new Vector3(-5f+s1,  -4f,  -3.5f),
                    new Vector3(-5f+s1,  -4f,  -2.5f),
                    new Vector3(-5f+s2, -10f,  -2.5f - b1),
                    new Vector3(-5f+s2, -10f,  -3.5f - b1),
                    leftStripX, uv1, sideStripW, uv2, 0.70f);
                DrawCapeFace(
                    new Vector3(-5f+s2,       -10f,  -3.5f - b1),
                    new Vector3(-5f+s2,       -10f,  -2.5f - b1),
                    new Vector3(-5f+s3, -16f+liftY,  -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY,  -3.5f - b2),
                    leftStripX, uv12, sideStripW, uv3, 0.70f);

                DrawCapeFace(
                    new Vector3(-5f, 0f, -2.5f),
                    new Vector3( 5f, 0f, -2.5f),
                    new Vector3( 5f, 0f, -3.5f),
                    new Vector3(-5f, 0f, -3.5f),
                    0, 0, topBottomStripW, topStripH, 0.80f);

                DrawCapeFace(
                    new Vector3(-5f+s3, -16f+liftY, -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY, -3.5f - b2),
                    new Vector3( 5f+s3, -16f+liftY, -2.5f - b2),
                    new Vector3(-5f+s3, -16f+liftY, -2.5f - b2),
                    0, texH - bottomStripH, topBottomStripW, bottomStripH, 0.55f);
            }
        }

        if (auraType != null)
        {
            int particleCount = 12;
            float t = auraPhase;

            if (auraType == "darkness")
            {
                RenderDarknessAuraBlob(t);
            }
            else if (auraType == "hearts")
            {
                int[,] brokenHeart = {
                    {0,1,1,0,0,2,2,0},
                    {1,1,1,1,2,2,2,2},
                    {1,1,1,1,1,2,2,2},
                    {0,1,1,1,2,2,2,0},
                    {0,0,1,1,1,2,2,0},
                    {0,0,0,1,2,2,0,0},
                    {0,0,0,1,2,0,0,0},
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
                            float ly = (5f - row) * pxSize;

                            var q0 = new Vector3(fx + lx,          fy + ly + pxSize, fz);
                            var q1 = new Vector3(fx + lx + pxSize, fy + ly + pxSize, fz);
                            var q2 = new Vector3(fx + lx + pxSize, fy + ly,          fz);
                            var q3 = new Vector3(fx + lx,          fy + ly,          fz);

                            byte cr, cg, cb;
                            float sh;
                            if (val == 2) { cr = 255; cg = 220; cb = 50; sh = 0.95f; }
                            else if (val == 3) { cr = 200; cg = 50; cb = 10; sh = 0.75f; }
                            else { cr = 255; cg = 130; cb = 20; sh = 0.88f; }

                            DrawSolidFace(q0, q1, q2, q3, cr, cg, cb, sh);
                        }
                    }
                }

                for (int e = 0; e < 12; e++)
                {
                    float life = (t * 1.6f + e * 0.52f) % 2.5f;
                    float frac = life / 2.5f;

                    float seed = e * 137.5f;
                    float startX = MathF.Sin(seed) * 3.5f;
                    float startZ = MathF.Cos(seed) * 2f;
                    float startY = -10f + (e % 5) * 4.5f;

                    float ex = startX + MathF.Sin(t * 0.8f + e * 1.4f) * frac * 3f;
                    float ez = startZ + MathF.Cos(t * 0.6f + e * 1.7f) * frac * 2f;
                    float ey = startY + frac * 12f;

                    float emberSize = (1f - frac) * 0.6f + 0.15f;
                    float dim = 1f - frac * 0.7f;

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

        if (previewLit)
        {
            for (int py = 0; py < h; py++)
            {
                float vertFrac = 1f - (float)py / h;
                float boost = 1.35f + vertFrac * 0.65f;

                for (int px = 0; px < w; px++)
                {
                    int off = py * stride + px * 4;
                    byte a = buf[off + 3];
                    if (a == 0) continue;

                    buf[off + 0] = (byte)Math.Min(255, (int)(buf[off + 0] * boost));
                    buf[off + 1] = (byte)Math.Min(255, (int)(buf[off + 1] * boost));
                    buf[off + 2] = (byte)Math.Min(255, (int)(buf[off + 2] * boost));
                }
            }
        }

        if (bbmodel != null && bbmodelTextures != null && bbmodelTextures.Count > 0)
        {
            DrawBBModel(bbmodel, bbmodelTextures, bbmodelAttachment ?? "head_top", bbmodelTime, false, 0f, 0f, 0f, 0f, 1f);
        }
        if (bbmodelSlots != null)
        {
            for (int i = 0; i < bbmodelSlots.Count; i++)
            {
                var slot = bbmodelSlots[i];
                if (slot.Model != null && slot.Textures != null && slot.Textures.Count > 0)
                {
                    string att = slot.Attachment ?? "head_top";
                    DrawBBModel(slot.Model, slot.Textures, att, bbmodelTime, false, slot.OffsetX, slot.OffsetY, slot.OffsetZ, slot.RotationY, slot.Scale);
                    if (att == "body_back")
                    {
                        DrawBBModel(slot.Model, slot.Textures, att, bbmodelTime, true, slot.OffsetX, slot.OffsetY, slot.OffsetZ, slot.RotationY, slot.Scale);
                    }
                }
            }
        }

        Vector3 BBAttachmentOffset(string att)
        {
            switch (att)
            {
                case "head_top":    return new Vector3(0, 10, 0);
                case "head_center": return new Vector3(0, 4, 0);
                case "head_front":  return new Vector3(0, 4, -4);
                case "body_back":   return new Vector3(-9.6f, -10.4f, 4.8f);
                case "feet_below":  return new Vector3(0, -24, 0);
                case "right_hand":  return new Vector3(-6, -10, 0);
                case "left_hand":   return new Vector3(6, -10, 0);
                default:            return new Vector3(0, 10, 0);
            }
        }

        float[] MatRotXYZ(float rxDeg, float ryDeg, float rzDeg)
        {
            float rx = rxDeg * MathF.PI / 180f, ry = ryDeg * MathF.PI / 180f, rz = rzDeg * MathF.PI / 180f;
            float cx_ = MathF.Cos(rx), sx_ = MathF.Sin(rx);
            float cy_ = MathF.Cos(ry), sy_ = MathF.Sin(ry);
            float cz_ = MathF.Cos(rz), sz_ = MathF.Sin(rz);
            float[] Rx = { 1, 0, 0, 0, cx_, -sx_, 0, sx_, cx_ };
            float[] Ry = { cy_, 0, sy_, 0, 1, 0, -sy_, 0, cy_ };
            float[] Rz = { cz_, -sz_, 0, sz_, cz_, 0, 0, 0, 1 };
            return MatMul(MatMul(Rz, Ry), Rx);
        }

        float[] MatMul(float[] a, float[] b) => new[]
        {
            a[0]*b[0]+a[1]*b[3]+a[2]*b[6], a[0]*b[1]+a[1]*b[4]+a[2]*b[7], a[0]*b[2]+a[1]*b[5]+a[2]*b[8],
            a[3]*b[0]+a[4]*b[3]+a[5]*b[6], a[3]*b[1]+a[4]*b[4]+a[5]*b[7], a[3]*b[2]+a[4]*b[5]+a[5]*b[8],
            a[6]*b[0]+a[7]*b[3]+a[8]*b[6], a[6]*b[1]+a[7]*b[4]+a[8]*b[7], a[6]*b[2]+a[7]*b[5]+a[8]*b[8]
        };

        Vector3 ApplyMat(float[] m, Vector3 p) => new Vector3(
            m[0]*p.X + m[1]*p.Y + m[2]*p.Z,
            m[3]*p.X + m[4]*p.Y + m[5]*p.Z,
            m[6]*p.X + m[7]*p.Y + m[8]*p.Z);

        void DrawBBModelFace(Image<Rgba32> tex, Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl,
                             float u1, float v1, float u2, float v2)
        {
            var stl = Project(tl); var str = Project(tr);
            var sbr = Project(br); var sbl = Project(bl);
            float ztl = ProjectZ(tl), ztr = ProjectZ(tr);
            float zbr = ProjectZ(br), zbl = ProjectZ(bl);
            float minX = MathF.Min(MathF.Min(stl.X, str.X), MathF.Min(sbr.X, sbl.X));
            float maxX = MathF.Max(MathF.Max(stl.X, str.X), MathF.Max(sbr.X, sbl.X));
            float minY = MathF.Min(MathF.Min(stl.Y, str.Y), MathF.Min(sbr.Y, sbl.Y));
            float maxY = MathF.Max(MathF.Max(stl.Y, str.Y), MathF.Max(sbr.Y, sbl.Y));
            int x0 = Math.Max(0, (int)MathF.Floor(minX));
            int x1 = Math.Min(w - 1, (int)MathF.Ceiling(maxX));
            int y0 = Math.Max(0, (int)MathF.Floor(minY));
            int y1 = Math.Min(h - 1, (int)MathF.Ceiling(maxY));
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
                        texU = u + v;
                        texV = v;
                        pixelZ = (1 - u - v) * ztl + u * ztr + v * zbr;
                        hit = true;
                    }
                    else if (PointInTriangle(pt, stl, sbr, sbl, out u, out v))
                    {
                        texU = u;
                        texV = u + v;
                        pixelZ = (1 - u - v) * ztl + u * zbr + v * zbl;
                        hit = true;
                    }
                    if (!hit) continue;
                    int zIdx = py * w + px;
                    if (pixelZ >= zBuf[zIdx]) continue;
                    if (texU < 0) texU = 0; else if (texU > 1) texU = 1;
                    if (texV < 0) texV = 0; else if (texV > 1) texV = 1;
                    int su = (int)(u1 + texU * (u2 - u1));
                    int sv = (int)(v1 + texV * (v2 - v1));
                    if (su < 0) su = 0; else if (su >= tex.Width) su = tex.Width - 1;
                    if (sv < 0) sv = 0; else if (sv >= tex.Height) sv = tex.Height - 1;
                    var color = tex[su, sv];
                    if (color.A == 0) continue;
                    zBuf[zIdx] = pixelZ;
                    SetPixel(buf, stride, w, h, px, py, color, 1.0f);
                }
            }
        }

        void DrawBBModel(LeafClient.Services.BBModel.BBModel model, System.Collections.Generic.List<Image<Rgba32>?> textures, string attachment, float time, bool mirrorX,
            float perOffX, float perOffY, float perOffZ, float perRotY, float perScale)
        {
            var rootOff = BBAttachmentOffset(attachment) + new Vector3(perOffX, perOffY, perOffZ);
            float mx = mirrorX ? -1f : 1f;
            bool isBodyBack = attachment == "body_back";
            float bbScale = (isBodyBack ? 1.3f : 1.0f) * (perScale <= 0f ? 1f : perScale);
            var perRotMat = perRotY != 0f ? MatRotXYZ(0f, perRotY, 0f) : null;
            float sweepDeg = isBodyBack ? 15f : 0f;
            var sweepPivot = new Vector3(7.4f, 5f, -3.06f);
            var sweepMat = sweepDeg != 0f ? MatRotXYZ(0f, sweepDeg, 0f) : null;
            var anim = (model.Animations.Count > 0) ? model.Animations[0] : null;
            float animTime = (anim != null && anim.Length > 0f) ? (time % anim.Length) : time;

            (float[] rot, float[] pos) EvalBoneTransform(string uuid, float[] baseRot)
            {
                if (anim == null || !anim.Animators.TryGetValue(uuid, out var animator))
                    return (baseRot, new float[3]);
                var rot = LeafClient.Services.BBModel.Molang.EvalKeyframeChannel(animator, "rotation", animTime, new float[3]);
                var pos = LeafClient.Services.BBModel.Molang.EvalKeyframeChannel(animator, "position", animTime, new float[3]);
                return (new[] { baseRot[0] + rot[0], baseRot[1] + rot[1], baseRot[2] + rot[2] }, pos);
            }

            void DrawElement(LeafClient.Services.BBModel.BBModel.Element e, float[] parentM, Vector3 parentB)
            {
                float fromX = e.From[0] - e.Inflate, fromY = e.From[1] - e.Inflate, fromZ = e.From[2] - e.Inflate;
                float toX = e.To[0] + e.Inflate, toY = e.To[1] + e.Inflate, toZ = e.To[2] + e.Inflate;
                const float MinThick = 0.02f;
                if (toX - fromX < MinThick) { float c = (fromX + toX) * 0.5f; fromX = c - MinThick * 0.5f; toX = c + MinThick * 0.5f; }
                if (toY - fromY < MinThick) { float c = (fromY + toY) * 0.5f; fromY = c - MinThick * 0.5f; toY = c + MinThick * 0.5f; }
                if (toZ - fromZ < MinThick) { float c = (fromZ + toZ) * 0.5f; fromZ = c - MinThick * 0.5f; toZ = c + MinThick * 0.5f; }

                var rE = MatRotXYZ(e.Rotation[0], e.Rotation[1], e.Rotation[2]);
                var pE = new Vector3(e.Origin[0], e.Origin[1], e.Origin[2]);
                var elemM = MatMul(parentM, rE);
                var elemB = ApplyMat(parentM, pE - ApplyMat(rE, pE)) + parentB;

                Vector3 Tv(Vector3 p)
                {
                    var transformed = ApplyMat(elemM, p) + elemB;
                    if (sweepMat != null)
                    {
                        var rel = transformed - sweepPivot;
                        transformed = ApplyMat(sweepMat, rel) + sweepPivot;
                    }
                    var scaled = transformed * bbScale;
                    if (perRotMat != null) scaled = ApplyMat(perRotMat, scaled);
                    var world = scaled + rootOff;
                    if (isBodyBack)
                    {
                        world = new Vector3(-world.X, world.Y, -world.Z);
                    }
                    if (mirrorX) world.X = -world.X;
                    return world;
                }

                var v000 = Tv(new Vector3(fromX, fromY, fromZ));
                var v100 = Tv(new Vector3(toX,   fromY, fromZ));
                var v010 = Tv(new Vector3(fromX, toY,   fromZ));
                var v110 = Tv(new Vector3(toX,   toY,   fromZ));
                var v001 = Tv(new Vector3(fromX, fromY, toZ));
                var v101 = Tv(new Vector3(toX,   fromY, toZ));
                var v011 = Tv(new Vector3(fromX, toY,   toZ));
                var v111 = Tv(new Vector3(toX,   toY,   toZ));

                string[] faceList = { "north", "south", "east", "west", "up", "down" };
                foreach (var key in faceList)
                {
                    if (!e.Faces.TryGetValue(key, out var f) || f.Uv == null) continue;
                    int texIdx = f.TextureIndex;
                    if (texIdx < 0 || texIdx >= textures.Count) texIdx = 0;
                    var tex = textures[texIdx];
                    if (tex == null) continue;
                    float resW = model.Res != null && model.Res.Width > 0 ? model.Res.Width : 16f;
                    float resH = model.Res != null && model.Res.Height > 0 ? model.Res.Height : 16f;
                    float texScaleX = tex.Width / resW;
                    float texScaleY = tex.Height / resH;
                    float u1 = f.Uv[0] * texScaleX, v1 = f.Uv[1] * texScaleY;
                    float u2 = f.Uv[2] * texScaleX, v2 = f.Uv[3] * texScaleY;
                    if (u1 == u2 || v1 == v2) continue;
                    Vector3 tl, tr, br, bl;
                    switch (key)
                    {
                        case "north":  tl = v110; tr = v010; br = v000; bl = v100; break;
                        case "south":  tl = v011; tr = v111; br = v101; bl = v001; break;
                        case "east":   tl = v111; tr = v110; br = v100; bl = v101; break;
                        case "west":   tl = v010; tr = v011; br = v001; bl = v000; break;
                        case "up":     tl = v010; tr = v110; br = v111; bl = v011; break;
                        default:       tl = v001; tr = v101; br = v100; bl = v000; break;
                    }
                    DrawBBModelFace(tex, tl, tr, br, bl, u1, v1, u2, v2);
                }
            }

            void DrawNode(LeafClient.Services.BBModel.BBModel.OutlinerNode node, float[] parentM, Vector3 parentB)
            {
                var (boneRot, bonePos) = EvalBoneTransform(node.Uuid, node.Rotation);
                var rN = MatRotXYZ(boneRot[0], boneRot[1], boneRot[2]);
                var pN = new Vector3(node.Origin[0], node.Origin[1], node.Origin[2]);
                var tN = new Vector3(bonePos[0], bonePos[1], bonePos[2]);
                var nodeM = MatMul(parentM, rN);
                var nodeB = ApplyMat(parentM, pN - ApplyMat(rN, pN) + tN) + parentB;
                foreach (var euid in node.ElementUuids)
                {
                    if (model.ElementsByUuid.TryGetValue(euid, out var e))
                        DrawElement(e, nodeM, nodeB);
                }
                foreach (var child in node.Children)
                    DrawNode(child, nodeM, nodeB);
            }

            float[] identity = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            if (model.Outliner.Count == 0)
            {
                foreach (var e in model.Elements) DrawElement(e, identity, Vector3.Zero);
            }
            else
            {
                foreach (var n in model.Outliner) DrawNode(n, identity, Vector3.Zero);
            }
        }

        return buf;
    }

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

        drawFace(
            new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
            new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
            uvX + uvD, uvY, uvW, uvD, 0.85f);

        drawFace(
            new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
            new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
            uvX + uvD + uvW, uvY, uvW, uvD, 0.5f);

        drawFace(
            new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
            new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
            uvX + uvD, uvY + uvD, uvW, uvH, 1.0f);

        drawFace(
            new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
            new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
            uvX + uvD + uvW + uvD, uvY + uvD, uvW, uvH, 0.55f);

        drawFace(
            new Vector3(x0, y1, z0), new Vector3(x0, y1, z1),
            new Vector3(x0, y0, z1), new Vector3(x0, y0, z0),
            uvX, uvY + uvD, uvD, uvH, 0.7f);

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

        if (uu >= 0f && vv >= 0f && uu + vv <= 1f)
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
