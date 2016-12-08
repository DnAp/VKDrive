using DokanNet;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;


namespace VKDrive
{
    public static class DokanInit
    {
        public static Thread DokanThread;
        public static int Status = 1;
        public static MainFs MainFs = null;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool Test() // todo Переделать на установлен ли драйвер?
        {
            return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\dokan.dll");
        }

        public static int InstallLib()
        {
            return InstallLib(false);
        }

        private static int InstallLib(bool twoStart)
        {
            // C:\Windows\system32\drivers\dokan.sys

            if (
                File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                            "\\Dokan\\DokanLibrary\\DokanUninstall.exe"))
            {
                // Докан есть, поищем удалятор.
                // c:\Program Files (x86)\Dokan\DokanLibrary\
                System.Windows.Forms.MessageBox.Show(@"Ошибка системы. Для исправления проделай следующие действия:
1) Удали драйвер Dokan, програма удаления запустится автоматически.
2) Перезагрузись, это действительно важно.
3) Запусти ВК Драйв повторно.
В случае повторения ошибки обратитесь к системному администратору.",
                    @"Проблема с драйвером Dokan",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                try
                {
                    var pr =
                        Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                                      "\\Dokan\\DokanLibrary\\DokanUninstall.exe");
                    if (pr != null)
                    {
                        pr.WaitForExit();
                        pr.Close();
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            else
            {
                if (!Properties.Settings.Default.FirstStart || twoStart)
                {
                    System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                        @"Не установлен драйвер Dokan. 
Запустить инсталятор драйвера?
В случае если эта ошибка будет повторятся обратись к системному администратору с просьбой переустановить Dokan",
                        @"Запустить инсталятор драйвера Dokan?",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Exclamation
                    );
                    if (result == System.Windows.Forms.DialogResult.No)
                        return 0;
                }
                try
                {
                    Process pr = Process.Start("Resurces\\DokanInstall_0.8.0-RC2.exe"); // , "/S"
                    pr.WaitForExit();
                    pr.Close();
                }
                catch (Exception)
                {
                    return InstallLib(true);
                }
                return !Test() ? InstallLib(true) : 1;
            }
            return 0;
        }

        public static void Start()
        {
            DokanThread = new Thread(StartMainFs);
            DokanThread.Start();
        }

        public static void End()
        {
            try
            {
                Dokan.Unmount(Properties.Settings.Default.MountPoint[0]);
                Dokan.RemoveMountPoint(Properties.Settings.Default.MountPoint + ":\\");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static bool IsWorking()
        {
            return Status == 1;
        }

        private static void StartMainFs(object obj)
        {
            try
            {
                MainFs = new MainFs();
                MainFs.Mount(Properties.Settings.Default.MountPoint + ":\\", DokanOptions.StderrOutput);
            }
            catch (Exception e)
            {
                InstallLib();
                var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                log.Fatal("StartMainFS fail", e);
                Status = -1;
            }
            System.Environment.Exit(0);
        }
    }
}