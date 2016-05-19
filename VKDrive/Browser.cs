using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VKDrive.Properties;
using VKDrive.Utils;
using VKDrive.VKAPI;
using static VKDrive.Properties.Settings;

namespace VKDrive
{
    public partial class Browser : Form
    {
        int _webBrowserLogoutWait = 0;
	    private bool _hasUpdate = false;

		public const int WM_QUERYENDSESSION = 0x0011;
		public const int WM_ENDSESSION = 0x0016;
		public const int WM_SYSCOMMAND = 0x0112;

		public static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Browser()
        {
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
            Visible = true;
            IsVisibilityChangeAllowed = true;
	        
			DokanInit.End();
			
			ChangeDriverName();

			ToAuthVkPage();
        }

		private void Browser_Load(object sender, EventArgs e)
		{
			Hide();
			SetVisibleCore(false);

			Default.FirstStart = false;
			Default.Save();
			new Task(delegate
			{
				if (CheckUpdate.HasNewVersion())
				{
					_hasUpdate = true;
					var timer = new System.Timers.Timer
					{
						Interval = 3*59*60000, // 2h 57min
						Enabled = true
					};
					timer.Elapsed += (o, args) => ShowUpgradeBallonTip();
					ShowUpgradeBallonTip();
				}
			}).Start();
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_QUERYENDSESSION || m.Msg == WM_ENDSESSION || m.Msg == WM_SYSCOMMAND)
			{
				DokanInit.End();
			}
			base.WndProc(ref m);
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

	    private void ShowUpgradeBallonTip()
	    {
			notifyIcon1.BalloonTipTitle = Resources.NotifyBallonTipUpgradeTitle;
			notifyIcon1.BalloonTipText = Resources.NotifyBallonTipUpgradeText;
			notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
			notifyIcon1.ShowBalloonTip(30000);
		}

        bool IsVisibilityChangeAllowed { get; set; }

        protected override void SetVisibleCore(bool value)
        {
            if (IsVisibilityChangeAllowed)
            {
				base.SetVisibleCore(value);
            }
        }  

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
	        if (_hasUpdate)
	        {
		        CheckUpdate.Upgrade();
	        }
			else if (DokanInit.IsWorking())
            {
                System.Diagnostics.Process.Start("explorer", Default.MountPoint + @":\\");
            }
            else
            {
                IsVisibilityChangeAllowed = true;
                Show();
				WindowState = FormWindowState.Normal;
				Focus();
            }
            
        }
        


        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
	        try
	        {
		        IsVisibilityChangeAllowed = true;

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
			        Show();
					WindowState = FormWindowState.Normal;
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
			        const string successUrl = "https://oauth.vk.com/blank.html";
			        const string blockedUrl = "https://oauth.vk.com/login?act=blocked";

			        if (locationUrl.Length >= blockedUrl.Length && locationUrl.Substring(0, blockedUrl.Length) == blockedUrl)
			        {
				        MessageBox.Show(Resources.Browser_webBrowser1_DocumentCompleted_Banned, Resources.Message_Error,
					        MessageBoxButtons.OK, MessageBoxIcon.Error);
				        Close();
				        return;
			        }

			        if (locationUrl.Length < successUrl.Length || locationUrl.Substring(0, successUrl.Length) != successUrl)
			        {
				        Show();
						WindowState = FormWindowState.Normal;
						return;
			        }
			        // все прошло ок
			        Hide();

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

					var apiResult = (JArray) Vkapi.Instance.StartTaskSync(new ApiQuery("users.get", new Dictionary<string, string> {{"uids", sessionData["user_id"]}}));
			        var user = apiResult[0].ToObject<SerializationObject.User>();

			        ChangeDriverName();
			        notifyIcon1.Text = @"VKDrive: " + user.FirstName + @" " + user.LastName;
#if !DEBUG
					SendStats();
#endif
					DokanInit.Start();
		        }
	        }
	        catch (Exception ex)
	        {
		        Log.Fatal("webBrowser1_DocumentCompleted exception", ex);
		        MessageBox.Show(ex.Message);
	        }
        }

	    private void SendStats()
	    {
			var timer = new Timer
			{
				Interval = 24 * 60 * 60000, // 24h
				Enabled = true
			};
			timer.Tick += (o, args) => Vkapi.Instance.StartTaskAsync(new ApiQuery("stats.trackVisitor"));
			Vkapi.Instance.StartTaskAsync(new ApiQuery("stats.trackVisitor"));
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
            Close();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Точно выйти из аккаунта ВКонтакте?", "Внимание", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      