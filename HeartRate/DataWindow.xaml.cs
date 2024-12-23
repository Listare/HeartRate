using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace HeartRate
{
	/// <summary>
	/// DataWindow.xaml 的交互逻辑
	/// </summary>
	public partial class DataWindow : Window
	{
		private BluetoothDeviceInfo _info;
		private BluetoothLEDevice _bleDevice;
		private GattCharacteristic _heartrateMeasurement;
		private bool _showAverage = false;
		private List<int> _averagePool;
		private bool _transparent = false;

		public DataWindow(BluetoothDeviceInfo device)
		{
			InitializeComponent();
			_info = device;
			_averagePool = new List<int>();
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			await ConnectAsync();
		}

		private async Task ConnectAsync()
		{
			try
			{
				_bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(_info.Address).AsTask();
				_bleDevice.ConnectionStatusChanged += BleDevice_ConnectionStatusChanged;
				var services = await _bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask();
				var heartrateService = services.Services.FirstOrDefault(s => s.Uuid == BluetoothDeviceInfo.HeartrateServiceGuid) ?? throw new Exception("未找到心率服务");
				var characteristics = await heartrateService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask();
				_heartrateMeasurement = characteristics.Characteristics.FirstOrDefault(c => c.Uuid == BluetoothDeviceInfo.HeartrateMeasurementCharacteristicGuid) ?? throw new Exception("未找到心率测量功能");
				_heartrateMeasurement.ProtectionLevel = GattProtectionLevel.Plain;
				_heartrateMeasurement.ValueChanged += HeartrateMeasurementCharasterstic_ValueChanged;

				var result = await _heartrateMeasurement.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask();
				if (result != GattCommunicationStatus.Success)
				{
					throw new Exception("启用通知失败: " + result.ToString());
				}
			}
			catch (Exception ex)
			{
				_bleDevice?.Dispose();
				_bleDevice = null;
				_heartrateMeasurement = null;

				MessageBox.Show(this, ex.Message, "连接设备失败", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}

		private async void BleDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
		{
			if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
			{
				var result = MessageBox.Show(this, "连接已关闭, 是否重连", "连接断开", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
				if (result == MessageBoxResult.OK)
				{
					_bleDevice?.Dispose();
					_bleDevice = null;

					try
					{
						await _bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask();
					}
					catch (Exception ex)
					{
						Dispatcher.Invoke(() =>
						{
							MessageBox.Show(this, ex.Message, "连接设备失败", MessageBoxButton.OK, MessageBoxImage.Error);
							Close();
						});
					}
				}
				else
				{
					Close();
				}
			}
		}

		private void HeartrateMeasurementCharasterstic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
		{
			CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] data);
			var heartrate = HeartrateProtocol.Parse(data);
			_averagePool.Add(heartrate.Heartrate.Value);
			if (_averagePool.Count > 20)
			{
				_averagePool.RemoveAt(0);
			}
			var average = (int)_averagePool.Average();

			Dispatcher.Invoke(() =>
			{
				if (_showAverage)
				{
					HeartRateRealtime.Text = heartrate.Heartrate.ToString();
					HeartRateSplitter.Text = "/";
					HeartRateAverage.Text = average.ToString();
				}
				else
				{
					HeartRateRealtime.Text = heartrate.Heartrate.ToString();
					HeartRateSplitter.Text = "";
					HeartRateAverage.Text = "";
				}
			});
		}

		private void MenuSwitchTransparent_Click(object sender, RoutedEventArgs e)
		{
			_transparent = !_transparent;
			WindowUtils.SetWindowTransparency(this, _transparent);

			var menu = Resources["TaskbarMenu"] as ContextMenu;
			var item = menu.Items[0] as MenuItem;
			if (_transparent)
			{
				item.Header = "关闭穿透";
			}
			else
			{
				item.Header = "开启穿透";
			}
		}

		private void MenuSwitchAverage_Click(object sender, RoutedEventArgs e)
		{
			_showAverage = !_showAverage;

			var menu = Resources["TaskbarMenu"] as ContextMenu;
			var item = menu.Items[1] as MenuItem;
			if (_showAverage)
			{
				item.Header = "关闭显示平均值";
			}
			else
			{
				item.Header = "开启显示平均值";
			}
		}

		private void MenuClose_Click(object sender, RoutedEventArgs e)
		{
			_bleDevice?.Dispose();
			_bleDevice = null;
			Close();
		}

		private void Window_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			DragMove();
		}
	}
}
