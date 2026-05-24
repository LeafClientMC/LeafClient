using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LeafClient.Services.BBModel
{
    public sealed class BBModel
    {
        public Meta MetaInfo { get; set; } = new();
        public Resolution Res { get; set; } = new();
        public List<Element> Elements { get; } = new();
        public Dictionary<string, Element> ElementsByUuid { get; } = new();
        public List<OutlinerNode> Outliner { get; } = new();
        public List<Texture> Textures { get; } = new();
        public List<Animation> Animations { get; } = new();

        public sealed class Meta { public bool BoxUv; }
        public sealed class Resolution { public int Width = 16; public int Height = 16; }

        public sealed class Element
        {
            public string Uuid = "";
            public string Name = "";
            public string Type = "cube";
            public float[] From = new float[3];
            public float[] To = new float[3];
            public float[] Origin = new float[3];
            public float[] Rotation = new float[3];
            public float Inflate;
            public int LightEmission;
            public bool BoxUv;
            public int[] UvOffset = new int[2];
            public Dictionary<string, Face> Faces = new();
        }

        public sealed class Face
        {
            public float[]? Uv;
            public int TextureIndex;
            public int Rotation;
        }

        public sealed class OutlinerNode
        {
            public string Uuid = "";
            public string Name = "";
            public bool IsGroup;
            public float[] Origin = new float[3];
            public float[] Rotation = new float[3];
            public List<string> ElementUuids = new();
            public List<OutlinerNode> Children = new();
        }

        public sealed class Texture
        {
            public string Name = "";
            public string Source = "";
            public int FrameTime;
            public byte[]? DecodedPng;
            public int Width;
            public int Height;
        }

        public sealed class Animation
        {
            public string Name = "";
            public string Loop = "loop";
            public float Length;
            public Dictionary<string, Animator> Animators = new();
        }

        public sealed class Animator
        {
            public string Name = "";
            public List<Keyframe> Keyframes = new();
        }

        public sealed class Keyframe
        {
            public string Channel = "rotation";
            public float Time;
            public string Interpolation = "linear";
            public string[] DataPoint = new[] { "0", "0", "0" };
        }
    }

    public static class BBModelParser
    {
        public const int MaxElements = 1000;
        public const int MaxOutlinerDepth = 32;
        public const int MaxTextures = 16;
        public const int MaxTextureBytes = 1024 * 1024;
        public const int MaxTextureBase64Chars = (MaxTextureBytes / 3 + 1) * 4 + 64;
        public const int MaxAnimations = 32;
        public const int MaxKeyframesPerAnim = 256;

        public static BBModel Parse(byte[] jsonBytes)
        {
            using var doc = JsonDocument.Parse(jsonBytes);
            return Parse(doc.RootElement);
        }

        public static BBModel Parse(JsonElement root)
        {
            var m = new BBModel();
            if (root.TryGetProperty("meta", out var meta))
            {
                if (meta.TryGetProperty("box_uv", out var bu) && bu.ValueKind == JsonValueKind.True) m.MetaInfo.BoxUv = true;
                else if (meta.TryGetProperty("box_uv", out var bu2) && bu2.ValueKind == JsonValueKind.False) m.MetaInfo.BoxUv = false;
            }
            if (root.TryGetProperty("resolution", out var res))
            {
                if (res.TryGetProperty("width", out var rw)) m.Res.Width = rw.GetInt32();
                if (res.TryGetProperty("height", out var rh)) m.Res.Height = rh.GetInt32();
            }
            if (root.TryGetProperty("elements", out var elementsArr) && elementsArr.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var el in elementsArr.EnumerateArray())
                {
                    if (++count > MaxElements) break;
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    var e = new BBModel.Element();
                    if (el.TryGetProperty("uuid", out var u) && u.ValueKind == JsonValueKind.String) e.Uuid = u.GetString() ?? "";
                    if (el.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String) e.Name = nm.GetString() ?? "";
                    if (el.TryGetProperty("type", out var ty) && ty.ValueKind == JsonValueKind.String) e.Type = ty.GetString() ?? "cube";
                    if (el.TryGetProperty("from", out var fr)) e.From = ReadVec3(fr);
                    if (el.TryGetProperty("to", out var to)) e.To = ReadVec3(to);
                    if (el.TryGetProperty("origin", out var or)) e.Origin = ReadVec3(or);
                    if (el.TryGetProperty("rotation", out var rot)) e.Rotation = ReadVec3(rot);
                    if (el.TryGetProperty("inflate", out var inf) && inf.ValueKind == JsonValueKind.Number) e.Inflate = inf.GetSingle();
                    if (el.TryGetProperty("light_emission", out var le) && le.ValueKind == JsonValueKind.Number) e.LightEmission = le.GetInt32();
                    if (el.TryGetProperty("box_uv", out var ebu) && (ebu.ValueKind == JsonValueKind.True || ebu.ValueKind == JsonValueKind.False))
                        e.BoxUv = ebu.GetBoolean();
                    else
                        e.BoxUv = m.MetaInfo.BoxUv;
                    if (el.TryGetProperty("uv_offset", out var uo) && uo.ValueKind == JsonValueKind.Array && uo.GetArrayLength() >= 2)
                        e.UvOffset = new[] { uo[0].GetInt32(), uo[1].GetInt32() };
                    if (el.TryGetProperty("faces", out var fac) && fac.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var fp in fac.EnumerateObject())
                        {
                            if (fp.Value.ValueKind != JsonValueKind.Object) continue;
                            var face = new BBModel.Face();
                            if (fp.Value.TryGetProperty("uv", out var uv) && uv.ValueKind == JsonValueKind.Array && uv.GetArrayLength() >= 4)
                                face.Uv = new[] { uv[0].GetSingle(), uv[1].GetSingle(), uv[2].GetSingle(), uv[3].GetSingle() };
                            if (fp.Value.TryGetProperty("texture", out var tx) && tx.ValueKind == JsonValueKind.Number)
                                face.TextureIndex = tx.GetInt32();
                            if (fp.Value.TryGetProperty("rotation", out var fr2) && fr2.ValueKind == JsonValueKind.Number)
                                face.Rotation = fr2.GetInt32();
                            e.Faces[fp.Name] = face;
                        }
                    }
                    m.Elements.Add(e);
                    if (!string.IsNullOrEmpty(e.Uuid)) m.ElementsByUuid[e.Uuid] = e;
                }
            }
            if (root.TryGetProperty("outliner", out var ol) && ol.ValueKind == JsonValueKind.Array)
            {
                var visited = new HashSet<string>();
                foreach (var node in ol.EnumerateArray())
                    m.Outliner.Add(ParseOutliner(node, 0, visited));
            }
            if (root.TryGetProperty("textures", out var ta) && ta.ValueKind == JsonValueKind.Array)
            {
                int tcount = 0;
                foreach (var t in ta.EnumerateArray())
                {
                    if (++tcount > MaxTextures) break;
                    if (t.ValueKind != JsonValueKind.Object) continue;
                    var tex = new BBModel.Texture();
                    if (t.TryGetProperty("name", out var tn) && tn.ValueKind == JsonValueKind.String) tex.Name = tn.GetString() ?? "";
                    if (t.TryGetProperty("source", out var ts) && ts.ValueKind == JsonValueKind.String) tex.Source = ts.GetString() ?? "";
                    if (t.TryGetProperty("frame_time", out var ft) && ft.ValueKind == JsonValueKind.Number) tex.FrameTime = ft.GetInt32();
                    if (!string.IsNullOrEmpty(tex.Source) && tex.Source.StartsWith("data:", StringComparison.Ordinal))
                    {
                        int comma = tex.Source.IndexOf(',');
                        if (comma > 0)
                        {
                            string b64 = tex.Source.Substring(comma + 1);
                            if (b64.Length <= MaxTextureBase64Chars)
                            {
                                try
                                {
                                    var decoded = Convert.FromBase64String(b64);
                                    if (decoded.Length <= MaxTextureBytes) tex.DecodedPng = decoded;
                                }
                                catch { }
                            }
                        }
                    }
                    m.Textures.Add(tex);
                }
            }
            if (root.TryGetProperty("animations", out var aa) && aa.ValueKind == JsonValueKind.Array)
            {
                int acount = 0;
                foreach (var a in aa.EnumerateArray())
                {
                    if (++acount > MaxAnimations) break;
                    if (a.ValueKind != JsonValueKind.Object) continue;
                    var anim = new BBModel.Animation();
                    if (a.TryGetProperty("name", out var an) && an.ValueKind == JsonValueKind.String) anim.Name = an.GetString() ?? "";
                    if (a.TryGetProperty("loop", out var alp) && alp.ValueKind == JsonValueKind.String) anim.Loop = alp.GetString() ?? "loop";
                    if (a.TryGetProperty("length", out var alen) && alen.ValueKind == JsonValueKind.Number) anim.Length = alen.GetSingle();
                    if (a.TryGetProperty("animators", out var ators) && ators.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var ap in ators.EnumerateObject())
                        {
                            var animator = new BBModel.Animator();
                            if (ap.Value.TryGetProperty("name", out var bn) && bn.ValueKind == JsonValueKind.String) animator.Name = bn.GetString() ?? "";
                            if (ap.Value.TryGetProperty("keyframes", out var kfa) && kfa.ValueKind == JsonValueKind.Array)
                            {
                                int kfc = 0;
                                foreach (var kf in kfa.EnumerateArray())
                                {
                                    if (++kfc > MaxKeyframesPerAnim) break;
                                    if (kf.ValueKind != JsonValueKind.Object) continue;
                                    var k = new BBModel.Keyframe();
                                    if (kf.TryGetProperty("channel", out var ch) && ch.ValueKind == JsonValueKind.String) k.Channel = ch.GetString() ?? "rotation";
                                    if (kf.TryGetProperty("time", out var tm) && tm.ValueKind == JsonValueKind.Number) k.Time = tm.GetSingle();
                                    if (kf.TryGetProperty("interpolation", out var ip) && ip.ValueKind == JsonValueKind.String) k.Interpolation = ip.GetString() ?? "linear";
                                    if (kf.TryGetProperty("data_points", out var dps) && dps.ValueKind == JsonValueKind.Array && dps.GetArrayLength() > 0)
                                    {
                                        var dp = dps[0];
                                        k.DataPoint = new[] { ReadDp(dp, "x"), ReadDp(dp, "y"), ReadDp(dp, "z") };
                                    }
                                    animator.Keyframes.Add(k);
                                }
                            }
                            animator.Keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
                            anim.Animators[ap.Name] = animator;
                        }
                    }
                    m.Animations.Add(anim);
                }
            }
            return m;
        }

        private static string ReadDp(JsonElement dp, string key)
        {
            if (dp.ValueKind != JsonValueKind.Object) return "0";
            if (!dp.TryGetProperty(key, out var v)) return "0";
            if (v.ValueKind == JsonValueKind.Null) return "0";
            if (v.ValueKind == JsonValueKind.String) return v.GetString() ?? "0";
            if (v.ValueKind == JsonValueKind.Number) return v.GetSingle().ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "0";
        }

        private static BBModel.OutlinerNode ParseOutliner(JsonElement el, int depth, HashSet<string> visited)
        {
            var node = new BBModel.OutlinerNode();
            if (el.ValueKind == JsonValueKind.String)
            {
                node.Uuid = el.GetString() ?? "";
                node.IsGroup = false;
                if (!string.IsNullOrEmpty(node.Uuid)) node.ElementUuids.Add(node.Uuid);
                return node;
            }
            if (depth > MaxOutlinerDepth || el.ValueKind != JsonValueKind.Object) return node;
            string uuid = "";
            if (el.TryGetProperty("uuid", out var u) && u.ValueKind == JsonValueKind.String) uuid = u.GetString() ?? "";
            if (!string.IsNullOrEmpty(uuid))
            {
                if (!visited.Add(uuid)) return node;
            }
            node.IsGroup = true;
            node.Uuid = uuid;
            if (el.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String) node.Name = nm.GetString() ?? "";
            if (el.TryGetProperty("origin", out var or)) node.Origin = ReadVec3(or);
            if (el.TryGetProperty("rotation", out var rot)) node.Rotation = ReadVec3(rot);
            if (el.TryGetProperty("children", out var ch) && ch.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in ch.EnumerateArray())
                {
                    if (c.ValueKind == JsonValueKind.String)
                        node.ElementUuids.Add(c.GetString() ?? "");
                    else
                        node.Children.Add(ParseOutliner(c, depth + 1, visited));
                }
            }
            return node;
        }

        private static float[] ReadVec3(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() >= 3)
                return new[] { el[0].GetSingle(), el[1].GetSingle(), el[2].GetSingle() };
            if (el.ValueKind == JsonValueKind.Object)
                return new[]
                {
                    el.TryGetProperty("x", out var x) && x.ValueKind == JsonValueKind.Number ? x.GetSingle() : 0f,
                    el.TryGetProperty("y", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetSingle() : 0f,
                    el.TryGetProperty("z", out var z) && z.ValueKind == JsonValueKind.Number ? z.GetSingle() : 0f
                };
            return new float[3];
        }
    }
}
