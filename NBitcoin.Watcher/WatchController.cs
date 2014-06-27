using NBitcoin.Watcher.Contracts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace NBitcoin.Watcher
{
	public class WatchController : ApiController
	{
		string _Directory;
		public WatchController(string directory)
		{
			if(!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			_Directory = directory;
		}
		[Route("ping")]
		[HttpPost]
		public string Ping([FromBody] string content)
		{
			return content == "ping" ? "pong" : "Who are you ?";
		}

		[Route("watches/add")]
		[HttpPost]
		public void AddWatches([FromBody] List<Watch> watches)
		{
			foreach(var watch in watches)
			{
				AddWatch(watch);
			}
		}

		private void AddWatch(Watch watch)
		{
			WatchDirectory.GetOrCreateWatchDirectory(_Directory, watch);
		}



		[Route("watches/list")]
		[HttpGet]
		public Watch[] ListWatches()
		{
			return WatchDirectory.ListWatchDirectories(_Directory).Select(c => c.Configuration).ToArray();
		}

		[Route("watches/get")]
		[HttpPost]
		public Watch[] GetWatches(string[] names)
		{
			List<Watch> result = new List<Watch>();
			foreach(var n in names)
			{
				var watchDir = WatchDirectory.GetWatchDirectory(_Directory, n);
				if(watchDir != null && !watchDir.MarkedDeleted)
					result.Add(watchDir.Configuration);
			}
			return result.ToArray();
		}

		[Route("watches/delete")]
		[HttpPost]
		public void DeleteWatches(string[] names)
		{
			foreach(var name in names)
			{
				var watch = WatchDirectory.GetWatchDirectory(_Directory, name);
				if(watch != null)
					watch.MarkDelete();
			}
		}
	}
}
