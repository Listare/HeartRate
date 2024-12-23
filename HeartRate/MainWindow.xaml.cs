using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

namespace HeartRate
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private HashSet<ulong> _devices;
		private bool _isScanning = false;
		private BluetoothLEAdvertisementWatcher _bleWatcher;

		public MainWindow()
		{
			InitializeComponent();

			_devices = new HashSet<ulong>();
			_bleWatcher = new BluetoothLEAdvertisementWatcher();
			_bleWatcher.ScanningMode = BluetoothLEScanningMode.Passive;
			_bleWatcher.AllowExtendedAdvertisements = true;
			_bleWatcher.Received += Advertisement_Received;
		}

		private async void Advertisement_Received(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs e)
		{
			if (e.AdvertisementType == BluetoothLEAdvertisementType.ConnectableUndirected)
			{
				if (_devices.Contains(e.BluetoothAddress))
				{
					return;
				}

				if (e.Advertisement.ServiceUuids.Contains(BluetoothDeviceInfo.HeartrateServiceGuid))
				{
					// 包含心率服务的设备
					var address = $"{e.BluetoothAddress:x12}".Insert(10, ":").Insert(8, ":").Insert(6, ":").Insert(4, ":").Insert(2, ":");

					string deviceName = "<Unknown>";
					if (!string.IsNullOrEmpty(e.Advertisement.LocalName))
					{
						deviceName = e.Advertisement.LocalName;
					}
					else
					{
						foreach (var section in e.Advertisement.DataSections)
						{
							if (section.DataType == 0x09)
							{
								using (var reader = DataReader.FromBuffer(section.Data))
								{
									deviceName = reader.ReadString(section.Data.Length);
								}
								break;
							}
						}
						if (!string.IsNullOrEmpty(deviceName))
						{
							using (var device = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress).AsTask())
							{
								if (device.Name != $"Bluetooth {address}")
								{
									deviceName = device.Name;
								}
							}
						}
					}

					BluetoothDeviceInfo info = new BluetoothDeviceInfo
					{
						Name = e.Advertisement.LocalName,
						Address = e.BluetoothAddress
					};
					_devices.Add(info.Address);
					Dispatcher.Invoke(() =>
					{
						bool found = false;
						for (int i = 0; i < DeviceListView.Items.Count; i++)
						{
							var cur = DeviceListView.Items[i] as BluetoothDeviceInfo;
							if (cur.Address == info.Address)
							{
								DeviceListView.Items[i] = info;
								found = true;
								break;
							}
						}
						if (!found)
						{
							DeviceListView.Items.Add(info);
						}
					});
				}
			}
		}

		private void ScanButton_Click(object sender, RoutedEventArgs e)
		{
			if (_isScanning)
			{
				_isScanning = false;
				_bleWatcher.Stop();
				ScanButton.Content = "开始扫描";
			}
			else
			{
				_isScanning = true;
				DeviceListView.Items.Clear();
				_devices.Clear();
				_bleWatcher.Start();
				ScanButton.Content = "停止扫描";
			}
		}

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedDevice = DeviceListView.SelectedItem as BluetoothDeviceInfo;
			if (selectedDevice == null)
			{
				return;
			}

			if (_isScanning)
			{
				_isScanning = false;
				_bleWatcher.Stop();
				ScanButton.Content = "开始扫描";
			}

			var window = new DataWindow(selectedDevice);
			window.Show();
			Close();
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				var adapter = await BluetoothAdapter.GetDefaultAsync().AsTask();
				if (!adapter.IsLowEnergySupported)
				{
					throw new Exception("不支持低功耗蓝牙");
				}
				var radio = await adapter.GetRadioAsync().AsTask();
				if (radio.State != RadioState.On)
				{
					MessageBox.Show(this, "蓝牙未开启", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}
	}
}
