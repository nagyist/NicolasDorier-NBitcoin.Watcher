using NBitcoin.Protocol;
using NBitcoin.Watcher.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;

namespace NBitcoin.Watcher.Tests
{
	public class WatcherTester : IDisposable
	{
		internal string _Folder;


		public WatcherTester(string folder)
		{
			TestUtils.EnsureNew(folder);
			_Folder = folder;

			WatcherOptions watcher = new WatcherOptions();
			watcher.Port = 80;
			watcher.Path = "WatcherTests/" + folder;
			watcher.WatchDirectory = folder;

			watcher.TestNet = true;
			watcher.AutoConfig = true;
			if(!watcher.EnsureAutoConfigured())
				throw new InvalidOperationException("This test need bitcoinq.exe -server -testnet running");

			_Client = new WatcherClient(watcher.CreateBaseAddress());
			_Server = watcher.OpenSelfHost().Result;

			NodeServer server = new NodeServer(Network.TestNet);
			server.RegisterPeerTableRepository(PeerCache);
			_Watcher = watcher.CreateWatcher(server);
		}

		PeerTableRepository _PeerCache;
		public PeerTableRepository PeerCache
		{
			get
			{
				if(_PeerCache == null)
					_PeerCache = new SqLitePeerTableRepository("PeerCache");
				return _PeerCache;
			}
		}

		private readonly Watcher _Watcher;
		public Watcher Watcher
		{
			get
			{
				return _Watcher;
			}
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
			if(_Watcher != null)
				_Watcher.Dispose();
			if(_Server != null)
				_Server.Dispose();
			CachedChain.Dispose();
			CachedIndex.Dispose();
		}

		#endregion

		private CachedResult _CachedChain;
		public CachedResult CachedChain
		{
			get
			{
				if(_CachedChain == null)
				{
					_CachedChain = new CachedResult("chain.dat", this);
				}
				return _CachedChain;
			}
		}

		private CachedResult _CachedIndex;
		public CachedResult CachedIndex
		{
			get
			{
				if(_CachedIndex == null)
				{
					_CachedIndex = new CachedResult("blockindex", this);
				}
				return _CachedIndex;
			}
		}

		
	}

	public class CachedResult : IDisposable
	{
		WatcherTester tester;
		string file;
		public CachedResult(string file, WatcherTester tester)
		{
			this.tester = tester;
			this.file = file;
		}

		public void UpdateCache()
		{
			var chainFile = Path.Combine(tester._Folder, file);
			if(File.Exists(chainFile))
			{
				File.Copy(chainFile, Path.Combine(Directory.GetParent(tester._Folder).FullName, file), true);
			}
		}

		bool copied;
		public void Copy()
		{
			var parent = Directory.GetParent(tester._Folder);
			var chain = parent.GetFiles(file).FirstOrDefault();
			if(chain != null)
				chain.CopyTo(Path.Combine(tester._Folder, file));
			copied = true;
		}

		#region IDisposable Members

		public void Dispose()
		{
			if(copied)
			{
				UpdateCache();
			}
		}

		#endregion
	}
}
