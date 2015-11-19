﻿using Dokan;
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
        public static int status = 1;
        public static MainFS mainFS;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool test() // todo Переделать на установлен ли драйвер?
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
                    Process pr = Process.Start("Resurces\\DokanInstall_0.8.0-RC2.exe"); // , "/S"
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
            try {
                DokanOptions opt = new DokanOptions();

                char mountPoint = Properties.Settings.Default.MountPoint;
                opt.MountPoint = mountPoint + ":\\";
                opt.DebugMode = false;
                opt.UseStdErr = true;

                opt.NetworkDrive = true;
                opt.VolumeLabel = "VKDrive";
                opt.UseKeepAlive = false;

                mainFS = new MainFS(mountPoint);
                status = DokanNet.DokanMain(opt, mainFS);

                switch (status)
                {
                    case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                        Log.Fatal("Drvie letter error");
                        break;
                    case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                        installLib();
                        Log.Fatal("Driver install error");
                        break;
                    case DokanNet.DOKAN_MOUNT_ERROR:
                        Log.Fatal("Mount error");
                        break;
                    case DokanNet.DOKAN_START_ERROR:
                        Log.Fatal("Start error");
                        break;
                    case DokanNet.DOKAN_ERROR:
                        Log.Fatal("Unknown error");
                        break;
                    case DokanNet.DOKAN_SUCCESS:
                        Log.Info("Start success");
                        break;
                    default:
                        Log.Fatal("Unknown status: " + status);
                        break;
                }
            }catch(Exception e)
            {
                ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                Log.Fatal("StartMainFS fail", e);
            }
            System.Environment.Exit(0);

        }
    }
}
