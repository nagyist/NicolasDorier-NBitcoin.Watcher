using NBitcoin.Scanning;
using NBitcoin.Watcher.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	public class WatchDirectory
	{
		public static IEnumerable<WatchDirectory> ListWatchDirectories(string watchesDirectory, bool includedMarkedDeleted = false)
		{
			foreach(var directory in System.IO.Directory.EnumerateDirectories(watchesDirectory))
			{
				var dir = GetWatchDirectory(directory);
				if(dir != null && (includedMarkedDeleted || !dir.MarkedDeleted))
					yield return dir;
			}
		}

		public static WatchDirectory GetWatchDirectory(string directory, string watchName)
		{
			return GetWatchDirectory(Path.Combine(directory, SanitizeDirectoryName(watchName)));
		}
		public static WatchDirectory GetWatchDirectory(string watchDir)
		{
			if(!System.IO.Directory.Exists(watchDir))
				return null;
			var watchConf = Path.Combine(watchDir, "watch.conf");
			if(!File.Exists(watchConf))
				return null;
			return new WatchDirectory(watchConf);

		}

		private readonly string _Directory;
		public string Directory
		{
			get
			{
				return _Directory;
			}
		}
		private readonly string _ConfigurationFile;
		public string ConfigurationFile
		{
			get
			{
				return _ConfigurationFile;
			}
		}
		public WatchDirectory(string watchConfig)
		{
			if(watchConfig == null)
				throw new ArgumentNullException("watchConfig");
			_Directory = Path.GetDirectoryName(watchConfig);
			_ConfigurationFile = watchConfig;
			Configuration = Watch.Parse(File.ReadAllText(watchConfig));
		}
		public Watch Configuration
		{
			get;
			set;
		}

		public static WatchDirectory GetOrCreateWatchDirectory(string directory, Watch watch)
		{
			var watchDir = Path.Combine(directory, SanitizeDirectoryName(watch.Name));
			var dir = GetWatchDirectory(watchDir);
			if(dir != null)
				return dir;
			System.IO.Directory.CreateDirectory(watchDir);
			var fileName = Path.Combine(watchDir, "watch.conf");
			File.WriteAllText(fileName, watch.ToString());
			return new WatchDirectory(fileName);
		}

		private ScanState _ScanState;

		private static string SanitizeDirectoryName(string directory)
		{
			var invalidChars = Path.GetInvalidFileNameChars().Concat(new char[] { '.', '/', '\\' }).ToArray();
			foreach(var c in invalidChars)
			{
				directory = directory.Replace(c.ToString(), "");
			}
			return directory;
		}

		public void Delete()
		{
			System.IO.Directory.Delete(Directory, true);
		}

		public void Process(Chain mainChain, IndexedBlockStore index)
		{
			if(MarkedDeleted)
			{
				if(_ScanState != null)
				{
					_ScanState.Dispose();
				}
				Delete();
				return;
			}

			if(_ScanState == null)
			{
				var scanner = Configuration.CreateScanner();
				if(scanner == null)
					return;
				var chainFile = Path.Combine(Directory, "chain.dat");
				var accountFile = Path.Combine(Directory, "account.dat");
				var startHeightFile = Path.Combine(Directory, "startheight");

				int startHeight = 0;
				if(!File.Exists(startHeightFile))
				{
					var start =
						mainChain.ToEnumerable(true)
							 .FirstOrDefault(b => b.Header.BlockTime <= Configuration.Start);
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

			_ScanState.Process(mainChain, index);
		}

		public void MarkDelete()
		{
			MarkedDeleted = true;
		}

		void WriteDeferred(string content)
		{
			var deferred = Path.Combine(Directory, "deferred");
			File.WriteAllText(deferred, content);
		}
		string ReadDeferred()
		{
			var deferred = Path.Combine(Directory, "deferred");
			if(!File.Exists(deferred))
				return null;
			return File.ReadAllText(deferred);
		}

		public bool MarkedDeleted
		{
			get
			{
				return ReadDeferred() == "delete";
			}
			private set
			{
				WriteDeferred("delete");
			}
		}
	}
}
