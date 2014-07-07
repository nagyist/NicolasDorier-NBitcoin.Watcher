using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;
using System.Web.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NBitcoin.Watcher.Contracts;
using System.Web.Http.Dispatcher;
using System.IO;
using NBitcoin.RPC;
using NBitcoin.Protocol;
using System.Diagnostics;

namespace NBitcoin.Watcher
{
	public class WatcherOptions : IHttpControllerActivator
	{
		public WatcherOptions()
		{
			Parser.Default.ParseArguments(new string[0], this);

			_Watcher = new Lazy<Watcher>(() =>
	   {
		   return CreateWatcher();
	   }, false);
		}
		[ParserState]
		public IParserState LastParserState
		{
			get;
			set;
		}



		[Option('p', "port", DefaultValue = 80,
				   HelpText = "Listening port")]
		public int Port
		{
			get;
			set;
		}

		[Option("path", DefaultValue = "NBitcoin",
					HelpText = "Relative listening Url")]
		public string Path
		{
			get;
			set;
		}

		[Option('w', "watches",
					DefaultValue = "",
					HelpText = "Location of Watches (launch directory by default)")]
		public string WatchDirectory
		{
			get;
			set;
		}

		[Option("autoconfig",
					DefaultValue = false,
					HelpText = "If present, blkdir,rpcuser,rpcpassword,rpcservice will be deduced from locally running bitcoinq processes")]
		public bool AutoConfig
		{
			get;
			set;
		}

		[Option("rpcservice",
					DefaultValue = "http://localhost:8332/",
					HelpText = "Url to RPC Service (mainnet port 8332, testnet port 18332)")]
		public string RPCService
		{
			get;
			set;
		}

		[Option("testnet",
					DefaultValue = false)]
		public bool TestNet
		{
			get;
			set;
		}



		[Option("blkdir",
						HelpText = "blk***.dat folder of bitcoinq")]
		public string BlockDirectory
		{
			get;
			set;
		}

		[Option("rpcuser",
					HelpText = "Username to access RPC Service")]
		public string RPCUser
		{
			get;
			set;
		}
		[Option("rpcpassword",
					HelpText = "Password to access RPC Service")]
		public string RPCPassword
		{
			get;
			set;
		}


		string _Usage;
		[HelpOption('?', "help", HelpText = "Display this help screen.")]
		public string GetUsage()
		{
			if(_Usage == null)
				_Usage = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
			return _Usage;
			//
		}


		public Uri CreateBaseAddress()
		{
			EnsureAutoConfigured();
			UriBuilder builder = new UriBuilder("http://localhost/");
			builder.Port = Port;
			builder.Path = Path;
			return builder.Uri;
		}

		public async Task<HttpSelfHostServer> OpenSelfHost()
		{
			EnsureAutoConfigured();
			var address = CreateBaseAddress();
			WatcherTrace.Opening(address);
			var conf = new HttpSelfHostConfiguration(address);
			conf.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
			conf.Formatters.JsonFormatter.SerializerSettings = Serialization.CreateSettings();
			conf.MapHttpAttributeRoutes();
			conf.Services.Replace(typeof(IHttpControllerActivator), this);

			HttpSelfHostServer server = new HttpSelfHostServer(conf);
			await server.OpenAsync();
			WatcherTrace.Started();
			return server;
		}

		bool _Configured;
		public bool EnsureAutoConfigured()
		{
			if(!_Configured && AutoConfig)
			{
				var process =
					BitcoinQProcess.List()
					   .Where(p => p.Testnet == this.TestNet && p.Server)
					   .FirstOrDefault();
				if(process == null)
				{
					WatcherTrace.AutoConfigNotDetected();
					return false;
				}
				else
				{
					WatcherTrace.AutoConfigDetected(process.ToString());
					RPCUser = process.RPCUser;
					RPCPassword = process.RPCPassword;
					BlockDirectory = process.Parameters["blkdir"];
					RPCService = process.RPCService;
				}
				_Configured = true;
			}
			return _Configured || !AutoConfig;
		}

		#region IHttpControllerActivator Members

		public System.Web.Http.Controllers.IHttpController Create(System.Net.Http.HttpRequestMessage request, System.Web.Http.Controllers.HttpControllerDescriptor controllerDescriptor, Type controllerType)
		{
			EnsureAutoConfigured();
			if(controllerType == typeof(WatchController))
				return CreateController();
			return null;
		}

		Lazy<Watcher> _Watcher;
		Watcher Watcher
		{
			get
			{
				return _Watcher.Value;
			}
		}
		public WatchController CreateController()
		{
			EnsureAutoConfigured();
			if(WatchDirectory == "")
				WatchDirectory = Directory.GetCurrentDirectory();
			return new WatchController(Watcher);
		}

		#endregion

		public Watcher CreateWatcher(NodeServer nodeClient = null)
		{

			RPCClient client = new RPCClient(new System.Net.NetworkCredential(RPCUser, RPCPassword), new Uri(RPCService, UriKind.Absolute), TestNet ? Network.TestNet : Network.Main);
			if(WatchDirectory == "")
				WatchDirectory = Directory.GetCurrentDirectory();

			if(nodeClient == null)
				nodeClient = new NodeServer(client.Network);
			return new Watcher(client, nodeClient,
								new BlockStore(BlockDirectory, nodeClient.Network), WatchDirectory);
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			var options = new WatcherOptions();
			if(Parser.Default.ParseArguments(args, options))
			{
				if(!options.EnsureAutoConfigured())
				{
					Environment.ExitCode = 1;
					return;
				}
				using(var server = options.OpenSelfHost().Result)
				{
					Console.ReadLine();
				}
			}
		}
	}
}
