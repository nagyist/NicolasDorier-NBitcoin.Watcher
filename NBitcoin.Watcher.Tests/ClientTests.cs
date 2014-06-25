using NBitcoin.Watcher.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NBitcoin.Watcher.Tests
{
	[TestFixture]
	public class ClientTests
	{
		[Test]
		public async Task CanPing()
		{
			using(var tester = CreateTester())
			{
				await tester.Client.Ping();
				await tester.Client.Ping();
			}
		}


		[Test]
		public void CanParseAndSerializeWatch()
		{
			var watch = new PubKeyHashWatch();
			var watch2 = Watch.Parse(watch.ToString());
			var watch3 = Watch.Parse<PubKeyHashWatch>(watch.ToString());
			Assert.True(watch2 is PubKeyHashWatch);
			Assert.AreEqual(watch.ToString(), watch2.ToString());
			Assert.AreEqual(watch.ToString(), watch3.ToString());
		}
		[Test]
		public async Task CanCRUDWatches()
		{
			using(var tester = CreateTester())
			{
				var k1 = new Key().PubKey.GetAddress(Network.Main).ToString();
				var k2 = new Key().PubKey.GetAddress(Network.Main).ToString();
				await tester.Client.AddWatches(new[] { k1, k2 }.Select(k => new PubKeyHashWatch(k)));

				var actualWatches = (await tester.Client.ListWatches()).OfType<PubKeyHashWatch>().ToArray();

				var w1 = actualWatches.FirstOrDefault(o => o.Address == k1);
				var w2 = actualWatches.FirstOrDefault(o => o.Address == k2);
				Assert.True(w1 != null);
				Assert.True(w2 != null);

				await tester.Client.DeleteWatches(new string[] { w1.Name });

				actualWatches = (await tester.Client.ListWatches()).OfType<PubKeyHashWatch>().ToArray();

				Assert.False(actualWatches.Any(o => o.Address == k1));
				Assert.True(actualWatches.Any(o => o.Address == k2));

				Assert.Null(await tester.Client.GetWatch(w1.Name));
				Assert.NotNull(await tester.Client.GetWatch(w2.Name));
			}
		}

		private WatcherTester CreateTester([CallerMemberName]string folder = null)
		{
			return new WatcherTester(folder);
		}
	}
}
