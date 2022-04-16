using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class HNY1 : NesBoardBase
	{
		// Config
		private int prg_bank_count;

		// State
		private int prg_bank;
		private int prg_reg;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_reg), ref prg_reg);

			if(ser.IsReader)
				SyncPRG();
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch(Cart.BoardType)
			{
				case "MAPPER0300-00":
					break;
				default:
					return false;
			}

			prg_bank_count = Cart.PrgSize / 16;

			SyncPRG();

			return true;
		}

		private void SyncPRG()
		{
			prg_bank = prg_reg % prg_bank_count;
		}

		public override void WritePrg(int addr, byte value)
		{
			switch(addr & 0xC000)
			{
				case 0x0000: //$8000:      PRG Reg LO
					prg_reg &= ~0xff;
					prg_reg |= value;
					break;
				case 0x4000: //$C000:      PRG Reg HI
					prg_reg &= ~0xff00;
					prg_reg |= value << 8;
					break;
			}
			SyncPRG();
		}

		public override byte ReadPrg(int addr)
		{
			int bank = addr >> 14;
			int offset = addr & ((1 << 14) - 1);
			if(bank == 1)
			{
				addr = ((prg_bank_count - 1) << 14) | offset;
			}
			else
			{
				addr = (prg_bank << 14) | offset;
			}
			return Rom[addr];
		}
	}
}
