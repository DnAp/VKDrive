                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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


    class Vkapi : IDisposable
    {
        const int Timeout = 1000 / 3;
        ConcurrentQueue<ApiQuery> _concurrentQueue = new ConcurrentQueue<ApiQuery>();
        bool _isAlive = true;
        private static Vkapi _instance;

        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        
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

        public JToken StartTaskSync(ApiQuery query)
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
            if (query.Responce.GetType() == typeof(JObject))
            {
                TryThrowException(((JObject)query.Responce).GetValue("error"));
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
                ApiQuery query;
                var qList = new List<ApiQuery>();
                var executeQuery = "return [";
                while (_concurrentQueue.TryDequeue(out query))
                {
                    qList.Add(query);
                    executeQuery += query + ",";
                }
                if(qList.Count == 0)
                {
                    _resetEvent.Set();
                    continue;
                }
                executeQuery += "0];";
	            var param = new Dictionary<string, string> {{"code", executeQuery}};
	            var executeApiQuery = new ApiQuery("execute", param, VkapiLibrary.Json);

                var jsonSrc = VkapiLibrary.Instance.Execute(executeApiQuery);
                //Console.WriteLine(jsonSrc);
                var jObject = JObject.Parse(jsonSrc);
                
                TryThrowException(jObject.GetValue("error"));

                var result = jObject.GetValue("response");
                if (result.GetType() != typeof(JArray) || ((JArray)result).Count < qList.Count)
                {
                    Log.Error("VK API EXECUTE ERROR: unknown responce :" + jObject);
                    throw new Exception("VK API EXECUTE ERROR: unknown responce :"+ jObject);
                }
                var i = 0;
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
                                                                                                                                        