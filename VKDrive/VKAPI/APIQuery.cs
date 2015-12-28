using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.VKAPI
{
    class ApiQuery
    {
        public string Method;
        public Dictionary<string, string> Param;
        public int Type;
        public JToken Responce = null;
        
        public ApiQuery(string method)
        {
            Construct(method, new Dictionary<string, string>(), VkapiLibrary.Xml);
        }
        public ApiQuery(string method, Dictionary<string, string> paramOld)
        {
            Construct(method, paramOld, VkapiLibrary.Xml);
        }

        public ApiQuery(string method, Dictionary<string, string> paramOld, int type)
        {
            Construct(method, paramOld, type);
        }

        private void Construct(string method, Dictionary<string, string> param, int type)
        {
            this.Method = method;
            this.Param = param;
            this.Type = type;
        }

        private string JsonEncode(string val)
        {
            // simple json encode \ => \\  " => \"
            return "\"" + val.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        override public string ToString()
        {
            string[] param = new string[Param.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> kv in Param)
            {
                param[i] = JsonEncode(kv.Key)+":"+ JsonEncode(kv.Value);
                i++;
            }

            return "API." + Method + "({" + String.Join(",", param) + "})";

        }
    }
}
