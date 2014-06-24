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
	}
}
