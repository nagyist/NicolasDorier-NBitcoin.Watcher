using NBitcoin.Protocol;
using NBitcoin.Watcher.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		[Category("UnitTests")]
		public void CanUpdateWatchPubKeyHashWatch()
		{
			CanUpdateWatch(new PubKeyHashWatch("mwdJkHRNJi1fEwHBx6ikWFFuo2rLBdri2h")
			{
				Start = DateTimeOffset.ParseExact("2014-05-19 23:04:53",
												  "yyyy-MM-dd HH:mm:ss",
												  CultureInfo.InvariantCulture,
												  DateTimeStyles.AssumeUniversal)
			});
		}

		[Test]
		[Category("UnitTests")]
		public void CanUpdateWatchStealthWatch()
		{
			CanUpdateWatch(new StealthAddressWatch()
			{
				Start = DateTimeOffset.ParseExact("2014-05-26 00:04:53",
												  "yyyy-MM-dd HH:mm:ss",
												  CultureInfo.InvariantCulture,
												  DateTimeStyles.AssumeUniversal),
				Address = "waPYjXyrTrvXjZHmMGdqs9YTegpRDpx97H5G3xqLehkgyrrZKsxGCmnwKexpZjXTCskUWwYywdUvrZK7L2vejeVZSYHVns61gm8VfU",
				ScanKey = "cc411aab02edcd3bccf484a9ba5280d4a774e6f81eac8ebec9cb1c2e8f73020a"
			});
		}


		private void CanUpdateWatch(Watch watch, [CallerMemberName]string folder = null)
		{
			using(var tester = CreateTester(folder))
			{
				tester.CachedChain.Copy();
				tester.CachedIndex.Copy();
				tester.Watcher.Load();
				tester.Watcher.UpdateChain();
				tester.Watcher.ReIndex();
				tester.Client.AddWatches(new[] 
				{ 
					watch
				}).Wait();
				tester.Watcher.FetchWatches();
				tester.Watcher.ProcessAll().Wait();
			}
		}

		[Test]
		[Category("Benchmark")]
		public void WatchCanSynchronize()
		{
			using(var tester = CreateTester())
			{
				tester.Watcher.Load();
				tester.Watcher.UpdateChain();
				tester.Watcher.ReIndex();
			}
		}

		[Test]
		[Category("UnitTests")]
		public async Task CanPing()
		{
			using(var tester = CreateTester())
			{
				await tester.Client.Ping();
				await tester.Client.Ping();
			}
		}


		[Test]
		[Category("UnitTests")]
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
		[Category("UnitTests")]
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


		[Test]
		[Category("Stress")]
		public void StressWatcher()
		{
			List<Task> tasks = new List<Task>();
			for(int i = 0 ; i < 5 ; i++)
			{
				var locali = i;
				var task = Task.Run(() =>
				{
					CanUpdateWatch(new StealthAddressWatch()
						{
							Start = DateTimeOffset.ParseExact("2014-05-26 00:04:53",
															  "yyyy-MM-dd HH:mm:ss",
															  CultureInfo.InvariantCulture,
															  DateTimeStyles.AssumeUniversal),
							Address = "waPYjXyrTrvXjZHmMGdqs9YTegpRDpx97H5G3xqLehkgyrrZKsxGCmnwKexpZjXTCskUWwYywdUvrZK7L2vejeVZSYHVns61gm8VfU",
							ScanKey = "cc411aab02edcd3bccf484a9ba5280d4a774e6f81eac8ebec9cb1c2e8f73020a"
						}, "StressWatcher-" + locali);
				});
				tasks.Add(task);
			}
			Task.WhenAll(tasks.ToArray()).Wait();
		}
	}
}
