using System;
using System.Collections.Generic;
using System.Globalization;

namespace LeafClient.Services.BBModel
{
    public static class Molang
    {
        public static float Eval(string? expr, float time)
        {
            if (string.IsNullOrEmpty(expr)) return 0f;
            string s = expr!.Trim();
            if (s.Length == 0) return 0f;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var quick)) return quick;
            var ctx = new Ctx { S = s, I = 0, Time = time };
            try { var v = ParseExpr(ctx); return float.IsFinite(v) ? v : 0f; }
            catch { return 0f; }
        }

        public static float[] EvalDataPoint(string[] dp, float time)
        {
            return new[] { Eval(dp[0], time), Eval(dp[1], time), Eval(dp[2], time) };
        }

        public static float[] EvalKeyframeChannel(BBModel.Animator animator, string channel, float time, float[] defaultValue)
        {
            var kfs = new List<BBModel.Keyframe>();
            foreach (var k in animator.Keyframes) if (k.Channel == channel) kfs.Add(k);
            if (kfs.Count == 0) return (float[])defaultValue.Clone();
            if (kfs.Count == 1 || time <= kfs[0].Time) return EvalDataPoint(kfs[0].DataPoint, time);
            var last = kfs[kfs.Count - 1];
            if (time >= last.Time) return EvalDataPoint(last.DataPoint, time);
            for (int i = 0; i < kfs.Count - 1; i++)
            {
                var a = kfs[i]; var b = kfs[i + 1];
                if (time >= a.Time && time <= b.Time)
                {
                    float span = b.Time - a.Time;
                    float t = span > 0f ? (time - a.Time) / span : 0f;
                    var av = EvalDataPoint(a.DataPoint, time);
                    var bv = EvalDataPoint(b.DataPoint, time);
                    return new[] { av[0] + (bv[0] - av[0]) * t, av[1] + (bv[1] - av[1]) * t, av[2] + (bv[2] - av[2]) * t };
                }
            }
            return (float[])defaultValue.Clone();
        }

        private sealed class Ctx { public string S = ""; public int I; public float Time; }

        private static char Peek(Ctx c) => c.I < c.S.Length ? c.S[c.I] : '\0';
        private static void SkipWs(Ctx c) { while (c.I < c.S.Length && char.IsWhiteSpace(c.S[c.I])) c.I++; }
        private static bool Eat(Ctx c, char ch) { SkipWs(c); if (c.I < c.S.Length && c.S[c.I] == ch) { c.I++; return true; } return false; }

        private static float ParseExpr(Ctx c) => ParseTern(c);

        private static float ParseTern(Ctx c)
        {
            float cond = ParseOr(c);
            SkipWs(c);
            if (Peek(c) == '?')
            {
                c.I++;
                float a = ParseExpr(c);
                Eat(c, ':');
                float b = ParseExpr(c);
                return cond != 0f ? a : b;
            }
            return cond;
        }

        private static float ParseOr(Ctx c)
        {
            float v = ParseAnd(c);
            while (true)
            {
                SkipWs(c);
                if (c.I + 1 < c.S.Length && c.S[c.I] == '|' && c.S[c.I + 1] == '|') { c.I += 2; v = (v != 0f || ParseAnd(c) != 0f) ? 1f : 0f; }
                else break;
            }
            return v;
        }

        private static float ParseAnd(Ctx c)
        {
            float v = ParseCmp(c);
            while (true)
            {
                SkipWs(c);
                if (c.I + 1 < c.S.Length && c.S[c.I] == '&' && c.S[c.I + 1] == '&') { c.I += 2; v = (v != 0f && ParseCmp(c) != 0f) ? 1f : 0f; }
                else break;
            }
            return v;
        }

        private static float ParseCmp(Ctx c)
        {
            float v = ParseAdd(c);
            while (true)
            {
                SkipWs(c);
                if (c.I + 1 < c.S.Length)
                {
                    char a = c.S[c.I], b = c.S[c.I + 1];
                    if ((a == '=' && b == '=') || (a == '!' && b == '=') || (a == '<' && b == '=') || (a == '>' && b == '='))
                    {
                        c.I += 2;
                        float r = ParseAdd(c);
                        if (a == '=') v = v == r ? 1f : 0f;
                        else if (a == '!') v = v != r ? 1f : 0f;
                        else if (a == '<') v = v <= r ? 1f : 0f;
                        else v = v >= r ? 1f : 0f;
                        continue;
                    }
                }
                if (c.I < c.S.Length && (c.S[c.I] == '<' || c.S[c.I] == '>'))
                {
                    char op = c.S[c.I]; c.I++;
                    float r = ParseAdd(c);
                    v = op == '<' ? (v < r ? 1f : 0f) : (v > r ? 1f : 0f);
                    continue;
                }
                break;
            }
            return v;
        }

        private static float ParseAdd(Ctx c)
        {
            float v = ParseMul(c);
            while (true)
            {
                SkipWs(c);
                char p = Peek(c);
                if (p == '+') { c.I++; v += ParseMul(c); }
                else if (p == '-') { c.I++; v -= ParseMul(c); }
                else break;
            }
            return v;
        }

        private static float ParseMul(Ctx c)
        {
            float v = ParseUnary(c);
            while (true)
            {
                SkipWs(c);
                char p = Peek(c);
                if (p == '*') { c.I++; v *= ParseUnary(c); }
                else if (p == '/') { c.I++; float d = ParseUnary(c); v = d == 0f ? 0f : v / d; }
                else if (p == '%') { c.I++; float d = ParseUnary(c); v = d == 0f ? 0f : v % d; }
                else break;
            }
            return v;
        }

        private static float ParseUnary(Ctx c)
        {
            SkipWs(c);
            char p = Peek(c);
            if (p == '-') { c.I++; return -ParseUnary(c); }
            if (p == '+') { c.I++; return ParseUnary(c); }
            if (p == '!') { c.I++; return ParseUnary(c) != 0f ? 0f : 1f; }
            return ParseAtom(c);
        }

        private static float ParseAtom(Ctx c)
        {
            SkipWs(c);
            if (Peek(c) == '(')
            {
                c.I++;
                float v = ParseExpr(c);
                Eat(c, ')');
                return v;
            }
            char ch = Peek(c);
            if ((ch >= '0' && ch <= '9') || ch == '.')
            {
                int start = c.I;
                while (c.I < c.S.Length && (char.IsDigit(c.S[c.I]) || c.S[c.I] == '.')) c.I++;
                return float.Parse(c.S.Substring(start, c.I - start), CultureInfo.InvariantCulture);
            }
            int idStart = c.I;
            while (c.I < c.S.Length && (char.IsLetterOrDigit(c.S[c.I]) || c.S[c.I] == '_' || c.S[c.I] == '.')) c.I++;
            string ident = c.S.Substring(idStart, c.I - idStart);
            SkipWs(c);
            if (Peek(c) == '(')
            {
                c.I++;
                var args = new List<float>();
                SkipWs(c);
                if (Peek(c) != ')')
                {
                    args.Add(ParseExpr(c));
                    while (Eat(c, ',')) args.Add(ParseExpr(c));
                }
                Eat(c, ')');
                return CallFn(ident, args, c.Time);
            }
            return ResolveVar(ident, c.Time);
        }

        private static float CallFn(string name, List<float> args, float time)
        {
            float a0 = args.Count > 0 ? args[0] : 0f;
            float a1 = args.Count > 1 ? args[1] : 0f;
            float a2 = args.Count > 2 ? args[2] : 0f;
            switch (name)
            {
                case "math.abs": return MathF.Abs(a0);
                case "math.acos": return MathF.Acos(a0) * 180f / MathF.PI;
                case "math.asin": return MathF.Asin(a0) * 180f / MathF.PI;
                case "math.atan": return MathF.Atan(a0) * 180f / MathF.PI;
                case "math.atan2": return MathF.Atan2(a0, a1) * 180f / MathF.PI;
                case "math.ceil": return MathF.Ceiling(a0);
                case "math.cos": return MathF.Cos(a0 * MathF.PI / 180f);
                case "math.exp": return MathF.Exp(a0);
                case "math.floor": return MathF.Floor(a0);
                case "math.ln": return a0 <= 0f ? 0f : MathF.Log(a0);
                case "math.mod": return a1 == 0f ? 0f : a0 % a1;
                case "math.max": return MathF.Max(a0, a1);
                case "math.min": return MathF.Min(a0, a1);
                case "math.clamp": return MathF.Max(a1, MathF.Min(a2, a0));
                case "math.lerp": return a0 + (a1 - a0) * a2;
                case "math.pi": return MathF.PI;
                case "math.pow": return MathF.Pow(a0, a1);
                case "math.random":
                {
                    float lo = args.Count > 0 ? a0 : 0f;
                    float hi = args.Count > 1 ? a1 : 1f;
                    return lo + (float)new Random().NextDouble() * (hi - lo);
                }
                case "math.round": return MathF.Round(a0);
                case "math.sin": return MathF.Sin(a0 * MathF.PI / 180f);
                case "math.sqrt": return MathF.Sqrt(MathF.Max(0f, a0));
                case "math.die_roll":
                {
                    int n = Math.Max(0, Math.Min(1000, (int)a0));
                    float sum = 0f;
                    var r = new Random();
                    for (int i = 0; i < n; i++) sum += a1 + (float)r.NextDouble() * (a2 - a1);
                    return sum;
                }
                default: return 0f;
            }
        }

        private static float ResolveVar(string name, float time)
        {
            switch (name)
            {
                case "query.anim_time":
                case "q.anim_time":
                case "query.life_time":
                case "q.life_time":
                    return time;
                case "true": return 1f;
                case "false": return 0f;
            }
            return 0f;
        }
    }
}
