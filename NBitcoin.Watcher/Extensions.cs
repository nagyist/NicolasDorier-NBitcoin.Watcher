using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	static class Extensions
	{
		public static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dico, TKey key, TValue value)
		{
			if(dico.ContainsKey(key))
			{
				dico.Remove(key);
				dico.Add(key, value);
			}
			else
			{
				dico.Add(key, value);
			}
		}
	}
}
