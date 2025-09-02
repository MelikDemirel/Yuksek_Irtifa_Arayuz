using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace GokhanUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // .NET 4.8 kontrolü
            if (!IsNet48Installed())
            {
                var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NDP48-x86-x64-AllOS-ENU.exe");
                if (File.Exists(exePath))
                {
                    MessageBox.Show(".NET Framework 4.8 bulunamadı.\nKurulum başlatılacak.",
                                    "Eksik .NET", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try
                    {
                        // Sessiz kurulum yapmak istersen "/quiet /norestart" parametre ekleyebilirsin
                        Process.Start(exePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Kurulum başlatılamadı: " + ex.Message,
                                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(".NET Framework 4.8 yüklü değil ve kurulum dosyası bulunamadı.",
                                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return; // .NET yoksa uygulama çalışmasın
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainMenu());
        }

        // .NET 4.8 kurulu mu kontrol eden metot
        static bool IsNet48Installed()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    int releaseKey = (int)ndpKey.GetValue("Release");
                    // 528040 ve üzeri = .NET Framework 4.8
                    return releaseKey >= 528040;
                }
            }
            return false;
        }
    }
}
