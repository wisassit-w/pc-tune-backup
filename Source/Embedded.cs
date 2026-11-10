using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PcTuneBackup
{
    // Saves the profile INSIDE a copy of this exe:
    // [exe bytes][profile json utf-8][8 bytes length][8 bytes magic "PTBPROF1"]
    static class Embedded
    {
        const string Magic = "PTBPROF1";

        static void ReadFully(Stream s, byte[] buf)
        {
            int off = 0;
            while (off < buf.Length)
            {
                int n = s.Read(buf, off, buf.Length - off);
                if (n <= 0) throw new EndOfStreamException();
                off += n;
            }
        }

        static long PayloadLen(FileStream fs)
        {
            if (fs.Length < 16) return 0;
            fs.Seek(-16, SeekOrigin.End);
            var tail = new byte[16];
            ReadFully(fs, tail);
            if (Encoding.ASCII.GetString(tail, 8, 8) != Magic) return 0;
            long len = BitConverter.ToInt64(tail, 0);
            if (len <= 0 || len > fs.Length - 16) return 0;
            return len;
        }

        public static string ReadFrom(string exePath)
        {
            using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long len = PayloadLen(fs);
                if (len == 0) return null;
                fs.Seek(-16 - len, SeekOrigin.End);
                var buf = new byte[len];
                ReadFully(fs, buf);
                return Encoding.UTF8.GetString(buf);
            }
        }

        public static string ReadOwn()
        {
            try { return ReadFrom(Application.ExecutablePath); }
            catch { return null; }
        }

        public static void WriteExe(string target, string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            using (var src = new FileStream(Application.ExecutablePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(target, FileMode.Create, FileAccess.Write))
            {
                long old = PayloadLen(src);
                long baseLen = old == 0 ? src.Length : src.Length - 16 - old;
                src.Seek(0, SeekOrigin.Begin);
                var buf = new byte[81920];
                long left = baseLen;
                while (left > 0)
                {
                    int n = src.Read(buf, 0, (int)Math.Min((long)buf.Length, left));
                    if (n <= 0) break;
                    dst.Write(buf, 0, n);
                    left -= n;
                }
                dst.Write(payload, 0, payload.Length);
                dst.Write(BitConverter.GetBytes((long)payload.Length), 0, 8);
                byte[] m = Encoding.ASCII.GetBytes(Magic);
                dst.Write(m, 0, m.Length);
            }
        }
    }
}
