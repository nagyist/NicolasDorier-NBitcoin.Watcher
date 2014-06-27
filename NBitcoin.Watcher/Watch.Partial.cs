using NBitcoin.DataEncoders;
using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Watcher.Contracts
{

	public partial class Watch
	{
		
		public virtual Scanner CreateScanner()
		{
			return null;
		}
	}

	public partial class PubKeyHashWatch : Watch
	{
		public override Scanner CreateScanner()
		{
			var address = Network.CreateFromBase58Data(Address);
			if(address is BitcoinScriptAddress)
				return new ScriptHashScanner((ScriptId)((BitcoinScriptAddress)address).ID);
			if(address is BitcoinAddress)
				return new PubKeyHashScanner((KeyId)((BitcoinAddress)address).ID);
			return null;
		}
	}

	public partial class StealthAddressWatch : Watch
	{
		public override Scanner CreateScanner()
		{
			return new StealthPaymentScanner(
				Network.CreateFromBase58Data<BitcoinStealthAddress>(Address),
				new Key(Encoders.Hex.DecodeData(ScanKey)));
		}
	}

}
