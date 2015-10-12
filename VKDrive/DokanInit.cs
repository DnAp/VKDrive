using Dokan;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace VKDrive
{
    public static class DokanInit
    {
        public static Thread DokanThread;
        public static int status = 1;
        public static MainFS mainFS;

        public static bool test()
        {
            return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\dokan.dll");
        }

        public static int installLib()
        {
            return installLib(false);
        }

        private static int installLib(bool twoStart)
        {
            // C:\Windows\system32\drivers\dokan.sys

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Dokan\\DokanLibrary\\DokanUninstall.exe"))
            {
                // Докан есть, поищем удалятор.
                // c:\Program Files (x86)\Dokan\DokanLibrary\
                System.Windows.Forms.MessageBox.Show("Ошибка системы. Для исправления проделай следующие действия:\n"
                    + "1) Удали драйвер Dokan, програма удаления запустится автоматически.\n"
                    + "2) Перезагрузись, это действительно важно.\n"
                    + "3) Запусти ВК Драйв повторно.\n"
                    + "В случае повторения ошибки обратитесь к системному администратору.",
                    "Проблема с драйвером Dokan", 
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                try
                {
                    Process pr = Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Dokan\\DokanLibrary\\DokanUninstall.exe");
                    pr.WaitForExit();
                    pr.Close();
                }
                catch (Exception) { }
            }
            else
            {
                if (!Properties.Settings.Default.FirstStart || twoStart)
                {
                    System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                        "Не установлен драйвер Dokan. \nЗапустить инсталятор драйвера?"
                            +"\nВ случае если эта ошибка будет повторятся обратись к системному администратору с просьбой переустановить Dokan",
                        "Запустить инсталятор драйвера Dokan?",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Exclamation
                    );
                    if (result == System.Windows.Forms.DialogResult.No)
                        return 0;
                }
                try
                {
                    if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2) // Win8/Win2012Server или выше
                    {
                        // Ставим на приложение флаг запуска с симуляцией win7
                        string installerName = Directory.GetCurrentDirectory() + "\\Resurces\\DokanInstall_0.6.0_auto.exe";
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", installerName, "WIN7RTM");
                        // HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers : WIN7RTM
                    }

                    Process pr = Process.Start("Resurces\\DokanInstall_0.6.0_auto.exe", "/S");
                    pr.WaitForExit();
                    pr.Close();
                }
                catch (Exception) {
                    return installLib(true);
                }
                if (!test())
                {
                    return installLib(true);
                }
                return 1;
            }
            return 0;
        }

        public static void start()
        {
            DokanThread = new Thread(startMainFS);
            DokanThread.Start();
        }

        public static void end()
        {
            try
            {
                Dokan.DokanNet.DokanUnmount(Properties.Settings.Default.MountPoint);
                Dokan.DokanNet.DokanRemoveMountPoint(Properties.Settings.Default.MountPoint + ":\\");
            }
            catch (Exception) { }
        }

        public static bool isWorking()
        {
            return status == 1;
        }

        private static void startMainFS(object obj)
        {
        
            DokanOptions opt = new DokanOptions();



            char mountPoint = Properties.Settings.Default.MountPoint;
            opt.MountPoint = mountPoint + ":\\";
            opt.DebugMode = false;
            opt.UseStdErr = false;

            opt.NetworkDrive = false;
            opt.VolumeLabel = "VKDrive";
            opt.UseKeepAlive = false;

            mainFS = new MainFS(mountPoint);
            status = DokanNet.DokanMain(opt, mainFS);

            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                    Console.WriteLine("Drvie letter error");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    installLib();
                    Console.WriteLine("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Console.WriteLine("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Console.WriteLine("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Console.WriteLine("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Console.WriteLine("Success");
                    break;
                default:
                    Console.WriteLine("Unknown status: %d", status);
                    break;
            }
            
            System.Environment.Exit(0);

        }
    }
}
