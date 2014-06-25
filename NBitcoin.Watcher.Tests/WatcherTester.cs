using NBitcoin.Watcher.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;

namespace NBitcoin.Watcher.Tests
{
	public class WatcherTester : IDisposable
	{
		string _Folder;


		public WatcherTester(string folder)
		{
			TestUtils.EnsureNew(folder);
			_Folder = folder;

			WatcherOptions watcher = new WatcherOptions();
			watcher.Port = 80;
			watcher.Path = "WatcherTests/" + folder;
			watcher.WatchDirectory = folder;
			_Client = new WatcherClient(watcher.CreateBaseAddress());
			_Server = watcher.OpenSelfHost().Result;
		}

		private readonly HttpSelfHostServer _Server;
		public HttpSelfHostServer Server
		{
			get
			{
				return _Server;
			}
		}

		private readonly WatcherClient _Client;
		public WatcherClient Client
		{
			get
			{
				return _Client;
			}
		}


		#region IDisposable Members

		public void Dispose()
		{
			_Server.Dispose();
		}

		#endregion
	}
}
