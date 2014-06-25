using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WATCHER_CLIENT
namespace NBitcoin.Watcher.Client
#else
namespace NBitcoin.Watcher.Contracts
#endif
{
	public class Watch
	{
		public static Watch Parse(string str)
		{
			return Parse<Watch>(str);
		}
		public static T Parse<T>(string str)
		{
			JsonTextReader reader = new JsonTextReader(new StringReader(str));
			return Serialization.CreateSerializer()
						.Deserialize<T>(reader);
		}

		static Random _Random = new Random();
		public Watch()
		{
			lock(_Random)
			{
				Name = "Watch-" + _Random.Next();
			}
			Start = DateTimeOffset.UtcNow;
		}
		public string Name
		{
			get;
			set;
		}
		public DateTimeOffset Start
		{
			get;
			set;
		}
		public override string ToString()
		{
			StringWriter writer = new StringWriter();
			Watch[] watch = new Watch[] { this }; //Force $type to serialize
			Serialization.CreateSerializer()
						 .Serialize(writer, watch);
			var result = writer.ToString();
			result = result.Substring(5); //Remove start [
			result = result.Substring(0, result.Length - 6) + "}"; //Remove end ]
			result = result.Replace("\r\n  ", "\r\n"); //Fix indent
			return result;
		}
	}

	[FriendlyName("PubKeyHash")]
	public class PubKeyHashWatch : Watch
	{
		public PubKeyHashWatch()
		{

		}
		public PubKeyHashWatch(string address)
		{
			Address = address;
		}
		public string Address
		{
			get;
			set;
		}
	}
}
