using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Watcher
{
	class WatcherTrace
	{
		internal static void Started()
		{
			Console.WriteLine("Server started");
		}

		internal static void Opening(Uri address)
		{
			Console.WriteLine("Opening at " + address.AbsoluteUri);
		}

		internal static void AutoConfigDetected(string config)
		{
			Console.WriteLine("Auto config detected :\r\n" + config);
		}

		internal static void AutoConfigNotDetected()
		{
			Console.WriteLine("No running RPC bitcoin service detected (bitcoinq.exe -server [-testnet])");
		}
	}
}
