                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using System.IO;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using System.Net;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using System.Text;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using System.Threading;

namespace VKDrive.VKAPI
{
    [JsonObject(MemberSerialization.OptIn)]
    struct VkResponceError
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        [JsonProperty("error_msg")]
        public string ErrorMsg { get; set; }
    }


	internal class Vkapi : IDisposable
    {
	    private const int Timeout = 1000 / 3;
		private const int MaxQueryInExecute = 25;
	    private readonly ConcurrentQueue<IApiQuery> _concurrentQueue = new ConcurrentQueue<IApiQuery>();
        private bool _isAlive = true;
        private static Vkapi _instance;

        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);
        
        private static readonly ILog Log = LogManager.GetLogger("VKAPI");

        private Vkapi()
        {
            new Thread(this.DoWork).Start();
        }

        public static Vkapi Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Vkapi();
                }
                return _instance;
            }
        }

        public JToken StartTaskSync(IApiQuery query)
        {
            _concurrentQueue.Enqueue(query);
            byte i = 0;
            do
            {
                _resetEvent.WaitOne();
                if(i==3)
                {
                    Log.Error("VK API EXECUTE ERROR: long wait");
                    throw new Exception("VK API EXECUTE ERROR: long wait");
                }
                i++;
            } while (query.Responce == null);
	        JToken error;
	        if (query.Responce.GetType() == typeof(JObject) && ((JObject)query.Responce).TryGetValue("error", out error))
            {
                TryThrowException(error);
            }
            

            return query.Responce;
        }

        private void TryThrowException(JToken errorJs)
        {
            if (errorJs != null)
            {
                var error = errorJs.ToObject<VkResponceError>();
                Log.Error("VK API EXECUTE ERROR: " + error.ErrorCode + ":" + error.ErrorMsg);
                var e = new Exception("VK API EXECUTE ERROR: " + error.ErrorCode + ":" + error.ErrorMsg);
                e.Data.Add("code", error.ErrorCode);
                e.Data.Add("msg", error.ErrorMsg);
                throw e;
            }
        }

        public void DoWork()
        {
            while (_isAlive)
            {
                Thread.Sleep(Timeout);
				IApiQuery query;
                var qList = new List<IApiQuery>();
                var executeQuery = "var result = [];";
	            var i = 0;
                while (i<25 && _concurrentQueue.TryDequeue(out query))
                {
	                qList.Add(query);
                    executeQuery += query.ToString();
					i++;
				}
				if(qList.Count == 0)
                {
                    _resetEvent.Set();
                    continue;
                }

				Log.Info("Query execute "+i+"/"+MaxQueryInExecute);

				executeQuery += "return result;";
	            var param = new Dictionary<string, string> {{"code", executeQuery}};
				var executeApiQuery = new ApiQuery("execute", param, VkapiLibrary.Json);

				var jsonSrc = Execute(executeApiQuery);

				//Console.WriteLine(jsonSrc);
				Log.Debug("Api multiquery: " + jsonSrc);
                var jObject = JObject.Parse(jsonSrc);
	            JToken error;
	            if (jObject.TryGetValue("error", out error))
	            {
		            TryThrowException(jObject.GetValue("error"));
	            }
	            var result = jObject.GetValue("response");
                if (result.GetType() != typeof(JArray) || ((JArray)result).Count < qList.Count)
                {
                    Log.Error("VK API EXECUTE ERROR: unknown responce :" + jObject);
                    throw new Exception("VK API EXECUTE ERROR: unknown responce :"+ jObject);
                }
                i = 0;
                var errorI = 0;
                foreach (var res in (JArray)result)
                {
                    if (i < qList.Count)
                    {
                        if (res.Type == JTokenType.Boolean && !(bool)res)
                        {
                            var executeErrors = (JArray)jObject.GetValue("execute_errors");
                            qList[i].Responce = executeErrors[errorI];
                            errorI++;
                        }
                        else
                        {
                            qList[i].Responce = res;
                        }
                        i++;
                    }
                }

                _resetEvent.Set();
            }
        }

		private static string Execute(ApiQuery apiQuery)
		{
			var param = new Dictionary<string, string>(apiQuery.Param)
			{
				{"access_token", VkapiLibrary.Instance.AccessTokien},
				{"lang", "ru"},
				{"v", "5.37"}
			};


			var url = "https://api.vk.com/method/" + apiQuery.Method;
			var postData = "";
			foreach (var val in param)
			{
				postData += val.Key + "=" + Uri.EscapeDataString(val.Value) + "&";
			}
			Log.Debug("API Request " + url + " " + postData);
			var byteArray = Encoding.UTF8.GetBytes(postData);

			// todo keep-alive
			var webReq = (HttpWebRequest)WebRequest.Create(url);
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
			return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
		}

		public void Stop()
        {
            _isAlive = false;
        }

        public void Dispose()
        {
            _resetEvent.Dispose();
        }
    }
}
                                                                                                                                        