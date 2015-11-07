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
    struct VKResponceError
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        [JsonProperty("error_msg")]
        public string ErrorMsg { get; set; }
    }


    class VKAPI
    {
        const int TIMEOUT = 1000 / 3;
        ConcurrentQueue<APIQuery> concurrentQueue = new ConcurrentQueue<APIQuery>();
        bool isAlive = true;
        private static VKAPI instance;

        private AutoResetEvent resetEvent = new AutoResetEvent(false);
        
        private static ILog Log = LogManager.GetLogger("VKAPI");

        private VKAPI()
        {
            Thread workerThread = new Thread(this.DoWork);
            workerThread.Start();
        }

        public static VKAPI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VKAPI();
                }
                return instance;
            }
        }

        public JToken StartTaskSync(APIQuery query)
        {
            concurrentQueue.Enqueue(query);
            byte i = 0;
            do
            {
                resetEvent.WaitOne();
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
                VKResponceError error = errorJs.ToObject<VKResponceError>();
                Log.Error("VK API EXECUTE ERROR: " + error.ErrorCode + ":" + error.ErrorMsg);
                Exception e = new Exception("VK API EXECUTE ERROR: " + error.ErrorCode + ":" + error.ErrorMsg);
                e.Data.Add("code", error.ErrorCode);
                e.Data.Add("msg", error.ErrorMsg);
                throw e;
            }
        }

        public void DoWork()
        {
            while (isAlive)
            {
                Thread.Sleep(TIMEOUT);
                APIQuery query;
                List<APIQuery> qList = new List<APIQuery>();
                String executeQuery = "return [";
                while (concurrentQueue.TryDequeue(out query))
                {
                    qList.Add(query);
                    executeQuery += query.ToString() + ",";
                }
                if(qList.Count == 0)
                {
                    resetEvent.Set();
                    continue;
                }
                executeQuery += "0];";
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("code", executeQuery);
                var executeAPIQuery = new APIQuery("execute", param, VKAPILibrary.JSON);

                string jsonSrc = VKAPILibrary.Instance.execute(executeAPIQuery);
                //Console.WriteLine(jsonSrc);
                JObject jObject = JObject.Parse(jsonSrc);
                
                TryThrowException(jObject.GetValue("error"));

                JToken result = jObject.GetValue("response");
                if (result.GetType() != typeof(JArray) || ((JArray)result).Count < qList.Count)
                {
                    Log.Error("VK API EXECUTE ERROR: unknown responce :" + jObject.ToString());
                    throw new Exception("VK API EXECUTE ERROR: unknown responce :"+ jObject.ToString());
                }
                int i = 0;
                int errorI = 0;
                foreach (JToken res in (JArray)result)
                {
                    if (i < qList.Count)
                    {
                        if (res.Type == JTokenType.Boolean && !(bool)res)
                        {
                            JArray executeErrors = (JArray)jObject.GetValue("execute_errors");
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

                resetEvent.Set();
            }
        }

        public void Stop()
        {
            isAlive = false;
        }
    }
}
                                                                                                                                        