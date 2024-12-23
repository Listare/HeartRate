using System;

namespace HeartRate
{
	public class BluetoothDeviceInfo
	{
		public string Name { get; set; }
		public ulong Address { get; set; }
		public string AddressString => $"{Address:x12}".Insert(10, ":").Insert(8, ":").Insert(6, ":").Insert(4, ":").Insert(2, ":");

		public static readonly Guid HeartrateServiceGuid = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");
		public static readonly Guid HeartrateMeasurementCharacteristicGuid = Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb");
	}
}
