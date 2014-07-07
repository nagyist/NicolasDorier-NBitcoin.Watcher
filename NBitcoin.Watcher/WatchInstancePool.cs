using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	public class WatchMessage
	{
		public WatchMessage(string watchName)
		{
			_Watch = watchName;
		}
		private readonly string _Watch;
		public string Watch
		{
			get
			{
				return _Watch;
			}
		}

		public TaskCompletionSource<int> Completion
		{
			get;
			set;
		}

		public Action<WatchInstance> Action
		{
			get;
			set;
		}
	}
	public class WatchInstancePool : IDisposable
	{
		EventLoopMessageListener<WatchMessage> _MessageListener;
		volatile List<WatchInstance> _Instances = new List<WatchInstance>();
		MessageProducer<WatchMessage> _MessageProducer;

		void MessageReceived(WatchMessage message)
		{
			WatchInstance instance = null;
			lock(_Instances)
			{
				instance = _Instances.FirstOrDefault(i => i.Directory.Configuration.Name.Equals(message.Watch, StringComparison.OrdinalIgnoreCase));
			}
			if(instance == null)
				return;
			try
			{
				message.Action(instance);
			}
			finally
			{
				message.Completion.SetResult(0);
			}

		}

		IDisposable _Subscription;
		public WatchInstancePool(MessageProducer<WatchMessage> producer)
		{
			_MessageListener = new EventLoopMessageListener<WatchMessage>(MessageReceived);
			_Subscription = producer.AddMessageListener(_MessageListener);
			_MessageProducer = producer;
		}
		public Task AddInstance(WatchInstance instance, Chain mainChain)
		{
			lock(_Watches)
			{
				if(_Watches.Add(instance.Directory.Configuration.Name))
				{
					lock(_Instances)
					{
						_Instances.Add(instance);
					}
					return SendMessage(instance.Directory.Configuration.Name, i =>
						{
							i.Initialize(mainChain);
						});
				}
			}
			TaskCompletionSource<int> source = new TaskCompletionSource<int>();
			source.SetResult(0);
			return source.Task;
		}

		public Task SendMessage(Action<WatchInstance> act)
		{
			lock(_Watches)
			{
				return Task.WhenAll(_Watches.Select(w => SendMessage(w, act)).ToArray());
			}
		}

		public Task SendMessage(string watchName, Action<WatchInstance> act)
		{
			var completion = new TaskCompletionSource<int>();
			lock(_Watches)
			{
				if(_Watches.Contains(watchName))
				{

					_MessageProducer.PushMessage(new WatchMessage(watchName)
					{
						Completion = completion,
						Action = act
					});
					return completion.Task;
				}
			}
			completion.SetResult(0);
			return completion.Task;
		}


		HashSet<string> _Watches = new HashSet<string>();

		#region IDisposable Members

		public void Dispose()
		{
			_Subscription.Dispose();
			_MessageListener.Dispose();
		}

		#endregion

		public void RemoveInstance(string watchName)
		{
			lock(_Watches)
			{
				if(_Watches.Remove(watchName))
				{
					SendMessage(watchName, i =>
					{
						_Instances.Remove(i);
					});
				}
			}
		}
	}
}
