﻿using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using VKDrive.Files;
using VKDrive.VKAPI;

namespace VKDrive
{
    public partial class Browser : Form
    {
        int webBrowserLogoutWait = 0;


        public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Browser()
        {
            Log.Info("Start vkdrive");
            

            bool mutexWasCreated;
            System.Threading.Mutex mutex = new System.Threading.Mutex(true, "VKDrive", out mutexWasCreated);
            if (!mutexWasCreated)
            {
                Log.Warn("Double start! Exit.");
                Environment.Exit(0);
                return;
            }

            InitializeComponent();

            // Важная процедура проверки установленности dokan
            if (!DokanInit.test())
            {
                Log.Warn("Dokan not installed");
                if (DokanInit.installLib() == 0)
                {
                    Log.Warn("Dokan fail install?");
                    notifyIcon1.Visible = false;
                    System.Environment.Exit(0);
                    return;
                }
            }
            

            // эта штка говорит что мы не показываем форму при запуске
            this.Visible = false;
            this.IsVisibilityChangeAllowed = false;
            DokanInit.end();
            
            toAuthVKPage();
        }
        private void Browser_Load(object sender, EventArgs e)
        {
            this.Hide();
            // todo найти диск даже если он занят. Но не ошметком вкдрайв
            if (Properties.Settings.Default.FirstStart)
            {
                /// Первый старт, поиск подходящего диска
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                string busyDrives = "";
                foreach (DriveInfo drive in allDrives)
                {
                    busyDrives += drive.Name[0];
                }
                char disk;
                disk = '*';
                for (char curDisk = 'V'; curDisk <= 'Z'; curDisk++)
                {
                    if (busyDrives.IndexOf(curDisk) == -1)
                    {
                        disk = curDisk;
                        break;
                    }
                }
                if (disk == '*')
                {
                    for (char curDisk = 'A'; curDisk <= 'V'; curDisk++)
                    {
                        if (busyDrives.IndexOf(curDisk) == -1)
                        {
                            MessageBox.Show("Ничего страшного если VKDrive запустится на диске " + curDisk + "?");
                            disk = curDisk;
                            break;
                        }
                    }
                }
                if (disk == '*')
                {
                    MessageBox.Show("Зачем тебе столько дисков? Мне правда интересно. Ах да, запуститься VKDrive не сможет:'(");
                    System.Environment.Exit(0);
                    return;
                }

                Properties.Settings.Default.MountPoint = disk;
                Properties.Settings.Default.FirstStart = false;
                Properties.Settings.Default.Save();
                SetVisibleCore(true);

                Timer timer = new Timer();
                timer.Tick += timer_Tick;
                timer.Interval = 500;
                timer.Start();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.Name[0] == Properties.Settings.Default.MountPoint)
                {
                    ((Timer)sender).Stop();
                    try
                    {
                        // плохо работает
                        System.Diagnostics.Process.Start(drive.Name[0] + @":\");
                    } catch(Exception){}
                }
            }
        }

        bool IsVisibilityChangeAllowed { get; set; }

        protected override void SetVisibleCore(bool value)
        {
            if (this.IsVisibilityChangeAllowed)
            {
                base.SetVisibleCore(value);
            }
        }  

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.IsVisibilityChangeAllowed = true;
            this.Show();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try {
                this.IsVisibilityChangeAllowed = true;

                Log.Debug("Document load completed: " + webBrowser1.Url.AbsoluteUri);
                if (webBrowserLogoutWait == 1)
                {
                    HtmlElement logout = webBrowser1.Document.GetElementById("logout_link");
                    if (logout != null)
                    {
                        Log.Debug("Document load completed have logout lonk: " + logout.GetAttribute("href"));
                        webBrowser1.Url = new Uri(logout.GetAttribute("href"));
                        webBrowserLogoutWait = 2;
                    }
                }
                else if (webBrowserLogoutWait == 2 && webBrowser1.Url.AbsoluteUri.IndexOf("login") > -1)
                {
                    webBrowserLogoutWait = 0;
                    toAuthVKPage();
                    this.Show();
                }
                else
                {
                    /// Первый запуск не авторизовывает приложение
                    /// https://oauth.vk.com/authorize?client_id=1234&display=popup&response_type=token&scope=audio,friends&redirect_uri=http%3A%2F%2Foauth.vk.com%2Fblank.html
                    /// и ожидаем ввода логина
                    /// Когда вход осуществлен переадресация происходит заголовками и мы сразу получаем
                    /// https://oauth.vk.com/blank.html
                    /// Прикинь, если аккаунт в вк заблокировали то показывается
                    /// https://oauth.vk.com/login?act=blocked
                    ///

                    string locationURL = webBrowser1.Url.AbsoluteUri;
                    string successURL = "https://oauth.vk.com/blank.html";
                    string blockedURL = "https://oauth.vk.com/login?act=blocked";

                    if (locationURL.Length >= blockedURL.Length && locationURL.Substring(0, blockedURL.Length) == blockedURL)
                    {
                        MessageBox.Show("ВКонтакте рассказал мне страшную историю: общий смысл сводится к тому что тебе нужно зайти на https://vk.com/", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                        return;
                    }

                    if (locationURL.Length < successURL.Length || locationURL.Substring(0, successURL.Length) != successURL)
                    {
                        this.Show();
                        return;
                    }
                    // все прошло ок
                    this.Hide();

                    string fullString = (string)locationURL.Split('#').GetValue(1);
                    string[] paramSrc = fullString.Split('&');

                    Dictionary<string, string> sessionData = new Dictionary<string, string>();
                    string[] curP;
                    for (int i = 0; i < paramSrc.Length; i++)
                    {
                        curP = paramSrc[i].Split('=');
                        sessionData.Add(curP[0], curP[1]);
                    }

                    VKAPI.VKAPILibrary api = VKAPI.VKAPILibrary.Instance;
                    api.AppID = Properties.Settings.Default.VKAppId;
                    api.Expire = Convert.ToInt32(sessionData["expires_in"]);
                    api.UserID = Convert.ToInt32(sessionData["user_id"]);
                    api.AccessTokien = (string)sessionData["access_token"];

                    JArray apiResult = (JArray)VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("users.get", new Dictionary<string, string>() { { "uids", sessionData["user_id"] } }));
                    SerializationObject.User user = apiResult[0].ToObject<SerializationObject.User>();

                    notifyIcon1.Text = "VKDrive: " + user.FirstName + " " + user.LastName;

                    DokanInit.start();
                }
            }catch(Exception ex)
            {
                Log.Fatal("webBrowser1_DocumentCompleted exception", ex);
                MessageBox.Show("Фатальная ошибка. "+ex.Message);
            }
        }

        private void toAuthVKPage()
        {
            string scope = "audio,friends,photos"; // ,photos,docs
            //Properties.Settings.Default.VKAppId
            webBrowser1.Url = new Uri("https://oauth.vk.com/authorize?client_id=" + Properties.Settings.Default.VKAppId +
                "&display=popup&response_type=token&scope="+scope+"&redirect_uri=http%3A%2F%2Foauth.vk.com%2Fblank.html");
        }



        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DokanInit.end();
            this.Close();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Точно выйти из аккаунта ВКонтакте?", "Внимание", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                webBrowserLogoutWait = 1;
                webBrowser1.Url = new Uri("https://vk.com");
                //DokanInit.end();
            }
        }


        private void Browser_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Чет не срабатывает 
            //System.Environment.Exit(0);
            DokanInit.end();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (DokanInit.isWorking())
            {
                System.Diagnostics.Process.Start("explorer", Properties.Settings.Default.MountPoint + ":\\");
            }
            else
            {
                this.Focus();
            }
        }

    }
}
