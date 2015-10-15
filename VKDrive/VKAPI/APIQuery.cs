using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.VKAPI
{
    class APIQuery
    {
        public string Method;
        public Dictionary<string, string> Param;
        public int Type;
        public JToken Responce = null;
        
        public APIQuery(string method)
        {
            construct(method, new Dictionary<string, string>(), VKAPILibrary.XML);
        }
        public APIQuery(string method, Dictionary<string, string> paramOld)
        {
            construct(method, paramOld, VKAPILibrary.XML);
        }

        public APIQuery(string method, Dictionary<string, string> paramOld, int type)
        {
            construct(method, paramOld, type);
        }

        private void construct(string method, Dictionary<string, string> param, int type)
        {
            this.Method = method;
            this.Param = param;
            this.Type = type;
        }

        private string jsonEncode(string val)
        {
            // simple json encode \ => \\  " => \"
            return "\"" + val.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        override public string ToString()
        {
            string res = "API." + Method + "({";

            foreach(KeyValuePair<string, string> kv in Param)
            {
                res += jsonEncode(kv.Key)+":"+ jsonEncode(kv.Value);
            }

            return res + "})";

        }
    }
}
