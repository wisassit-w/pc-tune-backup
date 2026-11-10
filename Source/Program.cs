using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("PC Tune Backup")]
[assembly: AssemblyProduct("PC Tune Backup")]
[assembly: AssemblyCompany("Niran Shop")]
[assembly: AssemblyVersion("3.1.0.0")]

namespace PcTuneBackup
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.Message, "PC Tune Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            try { Application.Run(new MainForm()); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PC Tune Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
