using NBitcoin.Protocol;
using NBitcoin.Watcher.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	public class Watcher : IDisposable
	{
		private readonly RPC.RPCClient _RPCClient;
		public RPC.RPCClient RPCClient
		{
			get
			{
				return _RPCClient;
			}
		}
		private readonly NodeServer _NodeServer;
		public NodeServer NodeServer
		{
			get
			{
				return _NodeServer;
			}
		}

		private readonly string _Directory;
		public string Directory
		{
			get
			{
				return _Directory;
			}
		}

		private Chain _Chain;
		public Chain Chain
		{
			get
			{
				return _Chain;
			}
		}

		private IndexedBlockStore _IndexedBlockStore;
		public IndexedBlockStore IndexedBlockStore
		{
			get
			{
				return _IndexedBlockStore;
			}
		}
		BlockStore _Store;

		public Watcher(RPC.RPCClient client,
						NodeServer nodeServer,
						BlockStore blockStore,
						string directory)
		{
			if(client == null)
				throw new ArgumentNullException("client");
			if(directory == null)
				throw new ArgumentNullException("directory");
			if(blockStore == null)
				throw new ArgumentNullException("blockStore");
			this._RPCClient = client;
			this._Directory = directory;
			this._Store = blockStore;
			_NodeServer = nodeServer;

			for(int i = 0 ; i < _Pools.Length ; i++)
			{
				_Pools[i] = new WatchInstancePool(new MessageProducer<WatchMessage>());
			}
		}

		WatchInstancePool[] _Pools = new WatchInstancePool[5];

		public void Load()
		{
			var chainFile = Path.Combine(_Directory, "chain.dat");
			if(!File.Exists(chainFile))
			{
				_Chain = new Chain(_RPCClient.Network, new StreamObjectStream<ChainChange>(File.Open(chainFile, FileMode.Create)));
			}
			else
			{
				_Chain = new Chain(new StreamObjectStream<ChainChange>(File.Open(chainFile, FileMode.Open)));
			}

			_IndexedBlockStore = new NBitcoin.IndexedBlockStore(
										new SQLiteNoSqlRepository(Path.Combine(Directory, "blockindex")),
										_Store);
		}


		public Task SendMessage(string watchName, Action<WatchInstance> act)
		{
			return Task.WhenAll(_Pools.Select(p => p.SendMessage(watchName, act)).ToArray());
		}


		public void UpdateChain()
		{
			uint256 best = RPCClient.GetBestBlockHash();
			while(true)
			{
				if(best == Chain.Tip.HashBlock)
					return;
				NodeServer.BuildChain(Chain);
				best = RPCClient.GetBestBlockHash();
			}
		}

		public void ReIndex()
		{
			IndexedBlockStore.ReIndex();
		}
		public void Dispose()
		{
			foreach(var pool in _Pools)
				pool.Dispose();
			if(Chain != null)
				Chain.Changes.Dispose();
			if(NodeServer != null)
				NodeServer.Dispose();
		}

		ConcurrentDictionary<string, WatchInstance> _CurrentDirectories = new ConcurrentDictionary<string, WatchInstance>();


		public void FetchWatches()
		{
			foreach(var watch in WatchDirectory.ListWatchDirectories(Directory))
			{
				AddWatch(watch);
			}
		}

		private Task AddWatch(WatchDirectory watch)
		{
			WatchInstance instance;
			if(!_CurrentDirectories.TryGetValue(watch.Configuration.Name, out instance))
			{
				instance = new WatchInstance(watch);
				var adding = _Pools[Math.Abs(watch.Configuration.Name.GetHashCode() % _Pools.Length)].AddInstance(instance, Chain);
				_CurrentDirectories.AddOrUpdate(watch.Configuration.Name, instance, (k, old) => old);
				return adding;
			}

			TaskCompletionSource<int> source = new TaskCompletionSource<int>();
			source.SetResult(0);
			return source.Task;
		}

		public Task ProcessAll()
		{
			return Task.WhenAll(_Pools.Select(p => p.SendMessage(i =>
			{
				i.Process(Chain, IndexedBlockStore);
			})).ToArray());
		}

		public Task DeleteInstance(string watchName)
		{
			return Task.WhenAll(_Pools.Select(pool => pool.SendMessage(watchName, instance =>
			{
				try
				{
					instance.Dispose();
					instance.Directory.Delete();
				}
				finally
				{
					pool.RemoveInstance(watchName);
				}
			})).ToArray());

		}

		public Task AddWatch(Watch watch)
		{
			var directory = WatchDirectory.GetOrCreateWatchDirectory(Directory, watch);
			return AddWatch(directory);
		}
	}
}
