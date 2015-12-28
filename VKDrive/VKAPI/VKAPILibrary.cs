using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using log4net;

namespace VKDrive.VKAPI
{
    class VkapiLibrary
    {
        public const int Json = 1;
        public const int Xml = 2;

        public int Expire = 0;
        public int UserId;
        /// <summary>
        /// Идентификатор сессии
        /// </summary>
        public string Sid = string.Empty;
        public string AccessTokien = string.Empty;
        public int AppId;
        private static readonly ILog Log = LogManager.GetLogger("VKAPI");

        private sealed class SingletonCreator
        {
            private static readonly VkapiLibrary instance = new VkapiLibrary();
            public static VkapiLibrary Instance { get { return instance; } }
        }

        public static VkapiLibrary Instance
        {
            get { return SingletonCreator.Instance; }
        }

        private string Md5Hash(string value)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }

        [Obsolete("Use APIQuery")]
        public string Execute(string method)
        {
            return Execute(method, new Dictionary<string, string>());
        }
        [Obsolete("Use APIQuery")]
        public string Execute(string method, Dictionary<string, string> paramOld)
        {
            return Execute(method, paramOld, VkapiLibrary.Xml);
        }
        [Obsolete("Use APIQuery")]
        public string Execute(string method, Dictionary<string, string> paramOld, int type)
        {
            return Execute(new ApiQuery(method, paramOld, type));
        }
        

        public string Execute(ApiQuery apiQuery)
        {
            Dictionary<string, string> param = new Dictionary<string, string>(apiQuery.Param);

            param.Add("access_token", this.AccessTokien);
            param.Add("lang", "ru");
            param.Add("v", "5.37");

            string url = "https://api.vk.com/method/" + apiQuery.Method;
            if (apiQuery.Type == VkapiLibrary.Xml)
            {
                url += ".xml";
            }
            string postData = "";
            foreach (KeyValuePair<string, string> val in param)
            {
                postData += val.Key + "=" + System.Uri.EscapeDataString(val.Value) + "&";
            }
            Log.Debug("API Request "+ url + " "+ postData);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            /// todo keep-alive
            HttpWebRequest webReq;
            webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Timeout = Properties.Settings.Default.Timeout * 1000;
            webReq.Method = "POST";
            webReq.ContentType = "application/x-www-form-urlencoded";
            //WebReq.Headers["Cookie"] = "remixlang=0; remixchk=5; remixsid=" + SID;

            webReq.ContentLength = byteArray.Length;
            Stream dataStream = webReq.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            var stream = webReq.GetResponse().GetResponseStream();
			if (stream == null)
			{
				Log.Error("VK API EXECUTE ERROR(response null)" + url);
				throw new Exception("VK API EXECUTE ERROR");
			}
			string result = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            
            if (result.Length > 50 && result.IndexOf("<error>", 0, 50) > 0)
            {
                int start = result.IndexOf("<error_code>") + 12;
                int exceptionCode = int.Parse(result.Substring(start, result.IndexOf("</error_code>", start) - start));

                start = result.IndexOf("<error_msg>") + 11;
                string exceptionText = result.Substring(start, result.IndexOf("</error_msg>", start) - start);
                // if exceptionCode == 5 // че-то с авторизацией, повторяем логин 
                //<error_code>15</error_code>
                //<error_msg>Access denied: group audio is disabled</error_msg>
                Log.Error("VK API EXECUTE ERROR: " + exceptionCode + ":" + exceptionText);
                Exception e = new Exception("VK API EXECUTE ERROR: " + exceptionCode + ":" + exceptionText);
                e.Data.Add("code", exceptionCode);
                e.Data.Add("msg", exceptionText);
                throw e;
            }
            return result;
        }



    }

}
