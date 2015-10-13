using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VKDrive.API
{
    class VKStorage
    {
        public static string get(string key){
            string xml = VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("storage.get", new Dictionary<string, string>() { { "key", key } }));
            XElement responce = XElement.Parse(xml);
            return responce.Value;
        }
        /// <summary>
        /// Помни %username% максимальнся длинна 4096 байт
        /// Если что вылетит эксепшен
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void set(string key, string value)
        {
            VKAPI.VKAPI.Instance.StartTaskSync(new VKAPI.APIQuery("storage.set", new Dictionary<string, string>() { { "key", key }, {"value", value} }));
        }

        public static void remove(string key, string value){
            string fullValue = get(key);
            if (fullValue.Length == 0)
                return;
            
            List<string> list = new List<string>(fullValue.Split('\n'));
            int i = list.IndexOf(value);
            if (i > -1)
            {
                list.RemoveAt(i);
                set(key, string.Join("\n", list));
            }
        }

        public static void join(string key, string value)
        {
            string fullValue = get(key);
            if (fullValue.Length == 0)
            {
                set(key, value);
                return;
            }
            string[] list = fullValue.Split('\n');

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == value)
                    return;
            }
            set(key, fullValue + "\n" + value);
        }

    }
}
