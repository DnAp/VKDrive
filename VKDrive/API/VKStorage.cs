using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VKDrive.API
{
    class VkStorage
    {
        public static string Get(string key){
            return VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("storage.get", new Dictionary<string, string>() { { "key", key } })).ToObject<string>();
        }
        /// <summary>
        /// Помни %username% максимальнся длинна 4096 байт
        /// Если что вылетит эксепшен
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(string key, string value)
        {
            VKAPI.Vkapi.Instance.StartTaskSync(new VKAPI.ApiQuery("storage.set", new Dictionary<string, string>() { { "key", key }, {"value", value} }));
        }

        public static void Remove(string key, string value){
            string fullValue = Get(key);
            if (fullValue.Length == 0)
                return;
            
            List<string> list = new List<string>(fullValue.Split('\n'));
            int i = list.IndexOf(value);
            if (i > -1)
            {
                list.RemoveAt(i);
                Set(key, string.Join("\n", list));
            }
        }

        public static void Join(string key, string value)
        {
            string fullValue = Get(key);
            if (fullValue.Length == 0)
            {
                Set(key, value);
                return;
            }
            string[] list = fullValue.Split('\n');

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == value)
                    return;
            }
            Set(key, fullValue + "\n" + value);
        }

    }
}
