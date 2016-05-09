using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static System.String;

namespace VKDrive.VKAPI
{
	internal class ApiQuery : IApiQuery
    {
        public string Method;
        public Dictionary<string, string> Param;
        
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
            Method = method;
            Param = param;
        }

        private static string JsonEncode(string val)
        {
            // simple json encode \ => \\  " => \"
            return "\"" + val.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        public override string ToString()
        {
            var param = new string[Param.Count];
            var i = 0;
            foreach (var kv in Param)
            {
                param[i] = JsonEncode(kv.Key)+":"+ JsonEncode(kv.Value);
                i++;
            }

            return "result.push(API." + Method + "({" + Join(",", param) + "}));";
        }
    }
}
