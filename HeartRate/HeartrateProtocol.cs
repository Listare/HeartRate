using System;

namespace HeartRate
{
	internal class HeartrateProtocol
	{
		public ushort? Heartrate { get; private set; }
		public bool ContractDetected { get; private set; }
		public ushort? EnergyExpended { get; private set; }
		public ushort[] RRIntervals { get; private set; }

		public static HeartrateProtocol Parse(byte[] data)
		{
			if (data.Length < 1)
				throw new ArgumentException("data is too short");

			bool isHeartrate16bit = (data[0] & 1) != 0;
			bool contractDetected = (data[0] & 2) != 0;
			bool contractSupported = (data[0] & 3) != 0;
			bool energyExpendedPresent = (data[0] & 8) != 0;
			bool rrIntervalPresent = (data[0] & 16) != 0;

			var result = new HeartrateProtocol();
			result.Heartrate = null;
			result.EnergyExpended = null;
			result.RRIntervals = null;

			int offset = 0;
			var flag = data[offset++];
			if (isHeartrate16bit)
			{
				result.Heartrate = BitConverter.ToUInt16(data, offset);
				offset += 2;
			}
			else
			{
				result.Heartrate = data[offset++];
			}

			result.ContractDetected = contractDetected;

			if (energyExpendedPresent)
			{
				// uint16
				result.EnergyExpended = BitConverter.ToUInt16(data, offset);
				offset += 2;
			}

			if ((flag & (1 << 4)) != 0)
			{
				var count = (data.Length - offset) / 2;
				result.RRIntervals = new ushort[count];
				for (int i = 0; i < count; i++)
				{
					result.RRIntervals[i] = BitConverter.ToUInt16(data, offset);
					offset += 2;
				}
			}

			return result;
		}
	}
}
