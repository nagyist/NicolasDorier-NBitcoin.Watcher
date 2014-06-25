using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

#if WATCHER_CLIENT
namespace NBitcoin.Watcher.Client
#else
namespace NBitcoin.Watcher.Contracts
#endif
{
	public class FriendlyNameAttribute : Attribute
	{
		public FriendlyNameAttribute(string name)
		{
			Name = name;
		}
		public string Name
		{
			get;
			set;
		}
	}
	public class Serialization
	{

		public static JsonSerializerSettings CreateSettings()
		{
			var seria = new JsonSerializerSettings();
			seria.TypeNameHandling = TypeNameHandling.Auto;
			seria.Binder = new FriendlyTypeBinder();
			seria.Formatting = Formatting.Indented;
			return seria;
		}
		public static JsonSerializer CreateSerializer()
		{
			return JsonSerializer.Create(CreateSettings());
		}
	}

	public class FriendlyTypeBinder : SerializationBinder
	{
		static Dictionary<string, Type> _NameToTypeMapping;
		static Dictionary<Type, string> _TypeToNameMapping;

		public void EnsureInitialized()
		{
			if(_NameToTypeMapping == null)
			{
				var friendlyTypes =
					typeof(FriendlyTypeBinder).Assembly
					.GetTypes()
					.Where(t => t.IsDefined(typeof(FriendlyNameAttribute), false)).ToArray();

			
				var nameToType = new Dictionary<string, Type>();
				var typeToName = new Dictionary<Type, string>();
				foreach(var type in friendlyTypes)
				{
					var friendlyName = ((FriendlyNameAttribute)(type.GetCustomAttributes(typeof(FriendlyNameAttribute),false)[0])).Name;
					typeToName.Add(type, friendlyName);
					friendlyName = friendlyName.ToLowerInvariant();
					nameToType.Add(friendlyName, type);
				}

				_NameToTypeMapping = nameToType;
				_TypeToNameMapping = typeToName;
			}
		}

		public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			EnsureInitialized();
			assemblyName = null;
			typeName = _TypeToNameMapping[serializedType];
		}
		public override Type BindToType(string assemblyName, string typeName)
		{
			EnsureInitialized();
			return _NameToTypeMapping[typeName.ToLowerInvariant()];
		}
	}
}
