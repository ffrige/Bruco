using nexus.protocols.ble;
using nexus.protocols.ble.scan;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Xamarin.Essentials;

namespace Bruco
{
    public class ParametersModel : INotifyPropertyChanged
    {

        //class constructor
        public ParametersModel(IBluetoothLowEnergyAdapter bleAdapter)
        {

            //for Android this comes from MainActivity.cs
            _bluetoothAdapter = bleAdapter;

            ConnectCommand = new Command(async () => { await Test(); });
            ExitCommand = new Command(Exit);

            _bt_Status = "Not connected";
            _bt_Busy = false;
            _bt_Connected = false;
            _joy_X = 0;
            _joy_Y = 0;
            _sendDataX = new byte[1];
            _sendDataY = new byte[1];

            //get screen size
            // Get Metrics
            _mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            _screenHeight = (int)_mainDisplayInfo.Height;
            _screenWidth = (int)_mainDisplayInfo.Width;

        }

        //local variables
        private bool _bt_Connected, _bt_Busy;
        private string _bt_Status;
        private IBluetoothLowEnergyAdapter _bluetoothAdapter;
        private ObservableCollection<IBlePeripheral> _bt_devices = new ObservableCollection<IBlePeripheral>();
        private IBleGattServerConnection _gattServer;
        private int _joy_X, _joy_Y;
        DisplayInfo _mainDisplayInfo;
        int _screenHeight, _screenWidth;
        private const int X = 0;
        private const int Y = 1;
        private byte[] _sendDataX, _sendDataY;
        private IBlePeripheral BT_Device;

        //public properties
        public Command ConnectCommand { get; }
        public Command ExitCommand { get; }

        public string BT_Status
        {
            get => _bt_Status;
            set { _bt_Status = value; OnPropertyChanged(); }
        }

        public int JoyX
        {
            get => _joy_X;
            set
            {
                _joy_X = value;
                _sendDataX[0] = (byte)_joy_X;
                OnPropertyChanged(nameof(Joy_X_str));
            }
        }

        public int JoyY
        {
            get => _joy_Y;
            set
            {
                _joy_Y = value;
                _sendDataY[0] = (byte)_joy_Y;
                OnPropertyChanged(nameof(Joy_Y_str));
                //StartWriting();
            }
        }

        public string Joy_X_str
        {
            get => _joy_X.ToString();
            set
            {
                _joy_X = Convert.ToInt16(value);
                OnPropertyChanged();
            }
        }
        public string Joy_Y_str
        {
            get => _joy_Y.ToString();
            set
            {
                _joy_Y = Convert.ToInt16(value);
                OnPropertyChanged();
            }
        }
        public int TargetHeight
        {
            get => _screenWidth / 10;
        }
        public bool BT_Busy
        {
            get => _bt_Busy;
        }

        void StartWriting()
        {
            if (!_bt_Busy && _bt_Connected)
            {
                _bt_Busy = true;
                //Device.BeginInvokeOnMainThread(async () => { await Write(); });
                _bt_Busy = false;
            }
        }

        void Exit()
        {
            //TODO - add bluetooth disconnect command
            System.Environment.Exit(0);
        }

        async Task Test()
        {
                await Scan();
                await Connect();
                BT_Status = _gattServer.State.ToString();
                //try
                //{
                //    foreach (var guid in await _gattServer.ListAllServices())
                //    {
                //        BT_Status += guid.ToString();
                //    }

                //}
                //catch (Exception ex) { BT_Status = ex.ToString(); }
        }


        async Task Scan()
        {

            if (_bluetoothAdapter.CurrentState.Value != EnabledDisabledState.Enabled)
            {
                await Application.Current.MainPage.DisplayAlert("Message", "Bluetooth is not turned on!", "OK");
                //Device.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Message", "Bluetooth is not turned on!", "OK"));
                return;
            }

            //check for permissions
            Plugin.Permissions.Abstractions.PermissionStatus locationPermissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync<LocationPermission>();
            if (locationPermissionStatus != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Permission denied!", "Please try again.", "OK");
                _bt_Busy = false;
                BT_Status = "Not allowed!";
                return;
            }

            BT_Status = "Scanning...";
            _bt_Busy = true;

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _bluetoothAdapter.ScanForBroadcasts(

               // Your IObserver<IBlePeripheral> or Action<IBlePeripheral> will be triggered for each discovered
               // peripheral based on the provided scan settings and filter (if any).
               (IBlePeripheral peripheral) =>
               {
                   if (peripheral.Advertisement.DeviceName == "BRUCO")
                   {
                       BT_Status = "Found BRUCO!";
                       BT_Device = peripheral;
                   }
               },
               // Provide a CancellationToken to stop the scan, or use the overload that takes a TimeSpan.
               // If you omit this argument, the scan will timeout after BluetoothLowEnergyUtils.DefaultScanTimeout
               cts.Token
            );

            _bt_Busy = false;
            //BT_Status = "Scan completed";

        }

        
        async Task Connect()
        {
         
            if (_bluetoothAdapter.CurrentState.Value != EnabledDisabledState.Enabled)
            {
                await Application.Current.MainPage.DisplayAlert("Message", "Bluetooth is not turned on!", "OK");
                return;
            }

            if (BT_Device == null)
            {
                await Application.Current.MainPage.DisplayAlert("Warning", "Select a device to connect!", "OK");
                return;
            }

            BlePeripheralConnectionRequest connection = new BlePeripheralConnectionRequest();

            try
            {
                BT_Status = "Trying to connect to " + BT_Device.Advertisement.DeviceName;
                _bt_Busy = true;
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                connection = await _bluetoothAdapter.ConnectToDevice(BT_Device, cts.Token);
            }
            catch (Exception ex)
            {
                BT_Status = "ERROR " + ex.Message;
            }

            if (connection.IsSuccessful())
            {
                //connected successfully
                BT_Status = "Connected to " + BT_Device.Advertisement.DeviceName;
                _bt_Connected = true;
                _gattServer = connection.GattServer;

                //_gattServer.Subscribe(
                //    async c =>
                //    {
                //        if (c == ConnectionState.Disconnected)
                //        {
                //            await CloseConnection();
                //        }

                //    });

                //add some delay before continuing
                await Task.Delay(1000);
            }
            else
            {
                // could not connect to device
                BT_Status = "ERROR: Cannot connect to " + BT_Device.Advertisement.DeviceName;
            }

            _bt_Busy = false;
        }
        

        async void ScanAndConnect()
        {

            if (_bluetoothAdapter.CurrentState.Value != EnabledDisabledState.Enabled)
            {
                await Application.Current.MainPage.DisplayAlert("Message", "Bluetooth is not turned on!", "OK");
                return;
            }

            BlePeripheralConnectionRequest connection = new BlePeripheralConnectionRequest();

            try
            {
                _bt_Busy = true;
                BT_Status = "Trying to connect to BRUCO...";
                connection = await _bluetoothAdapter.FindAndConnectToDevice(
                new ScanFilter()
                .SetAdvertisedDeviceName("BRUCO"),
                TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                BT_Status = "ERROR " + ex.Message;
            }

            if (connection.IsSuccessful())
            {
                //connected successfully
                BT_Status = "Connected to BRUCO!";
                _bt_Connected = true;
                _gattServer = connection.GattServer;

                //show characteristics of Joystick service
                BT_Status = "";
                try
                {
                    foreach (var guid in await _gattServer.ListAllServices())
                    {
                        BT_Status += guid.ToString() + "\n\r";
                    }

                }
                catch (Exception e) { BT_Status = e.ToString(); }

                //add some delay before continuing
                await Task.Delay(1000);
            }
            else
            {
                // could not connect to device
                BT_Status = "ERROR: Cannot connect to BRUCO!";
            }
            _bt_Busy = false;
        }

        async void Write()
        {
            if (!_bt_Connected)
            {
                await Application.Current.MainPage.DisplayAlert("Message", "Bluetooth is not connected!", "OK");
                return;
            }
            try
            {
                _bt_Busy = true;
                await _gattServer.WriteCharacteristicValue(BLEIdentifiers.JoystickId, BLEIdentifiers.JoyXId, _sendDataX);
                //await _gattServer.WriteCharacteristicValue(BLEIdentifiers.JoystickId, BLEIdentifiers.JoyYId, _sendDataY);
            }
            catch (Exception ex)
            {
                BT_Status = "ERROR " + ex.Message;
            }
            _bt_Busy = false;
        }
        


        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }
}
