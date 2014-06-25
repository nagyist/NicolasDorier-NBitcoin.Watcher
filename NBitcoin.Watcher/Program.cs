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

namespace NBitcoin.Watcher
{
	public class WatcherOptions : IHttpControllerActivator
	{
		public WatcherOptions()
		{
			Parser.Default.ParseArguments(new string[0], this);
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

		[Option('w',"watches", 
					DefaultValue="",
					HelpText="Location of Watches (launch directory by default)")]
		public string WatchDirectory
		{
			get;
			set;
		}

		string _Usage;
		[HelpOption('?', "help", HelpText = "Display this help screen.")]
		public string GetUsage()
		{
			if(_Usage == null)
				_Usage = HelpText.AutoBuild(this,(HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
			return _Usage;
			//
		}

		public Uri CreateBaseAddress()
		{
			UriBuilder builder = new UriBuilder("http://localhost/");
			builder.Port = Port;
			builder.Path = Path;
			return builder.Uri;
		}

		public async Task<HttpSelfHostServer> OpenSelfHost()
		{
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

		#region IHttpControllerActivator Members

		public System.Web.Http.Controllers.IHttpController Create(System.Net.Http.HttpRequestMessage request, System.Web.Http.Controllers.HttpControllerDescriptor controllerDescriptor, Type controllerType)
		{
			if(controllerType == typeof(WatchController))
				return CreateController();
			return null;
		}

		public WatchController CreateController()
		{
			if(WatchDirectory == "")
				WatchDirectory = Directory.GetCurrentDirectory();
			return new WatchController(WatchDirectory);
		}

		#endregion
	}
	class Program
	{
		static void Main(string[] args)
		{
			var options = new WatcherOptions();
			if(Parser.Default.ParseArguments(args, options))
			{
				using(var server = options.OpenSelfHost().Result)
				{
					Console.ReadLine();
				}
			}
		}
	}
}
