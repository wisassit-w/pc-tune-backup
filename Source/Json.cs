using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PcTuneBackup
{
    // Tiny JSON helper. object = Dictionary<string,object>, array = List<object>,
    // plus string / long / double / bool / null.
    public static class Json
    {
        // ---------- helpers ----------
        public static Dictionary<string, object> O(object o) { return o as Dictionary<string, object>; }
        public static List<object> A(object o) { return o as List<object>; }
        public static string S(object o) { return o == null ? null : Convert.ToString(o, CultureInfo.InvariantCulture); }
        public static long L(object o) { return Convert.ToInt64(o, CultureInfo.InvariantCulture); }
        public static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v)) return v;
            return null;
        }

        // ---------- write ----------
        public static string Write(object value)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        static void Indent(StringBuilder sb, int n) { sb.Append(' ', n * 2); }

        static void WriteValue(StringBuilder sb, object v, int lvl)
        {
            if (v == null) { sb.Append("null"); return; }
            string s = v as string;
            if (s != null) { WriteString(sb, s); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is int || v is long || v is uint || v is short || v is byte)
            {
                sb.Append(Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (v is double) { sb.Append(((double)v).ToString("R", CultureInfo.InvariantCulture)); return; }

            var d = v as Dictionary<string, object>;
            if (d != null)
            {
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\n");
                bool first = true;
                foreach (var kv in d)
                {
                    if (!first) sb.Append(",\n");
                    first = false;
                    Indent(sb, lvl + 1);
                    WriteString(sb, kv.Key);
                    sb.Append(": ");
                    WriteValue(sb, kv.Value, lvl + 1);
                }
                sb.Append("\n");
                Indent(sb, lvl);
                sb.Append("}");
                return;
            }
            var l = v as List<object>;
            if (l != null)
            {
                if (l.Count == 0) { sb.Append("[]"); return; }
                sb.Append("[\n");
                for (int i = 0; i < l.Count; i++)
                {
                    if (i > 0) sb.Append(",\n");
                    Indent(sb, lvl + 1);
                    WriteValue(sb, l[i], lvl + 1);
                }
                sb.Append("\n");
                Indent(sb, lvl);
                sb.Append("]");
                return;
            }
            throw new Exception("Unsupported JSON type: " + v.GetType().Name);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------- parse ----------
        public static object Parse(string text)
        {
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
            var p = new Parser(text);
            p.Ws();
            object v = p.Value();
            p.Ws();
            if (p.Pos < text.Length) throw new Exception("Invalid JSON: extra data at " + p.Pos);
            return v;
        }

        class Parser
        {
            readonly string s;
            public int Pos;
            public Parser(string text) { s = text; }

            public void Ws()
            {
                while (Pos < s.Length && char.IsWhiteSpace(s[Pos])) Pos++;
            }

            void Expect(char c)
            {
                if (Pos >= s.Length || s[Pos] != c) throw new Exception("Invalid JSON: expected '" + c + "' at " + Pos);
                Pos++;
            }

            public object Value()
            {
                Ws();
                if (Pos >= s.Length) throw new Exception("Invalid JSON: unexpected end");
                char c = s[Pos];
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (Lit("true")) return true;
                if (Lit("false")) return false;
                if (Lit("null")) return null;
                return Num();
            }

            bool Lit(string w)
            {
                if (string.CompareOrdinal(s, Pos, w, 0, w.Length) == 0) { Pos += w.Length; return true; }
                return false;
            }

            object Num()
            {
                int start = Pos;
                while (Pos < s.Length && "+-0123456789.eE".IndexOf(s[Pos]) >= 0) Pos++;
                string t = s.Substring(start, Pos - start);
                if (t.Length == 0) throw new Exception("Invalid JSON at " + start);
                long l;
                if (t.IndexOfAny(new[] { '.', 'e', 'E' }) < 0 &&
                    long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
                return double.Parse(t, CultureInfo.InvariantCulture);
            }

            string Str()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (true)
                {
                    if (Pos >= s.Length) throw new Exception("Invalid JSON: unterminated string");
                    char c = s[Pos++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = s[Pos++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)int.Parse(s.Substring(Pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            Pos += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                return sb.ToString();
            }

            Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                Expect('{');
                Ws();
                if (s[Pos] == '}') { Pos++; return d; }
                while (true)
                {
                    Ws();
                    string k = Str();
                    Ws();
                    Expect(':');
                    d[k] = Value();
                    Ws();
                    if (s[Pos] == ',') { Pos++; continue; }
                    Expect('}');
                    return d;
                }
            }

            List<object> Arr()
            {
                var l = new List<object>();
                Expect('[');
                Ws();
                if (s[Pos] == ']') { Pos++; return l; }
                while (true)
                {
                    l.Add(Value());
                    Ws();
                    if (s[Pos] == ',') { Pos++; continue; }
                    Expect(']');
                    return l;
                }
            }
        }
    }
}
