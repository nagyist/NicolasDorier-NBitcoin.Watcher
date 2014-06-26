using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
		}

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
			if(Chain != null)
				Chain.Changes.Dispose();
			if(NodeServer != null)
				NodeServer.Dispose();
		}
	}
}
