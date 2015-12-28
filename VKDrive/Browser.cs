using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using VKDrive.Properties;
using VKDrive.VKAPI;
using static VKDrive.Properties.Settings;

namespace VKDrive
{
    public partial class Browser : Form
    {
        int _webBrowserLogoutWait = 0;
	    private System.Threading.Mutex _mutex;

		public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Browser()
        {
            Log.Info("Start vkdrive");
            
            bool mutexWasCreated;
			_mutex = new System.Threading.Mutex(true, "VKDrive", out mutexWasCreated);
            if (!mutexWasCreated)
            {
                Log.Warn("Double start! Exit.");
                Environment.Exit(0);
            }

            InitializeComponent();

            // Важная процедура проверки установленности dokan
            if (!DokanInit.Test())
            {
                Log.Warn("Dokan not installed");
                if (DokanInit.InstallLib() == 0)
                {
                    Log.Warn("Dokan fail install?");
                    notifyIcon1.Visible = false;
                    Environment.Exit(0);
                }
            }
            
            // эта штка говорит что мы не показываем форму при запуске
            this.Visible = false;
            this.IsVisibilityChangeAllowed = false;
            DokanInit.End();
			
			ChangeDriverName();

			ToAuthVkPage();
        }

		/// Проверка и поиск диска
		private void ChangeDriverName()
	    {
			var busyDrives = new HashSet<String>();
			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				busyDrives.Add(drive.Name.Substring(0, drive.Name.IndexOf(":", StringComparison.Ordinal)));
			}

			if (!busyDrives.Contains(Default.MountPoint))
				return;

			var curDisk = 'V';
			while (curDisk <= 'Z')
			{
				if (!busyDrives.Contains(curDisk.ToString()))
				{
					Default.MountPoint = curDisk.ToString();
					Default.Save();
					return;
				}
				curDisk++;
			}
			curDisk = 'A';
            while (curDisk < 'V')
			{
				if (!busyDrives.Contains(curDisk.ToString()))
				{
					Default.MountPoint = curDisk.ToString();
					Default.Save();
                    return;
				}
				curDisk++;
			}
        	MessageBox.Show(Resources.Browser_Browser_Load_BusyAllDrivers);
			Environment.Exit(0);


			// Есть беда, докан не умеет диски из 2 букв, код дальше выбирает именно его
			/*
			var curDisk = 'V';
			var prefix = "";
			while (true)
			{
				while(curDisk <= 'Z')
				{
					if (!busyDrives.Contains(prefix+curDisk))
					{
						Default.MountPoint = prefix+curDisk;
						return;
					}
					curDisk++;
				}
				if (prefix == "")
				{
					prefix = "A";
				}
				else
				{
					var lastLetter = prefix[prefix.Length - 1];
					if (lastLetter == 'Z')
					{
						prefix += "A";
					}
					else
					{
						lastLetter++;
						prefix = prefix.Substring(0, prefix.Length - 1) + lastLetter;
					}
				}
				curDisk = 'A';
			}*/
		}

		private void Browser_Load(object sender, EventArgs e)
        {
            this.Hide();
			
            Default.FirstStart = false;
            Default.Save();
            SetVisibleCore(true);

			/*
			var timer = new Timer();
            timer.Tick += timer_Tick;
            timer.Interval = 500;
            timer.Start();
			*/
        }
		/*
        private void timer_Tick(object sender, EventArgs e)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.Name[0] == Default.MountPoint)
                {
                    ((Timer)sender).Stop();
                    try
                    {
                        // плохо работает
                        System.Diagnostics.Process.Start(drive.Name[0] + @":\");
                    } catch(Exception){}
                }
            }
        }*/

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
            if (DokanInit.IsWorking())
            {
                System.Diagnostics.Process.Start("explorer", Default.MountPoint + @":\\");
            }
            else
            {
                this.IsVisibilityChangeAllowed = true;
                this.Show();
                this.Focus();
            }
            
        }
        


        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
	        try
	        {
		        this.IsVisibilityChangeAllowed = true;

		        Log.Debug("Document load completed: " + webBrowser1.Url.AbsoluteUri);
		        if (_webBrowserLogoutWait == 1)
		        {
			        HtmlElement logout = webBrowser1.Document?.GetElementById("logout_link");
			        if (logout != null)
			        {
				        Log.Debug("Document load completed have logout lonk: " + logout.GetAttribute("href"));
				        webBrowser1.Url = new Uri(logout.GetAttribute("href"));
				        _webBrowserLogoutWait = 2;
			        }
		        }
		        else if (_webBrowserLogoutWait == 2 && webBrowser1.Url.AbsoluteUri.IndexOf("login", StringComparison.Ordinal) > -1)
		        {
			        _webBrowserLogoutWait = 0;
			        ToAuthVkPage();
			        this.Show();
		        }
		        else
		        {
			        // Первый запуск не авторизовывает приложение
			        // https://oauth.vk.com/authorize?client_id=1234&display=popup&response_type=token&scope=audio,friends&redirect_uri=http%3A%2F%2Foauth.vk.com%2Fblank.html
			        // и ожидаем ввода логина
			        // Когда вход осуществлен переадресация происходит заголовками и мы сразу получаем
			        // https://oauth.vk.com/blank.html
			        // Прикинь, если аккаунт в вк заблокировали то показывается
			        // https://oauth.vk.com/login?act=blocked

			        string locationUrl = webBrowser1.Url.AbsoluteUri;
			        string successUrl = "https://oauth.vk.com/blank.html";
			        string blockedUrl = "https://oauth.vk.com/login?act=blocked";

			        if (locationUrl.Length >= blockedUrl.Length && locationUrl.Substring(0, blockedUrl.Length) == blockedUrl)
			        {
				        MessageBox.Show(Resources.Browser_webBrowser1_DocumentCompleted_Banned, Resources.Message_Error,
					        MessageBoxButtons.OK, MessageBoxIcon.Error);
				        this.Close();
				        return;
			        }

			        if (locationUrl.Length < successUrl.Length || locationUrl.Substring(0, successUrl.Length) != successUrl)
			        {
				        this.Show();
				        return;
			        }
			        // все прошло ок
			        this.Hide();

			        string fullString = (string) locationUrl.Split('#').GetValue(1);
			        string[] paramSrc = fullString.Split('&');

			        Dictionary<string, string> sessionData = new Dictionary<string, string>();
			        for (int i = 0; i < paramSrc.Length; i++)
			        {
				        var curP = paramSrc[i].Split('=');
				        sessionData.Add(curP[0], curP[1]);
			        }

			        var api = VkapiLibrary.Instance;
			        api.AppId = Default.VKAppId;
			        api.Expire = Convert.ToInt32(sessionData["expires_in"]);
			        api.UserId = Convert.ToInt32(sessionData["user_id"]);
			        api.AccessTokien = (string) sessionData["access_token"];

			        JArray apiResult =
				        (JArray)
					        Vkapi.Instance.StartTaskSync(new ApiQuery("users.get",
						        new Dictionary<string, string> {{"uids", sessionData["user_id"]}}));
			        SerializationObject.User user = apiResult[0].ToObject<SerializationObject.User>();

			        ChangeDriverName();
			        notifyIcon1.Text = @"VKDrive: " + user.FirstName + @" " + user.LastName;

			        DokanInit.Start();
		        }
	        }
	        catch (Exception ex)
	        {
		        Log.Fatal("webBrowser1_DocumentCompleted exception", ex);
		        MessageBox.Show(ex.Message);
	        }
        }

        private void ToAuthVkPage()
        {
            string scope = "audio,friends,photos"; // ,photos,docs
            //Properties.Settings.Default.VKAppId
            webBrowser1.Url = new Uri("https://oauth.vk.com/authorize?client_id=" + Default.VKAppId +
                "&display=popup&response_type=token&scope="+scope+"&redirect_uri=http%3A%2F%2Foauth.vk.com%2Fblank.html");
        }



        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DokanInit.End();
            this.Close();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Точно выйти из аккаунта ВКонтакте?", "Внимание", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                _webBrowserLogoutWait = 1;
                webBrowser1.Url = new Uri("https://vk.com");
                //DokanInit.end();
            }
        }


        private void Browser_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Чет не срабатывает 
            //System.Environment.Exit(0);
            DokanInit.End();
        }
        

    }
}
