using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PcTuneBackup
{
    public interface ISettingsModule
    {
        string Key { get; }
        string Title { get; }
        object Export(Action<string> log);
        void Restore(object data, Action<string> log);
    }

    static class Shell
    {
        public static readonly Encoding Oem;

        static Shell()
        {
            Encoding enc;
            try { enc = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); }
            catch { enc = Encoding.UTF8; }
            Oem = enc;
        }

        public static string Run(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args);
            psi.RedirectStandardOutput = true;
            psi.StandardOutputEncoding = Oem;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
        }

        public static RegistryKey Hive(string h)
        {
            return RegistryKey.OpenBaseKey(
                h == "HKLM" ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
                RegistryView.Registry64);
        }
    }

    // ======================= Registry =======================
    class RegistryModule : ISettingsModule
    {
        public string Key { get { return "registry"; } }
        public string Title { get { return "Registry: Input Lag / Network / HAGS / Game Mode"; } }

        class Target
        {
            public string Path;
            public string[] Names;
            public Target(string p, params string[] n) { Path = p; Names = n; }
        }

        static readonly Target[] Targets = new Target[]
        {
            new Target(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "NetworkThrottlingIndex", "SystemResponsiveness"),
            new Target(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                "GPU Priority", "Priority", "Scheduling Category", "SFIO Priority"),
            new Target(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation"),
            new Target(@"HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters", "MouseDataQueueSize"),
            new Target(@"HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters", "KeyboardDataQueueSize"),
            new Target(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode"),
            new Target(@"HKCU\Software\Microsoft\GameBar", "AutoGameModeEnabled", "AllowAutoGameMode"),
            new Target(@"HKCU\Control Panel\Mouse", "MouseSpeed", "MouseThreshold1", "MouseThreshold2", "MouseSensitivity"),
            new Target(@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\*",
                "TcpAckFrequency", "TCPNoDelay", "TcpDelAckTicks"),
        };

        static void Split(string full, out string hive, out string sub)
        {
            int i = full.IndexOf('\\');
            hive = full.Substring(0, i);
            sub = full.Substring(i + 1);
        }

        static object ToNode(object val, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord: return (long)unchecked((uint)(int)val);
                case RegistryValueKind.QWord: return (long)val;
                case RegistryValueKind.Binary: return Convert.ToBase64String((byte[])val);
                case RegistryValueKind.MultiString:
                    var arr = new List<object>();
                    foreach (var x in (string[])val) arr.Add(x);
                    return arr;
                default: return val.ToString();
            }
        }

        static object FromNode(object raw, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord: return unchecked((int)(uint)Json.L(raw));
                case RegistryValueKind.QWord: return Json.L(raw);
                case RegistryValueKind.Binary: return Convert.FromBase64String(Json.S(raw));
                case RegistryValueKind.MultiString:
                    return Json.A(raw).Select(x => Json.S(x)).ToArray();
                default: return Json.S(raw);
            }
        }

        static string Display(object v)
        {
            var b = v as byte[];
            if (b != null) return "(binary " + b.Length + " bytes)";
            var ms = v as string[];
            if (ms != null) return string.Join(", ", ms);
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public object Export(Action<string> log)
        {
            var list = new List<object>();
            foreach (var t in Targets)
            {
                string hive, sub;
                Split(t.Path, out hive, out sub);
                bool wildcard = sub.EndsWith("\\*");
                string baseSub = wildcard ? sub.Substring(0, sub.Length - 2) : sub;
                using (var root = Shell.Hive(hive))
                using (var baseKey = root.OpenSubKey(baseSub))
                {
                    if (baseKey == null) continue;

                    var keys = new List<RegistryKey>();
                    if (wildcard)
                    {
                        foreach (var n in baseKey.GetSubKeyNames())
                        {
                            var opened = baseKey.OpenSubKey(n);
                            if (opened != null) keys.Add(opened);
                        }
                    }
                    else keys.Add(baseKey);

                    var seen = new HashSet<string>();
                    foreach (var key in keys)
                    {
                        foreach (var name in t.Names)
                        {
                            object val = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                            if (val == null) continue;
                            if (wildcard && !seen.Add(name)) continue;
                            var kind = key.GetValueKind(name);
                            var e = new Dictionary<string, object>();
                            e["path"] = t.Path;
                            e["name"] = name;
                            e["kind"] = kind.ToString();
                            e["value"] = ToNode(val, kind);
                            list.Add(e);
                            log("  " + name + " = " + Display(val));
                        }
                    }
                    if (wildcard) foreach (var key2 in keys) key2.Close();
                }
            }
            log("  registry values: " + list.Count);
            return list;
        }

        public void Restore(object data, Action<string> log)
        {
            var all = Json.A(data);
            int ok = 0;
            foreach (var item in all)
            {
                try
                {
                    var e = Json.O(item);
                    string path = Json.S(e["path"]);
                    string name = Json.S(e["name"]);
                    var kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), Json.S(e["kind"]));
                    object value = FromNode(e["value"], kind);

                    string hive, sub;
                    Split(path, out hive, out sub);
                    using (var root = Shell.Hive(hive))
                    {
                        var targets = new List<string>();
                        if (sub.EndsWith("\\*"))
                        {
                            using (var b = root.OpenSubKey(sub.Substring(0, sub.Length - 2)))
                            {
                                if (b != null)
                                    foreach (var n in b.GetSubKeyNames())
                                        targets.Add(sub.Substring(0, sub.Length - 1) + n);
                            }
                        }
                        else targets.Add(sub);

                        foreach (var tk in targets)
                        {
                            using (var k = root.CreateSubKey(tk))
                            {
                                k.SetValue(name, value, kind);
                            }
                        }
                    }
                    ok++;
                }
                catch (Exception ex) { log("  ! " + ex.Message); }
            }
            log("  restored " + ok + "/" + all.Count + " values");
        }
    }

    // ======================= Netsh =======================
    class NetshModule : ISettingsModule
    {
        public string Key { get { return "netsh"; } }
        public string Title { get { return "Netsh: TCP / Network stack"; } }

        public object Export(Action<string> log)
        {
            var d = new Dictionary<string, object>();
            d["dump"] = Shell.Run("netsh", "interface dump");
            d["tcpGlobalRaw"] = Shell.Run("netsh", "int tcp show global");
            return d;
        }

        public void Restore(object data, Action<string> log)
        {
            var d = Json.O(data);
            string tmp = Path.Combine(Path.GetTempPath(), "netsh_restore.txt");
            File.WriteAllText(tmp, Json.S(d["dump"]), Shell.Oem);
            Shell.Run("netsh", "-f \"" + tmp + "\"");
            File.Delete(tmp);
            log("  netsh script applied");
        }
    }

    // ======================= LAN Adapter =======================
    class AdapterModule : ISettingsModule
    {
        public string Key { get { return "adapters"; } }
        public string Title { get { return "LAN Adapter driver (Buffers / RSS / Power saving)"; } }

        const string ClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

        static bool AllDigits(string s)
        {
            if (s.Length == 0) return false;
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        // physical card = PCI or USB device (virtual adapters are not)
        static bool IsPhysical(RegistryKey k)
        {
            foreach (var n in new string[] { "ComponentId", "MatchingDeviceId" })
            {
                string v = k.GetValue(n) as string;
                if (v == null) continue;
                v = v.ToLowerInvariant();
                if (v.StartsWith("pci\\") || v.StartsWith("usb\\")) return true;
            }
            return false;
        }

        public object Export(Action<string> log)
        {
            var result = new List<object>();
            using (var hklm = Shell.Hive("HKLM"))
            using (var cls = hklm.OpenSubKey(ClassPath))
            {
                if (cls == null) return result;
                foreach (var sub in cls.GetSubKeyNames())
                {
                    if (!AllDigits(sub)) continue;
                    using (var k = cls.OpenSubKey(sub))
                    {
                        if (k == null) continue;
                        string desc = k.GetValue("DriverDesc") as string;
                        if (desc == null || !IsPhysical(k)) continue;

                        var props = new Dictionary<string, object>();
                        using (var ps = k.OpenSubKey(@"Ndi\params"))
                        {
                            if (ps != null)
                                foreach (var p in ps.GetSubKeyNames())
                                {
                                    object v = k.GetValue(p);
                                    if (v != null) props[p] = v.ToString();
                                }
                        }
                        var o = new Dictionary<string, object>();
                        o["driverDesc"] = desc;
                        o["properties"] = props;
                        result.Add(o);
                        log("  adapter: " + desc + " (" + props.Count + " props)");
                    }
                }
            }
            return result;
        }

        public void Restore(object data, Action<string> log)
        {
            using (var hklm = Shell.Hive("HKLM"))
            using (var cls = hklm.OpenSubKey(ClassPath, true))
            {
                foreach (var item in Json.A(data))
                {
                    var a = Json.O(item);
                    string desc = Json.S(a["driverDesc"]);
                    bool found = false;
                    foreach (var sub in cls.GetSubKeyNames())
                    {
                        if (!AllDigits(sub)) continue;
                        using (var k = cls.OpenSubKey(sub, true))
                        {
                            if (k == null || (k.GetValue("DriverDesc") as string) != desc) continue;
                            found = true;
                            foreach (var kv in Json.O(a["properties"]))
                                k.SetValue(kv.Key, Json.S(kv.Value), RegistryValueKind.String);
                        }
                        Shell.Run("powershell",
                            "-NoProfile -Command \"Restart-NetAdapter -InterfaceDescription '" + desc.Replace("'", "''") + "'\"");
                        log("  adapter restored: " + desc);
                    }
                    if (!found) log("  ! adapter not found on this PC: " + desc);
                }
            }
        }
    }

    // ======================= Power Plan =======================
    class PowerPlanModule : ISettingsModule
    {
        public string Key { get { return "powerplan"; } }
        public string Title { get { return "Power Plan (Ultimate Performance etc.)"; } }

        static readonly Regex GuidRx = new Regex(@"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}");

        public object Export(Action<string> log)
        {
            string active = Shell.Run("powercfg", "/getactivescheme");
            var m = GuidRx.Match(active);
            if (!m.Success) throw new Exception("cannot read active power scheme");

            string tmp = Path.Combine(Path.GetTempPath(), "scheme.pow");
            if (File.Exists(tmp)) File.Delete(tmp);
            Shell.Run("powercfg", "/export \"" + tmp + "\" " + m.Value);
            byte[] bytes = File.ReadAllBytes(tmp);
            File.Delete(tmp);
            log("  " + active.Trim());

            var d = new Dictionary<string, object>();
            d["activeText"] = active.Trim();
            d["guid"] = m.Value;
            d["pow"] = Convert.ToBase64String(bytes);
            return d;
        }

        public void Restore(object data, Action<string> log)
        {
            var d = Json.O(data);
            string tmp = Path.Combine(Path.GetTempPath(), "scheme_restore.pow");
            File.WriteAllBytes(tmp, Convert.FromBase64String(Json.S(d["pow"])));
            string res = Shell.Run("powercfg", "/import \"" + tmp + "\"");
            File.Delete(tmp);
            var m = GuidRx.Match(res);
            if (!m.Success) throw new Exception("power plan import failed");
            Shell.Run("powercfg", "/setactive " + m.Value);
            log("  power plan imported and activated");
        }
    }

    // ======================= MSI Mode =======================
    class MsiModeModule : ISettingsModule
    {
        public string Key { get { return "msi"; } }
        public string Title { get { return "MSI Mode (GPU / LAN / PCI devices)"; } }

        const string PciRoot = @"SYSTEM\CurrentControlSet\Enum\PCI";
        const string Tail = @"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

        public object Export(Action<string> log)
        {
            var list = new List<object>();
            using (var hklm = Shell.Hive("HKLM"))
            using (var pci = hklm.OpenSubKey(PciRoot))
            {
                if (pci == null) return list;
                foreach (var dev in pci.GetSubKeyNames())
                {
                    using (var devKey = pci.OpenSubKey(dev))
                    {
                        if (devKey == null) continue;
                        foreach (var inst in devKey.GetSubKeyNames())
                        {
                            string full = PciRoot + "\\" + dev + "\\" + inst + "\\" + Tail;
                            using (var k = hklm.OpenSubKey(full))
                            {
                                if (k == null) continue;
                                object msiObj = k.GetValue("MSISupported");
                                if (!(msiObj is int)) continue;

                                string desc = dev;
                                using (var instKey = devKey.OpenSubKey(inst))
                                {
                                    if (instKey != null)
                                    {
                                        string dd = instKey.GetValue("DeviceDesc") as string;
                                        if (dd != null)
                                        {
                                            int idx = dd.LastIndexOf(';');
                                            desc = idx >= 0 ? dd.Substring(idx + 1) : dd;
                                        }
                                    }
                                }
                                var o = new Dictionary<string, object>();
                                o["path"] = full;
                                o["desc"] = desc;
                                o["MSISupported"] = (long)(int)msiObj;
                                object lim = k.GetValue("MessageNumberLimit");
                                if (lim is int) o["MessageNumberLimit"] = (long)(int)lim;
                                list.Add(o);
                            }
                        }
                    }
                }
            }
            log("  devices with MSI key: " + list.Count);
            return list;
        }

        public void Restore(object data, Action<string> log)
        {
            var all = Json.A(data);
            int ok = 0;
            using (var hklm = Shell.Hive("HKLM"))
            {
                foreach (var item in all)
                {
                    var e = Json.O(item);
                    try
                    {
                        using (var k = hklm.OpenSubKey(Json.S(e["path"]), true))
                        {
                            if (k == null) { log("  ! device not found: " + Json.S(Json.Get(e, "desc"))); continue; }
                            k.SetValue("MSISupported", (int)Json.L(e["MSISupported"]), RegistryValueKind.DWord);
                            object lim = Json.Get(e, "MessageNumberLimit");
                            if (lim != null)
                                k.SetValue("MessageNumberLimit", (int)Json.L(lim), RegistryValueKind.DWord);
                            ok++;
                        }
                    }
                    catch (Exception ex) { log("  ! " + ex.Message); }
                }
            }
            log("  MSI restored " + ok + "/" + all.Count + " (reboot required)");
        }
    }

    // ======================= Game Files =======================
    class FileModule : ISettingsModule
    {
        public string Key { get { return "files"; } }
        public string Title { get { return "FiveM / GTA files (commandline.txt, gta5_settings.xml, ...)"; } }

        public List<string> ExtraPaths = new List<string>();

        IEnumerable<string> Candidates()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            yield return Path.Combine(appData, Path.Combine("CitizenFX", "gta5_settings.xml"));
            string ini = Path.Combine(local, Path.Combine("FiveM", Path.Combine("FiveM.app", "CitizenFX.ini")));
            yield return ini;
            yield return Path.Combine(local, Path.Combine("FiveM", Path.Combine("FiveM.app", "commandline.txt")));
            yield return Path.Combine(docs, Path.Combine("Rockstar Games", Path.Combine("GTA V", "settings.xml")));

            if (File.Exists(ini))
            {
                string[] lines = null;
                try { lines = File.ReadAllLines(ini); } catch { }
                if (lines != null)
                    foreach (var line in lines)
                        if (line.StartsWith("IVPath=", StringComparison.OrdinalIgnoreCase))
                        {
                            string dir = line.Substring(7).Trim();
                            if (Directory.Exists(dir)) yield return Path.Combine(dir, "commandline.txt");
                        }
            }
            foreach (var p in ExtraPaths) yield return p;
        }

        public object Export(Action<string> log)
        {
            var list = new List<object>();
            var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Candidates())
            {
                if (!done.Add(path) || !File.Exists(path)) continue;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    var o = new Dictionary<string, object>();
                    o["path"] = path;
                    o["data"] = Convert.ToBase64String(ms.ToArray());
                    list.Add(o);
                }
                log("  file: " + path);
            }
            if (list.Count == 0) log("  no game files found");
            return list;
        }

        public void Restore(object data, Action<string> log)
        {
            foreach (var item in Json.A(data))
            {
                try
                {
                    var e = Json.O(item);
                    string path = Json.S(e["path"]);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                    File.WriteAllBytes(path, Convert.FromBase64String(Json.S(e["data"])));
                    log("  file restored: " + path);
                }
                catch (Exception ex) { log("  ! " + ex.Message); }
            }
        }
    }
}
