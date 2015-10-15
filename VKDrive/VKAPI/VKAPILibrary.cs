using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using log4net;

namespace VKDrive.VKAPI
{
    class VKAPILibrary
    {
        public const int JSON = 1;
        public const int XML = 2;

        public int Expire = 0;
        public int UserID;
        /// <summary>
        /// Идентификатор сессии
        /// </summary>
        public string SID = string.Empty;
        public string AccessTokien = string.Empty;
        public int AppID;
        private static ILog Log = LogManager.GetLogger("VKAPI");

        private sealed class SingletonCreator
        {
            private static readonly VKAPILibrary instance = new VKAPILibrary();
            public static VKAPILibrary Instance { get { return instance; } }
        }

        public static VKAPILibrary Instance
        {
            get { return SingletonCreator.Instance; }
        }

        private string MD5Hash(string Value)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(Value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }

        [Obsolete("Use APIQuery")]
        public string execute(string method)
        {
            return execute(method, new Dictionary<string, string>());
        }
        [Obsolete("Use APIQuery")]
        public string execute(string method, Dictionary<string, string> paramOld)
        {
            return execute(method, paramOld, VKAPILibrary.XML);
        }
        [Obsolete("Use APIQuery")]
        public string execute(string method, Dictionary<string, string> paramOld, int type)
        {
            return execute(new APIQuery(method, paramOld, type));
        }
        

        public string execute(APIQuery apiQuery)
        {
            Dictionary<string, string> param = new Dictionary<string, string>(apiQuery.Param);

            param.Add("access_token", this.AccessTokien);
            param.Add("lang", "ru");
            param.Add("v", "5.37");

            string url = "https://api.vk.com/method/" + apiQuery.Method;
            if (apiQuery.Type == VKAPILibrary.XML)
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
            HttpWebRequest WebReq;
            WebReq = (HttpWebRequest)WebRequest.Create(url);
            WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
            WebReq.Method = "POST";
            WebReq.ContentType = "application/x-www-form-urlencoded";
            //WebReq.Headers["Cookie"] = "remixlang=0; remixchk=5; remixsid=" + SID;

            WebReq.ContentLength = byteArray.Length;
            Stream dataStream = WebReq.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse webResult = WebReq.GetResponse();
            if (webResult == null)
            {
                Log.Error("VK API EXECUTE ERROR(request null)" + url);
                throw new Exception("VK API EXECUTE ERROR");
            }

            Stream stream = webResult.GetResponseStream();
            StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            
            if (result.Length > 50 && result.IndexOf("<error>", 0, 50) > 0)
            {
                int start = result.IndexOf("<error_code>") + 12;
                int exceptionCode = int.Parse(result.Substring(start, result.IndexOf("</error_code>", start) - start));

                start = result.IndexOf("<error_msg>") + 11;
                string exceptionText = result.Substring(start, result.IndexOf("</error_msg>", start) - start);
                // if exceptionCode == 5 // че-то с авторизацией, повторяем логин 
                //<error_code>15</error_code>
                //<error_msg>Access denied: group audio is disabled</error_msg>
                Log.Error("VK API EXECUTE ERROR: " + exceptionCode.ToString() + ":" + exceptionText);
                Exception e = new Exception("VK API EXECUTE ERROR: " + exceptionCode.ToString() + ":" + exceptionText);
                e.Data.Add("code", exceptionCode);
                e.Data.Add("msg", exceptionText);
                throw e;
            }
            return result;
        }



    }

}
