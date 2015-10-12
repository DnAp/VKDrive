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
        public string Responce;

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
    }
}
