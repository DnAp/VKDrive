using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VKDrive.Utils
{
	public class UnixTimestampJsonConverter : JsonConverter
	{
		public override object ReadJson(
			JsonReader reader,
			Type objectType,
			object existingValue,
			JsonSerializer serializer)
		{
			var ts = serializer.Deserialize<long>(reader);

			var unixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return unixTimeStamp.AddSeconds(ts);
		}

		public override bool CanConvert(Type type)
		{
			return typeof(DateTime).IsAssignableFrom(type);
		}

		public override void WriteJson(
			JsonWriter writer,
			object value,
			JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override bool CanRead
		{
			get { return true; }
		}
	}
}
