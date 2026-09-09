using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PDTwin.RemoteApp
{
    internal static class EndPlcSession
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DialogResult answer = MessageBox.Show(
                "Hãy Save project và đóng GX Works2 trước khi kết thúc phiên.\n\n" +
                "Bạn có chắc muốn Sign out khỏi phiên PLC này không?",
                "Kết thúc phiên PLC",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            ProcessStartInfo signOut = new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\shutdown.exe"),
                Arguments = "/l",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(signOut);
        }
    }
}
