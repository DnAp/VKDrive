using Newtonsoft.Json.Linq;

namespace VKDrive.VKAPI
{
	internal abstract class IApiQuery
	{
		public JToken Responce = null;
		public abstract override string ToString();
	}

}