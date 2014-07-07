using NBitcoin.Protocol;
using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	public class WatchInstance
	{
		public WatchInstance(WatchDirectory directory)
		{
			_Directory = directory;
		}
		private readonly WatchDirectory _Directory;
		public WatchDirectory Directory
		{
			get
			{
				return _Directory;
			}
		}

		private ScanState _ScanState;

		public void Process(Chain mainChain, IndexedBlockStore index)
		{
			_ScanState.Process(mainChain, index);
		}


		public void Initialize(Chain mainChain)
		{
			var scanner = Directory.Configuration.CreateScanner();
			if(scanner == null)
				return;
			var chainFile = Path.Combine(Directory.Directory, "chain.dat");
			var accountFile = Path.Combine(Directory.Directory, "account.dat");
			var startHeightFile = Path.Combine(Directory.Directory, "startheight");

			int startHeight = 0;
			if(!File.Exists(startHeightFile))
			{
				var start =
					mainChain.ToEnumerable(true)
						 .FirstOrDefault(b => b.Header.BlockTime <= Directory.Configuration.Start);
				if(start == null)
					start = mainChain.Genesis;
				startHeight = start.Height;
				System.IO.File.WriteAllText(startHeightFile, start.Height.ToString());
			}
			else
			{
				startHeight = int.Parse(File.ReadAllText(startHeightFile));
			}

			_ScanState = new ScanState(scanner,
		new Chain(new StreamObjectStream<ChainChange>(File.Open(chainFile, FileMode.OpenOrCreate))),
		new Account(new StreamObjectStream<AccountEntry>(File.Open(accountFile, FileMode.OpenOrCreate))), startHeight);
		}

		public void Dispose()
		{
			if(_ScanState != null)
				_ScanState.Dispose();
		}
	}
}
