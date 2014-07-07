using NBitcoin.Protocol;
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
		public static IEnumerable<WatchDirectory> ListWatchDirectories(string watchesDirectory)
		{
			foreach(var directory in System.IO.Directory.EnumerateDirectories(watchesDirectory))
			{
				var dir = GetWatchDirectory(directory);
				if(dir != null)
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

	}
}
