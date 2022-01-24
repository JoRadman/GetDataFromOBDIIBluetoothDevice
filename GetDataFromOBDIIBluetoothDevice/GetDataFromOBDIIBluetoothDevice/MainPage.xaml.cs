using Android.Bluetooth;
using Java.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;


namespace GetDataFromOBDIIBluetoothDevice
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        bool finished = false;

        BluetoothSocket BthSocket = null;
        BluetoothAdapter myAdapter;

        private Stream reader;
        private Stream writer;

        string A = string.Empty;
        string B = string.Empty;
        string C = string.Empty;
        string D = string.Empty;

        bool CanGetAgain = false;

        System.Timers.Timer GetData = new System.Timers.Timer();

        List<OBDIIDevicesWithVehicle> PairedDevices = new List<OBDIIDevicesWithVehicle>();

        string OBDIIDeviceText = string.Empty;
        public MainPage()
        {
            InitializeComponent();
            Init();
        }

        private void pi_OBDIIDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pi_OBDIIDevices.SelectedIndex >= 0)
            {
                en_DeviceMacAdress.Text = PairedDevices[pi_OBDIIDevices.SelectedIndex].MACAdress;
                OBDIIDeviceText = PairedDevices[pi_OBDIIDevices.SelectedIndex].Name;
            }
            else
            {
                en_DeviceMacAdress.Text = string.Empty;
                OBDIIDeviceText = string.Empty;
            }
        }
        /// <summary>
        /// Every 5 seconds device collect data from vehicle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_StartCollecting_Clicked(object sender, EventArgs e)
        {
            GetData.Interval = 5000;
            GetData.Elapsed += GetData_Elapsed;
            GetData.Start();
        }

        private void GetData_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool backdata;

            if (BthSocket != null)
            {
                if (BthSocket.IsConnected)
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (CanGetAgain == false)
                            backdata = await MainMethod();
                    });

                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        BluetoothConnectionAndStartingCodes();
                    });
                }
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    BluetoothConnectionAndStartingCodes();
                });
            }
        }

       /// <summary>
       /// It collects and shows all bluetooth paired devices from mobile phone or other device.
       /// </summary>
        private async void Init()
        {
            myAdapter = BluetoothAdapter.DefaultAdapter;

            if (myAdapter.IsEnabled == false)
            {

            }
            else
            {
                foreach (var item in myAdapter.BondedDevices)
                {
                    OBDIIDevicesWithVehicle obdii = new OBDIIDevicesWithVehicle();

                    obdii.Name = item.Name;
                    obdii.MACAdress = item.Address;

                    PairedDevices.Add(obdii);
                }

                if (PairedDevices.Count > 0)
                {
                    foreach (var device in PairedDevices)
                        pi_OBDIIDevices.Items.Add(device.Name);
                }
            }
        }

        /// <summary>
        /// With this method you're sending codes to OBDII device in vehicle and then it will send back information you need.
        /// </summary>
        public async void BluetoothConnectionAndStartingCodes()
        {
            try
            {
                bool connected = Connection().Result;

                if (connected)
                {
                    reader = BthSocket.InputStream;
                    writer = BthSocket.OutputStream;

                    string s1, s2, s3, s4, s5, s6, s7, s8;

                    s1 = await SendAndReceive("ATZ\r"); //Reset
                    s2 = await SendAndReceive("ATI\r"); //Get Info about OBDII Device
                    s3 = await SendAndReceive("ATH0\r"); //doesn't return any numbers before "real" return code
                    s4 = await SendAndReceive("ATE0\r"); //echo turned off
                    s5 = await SendAndReceive("ATL1\r"); //linefeeds are turned on, now you will get empty lines between return codes. We set code to fit with this,so don't change it
                    s6 = await SendAndReceive("ATAL\r"); //we allowed big return bytes
                    s7 = await SendAndReceive("ATS1\r"); //we allowed spaces between numbers in return code. Example: 41 0C FF, if ATS0 you will get: 410CFF
                    s8 = await SendAndReceive("ATSP0\r"); //protocol is set to automatic

                    bool result = await MainMethod();
                }
                else
                {
                    BluetoothConnectionAndStartingCodes();
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Make bluetooth connection with OBDII device in vehicle.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connection()
        {
            bool backdata = false;
            try
            {
                myAdapter = BluetoothAdapter.DefaultAdapter;

                if (myAdapter.IsEnabled == false)
                {
                    //here you can write your code when the bluetooth is off. Example: Tell user to turn on his bluetooth on mobile phone
                }

                BluetoothDevice device = (from bd in myAdapter.BondedDevices
                                          where bd.Name == OBDIIDeviceText
                                          select bd).FirstOrDefault();

                UUID uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

                if ((int)Android.OS.Build.VERSION.SdkInt >= 10)
                    BthSocket = device.CreateInsecureRfcommSocketToServiceRecord(uuid);
                else
                    BthSocket = device.CreateRfcommSocketToServiceRecord(uuid);

                if (BthSocket != null)
                {
                    BthSocket.Connect();
                }

                if (BthSocket.IsConnected)
                {
                    backdata = true;
                }
            }
            catch (Exception ex)
            {
                backdata = false;
            }
            return backdata;
        }

        /// <summary>
        /// With this method you send code and receive data from OBDII device.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private async Task<string> SendAndReceive(string msg)
        {
            string s = string.Empty;
            try
            {
                bool povrat = await WriteAsync(msg);
                s = await ReadAsync();
                System.Diagnostics.Debug.WriteLine("Received: " + s);
                s = s.Replace("SEARCHING...\r\n", "");
            }
            catch (Exception ex)
            {

            }
            return s;
        }

        /// <summary>
        /// Sending code to OBDII device.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private async Task<bool> WriteAsync(string msg)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(msg);
                byte[] buffer = GetBytes(msg);
                await writer.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {

            }
            return true;
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = null;
            try
            {
                bytes = new byte[str.Length * sizeof(char)];
                Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {

            }
            return bytes;
        }

        /// <summary>
        /// Read data that OBDII send it back after receiving code.
        /// You'll receive complicated string that needs to be fixed. But it will end with >.
        /// </summary>
        /// <returns></returns>
        private async Task<string> ReadAsync()
        {
            string ret = string.Empty;
            try
            {
                ret = await ReadAsyncRaw();
                while (!ret.Trim().EndsWith(">"))
                {
                    string tmp = await ReadAsyncRaw();
                    ret = ret + tmp;
                }
            }
            catch (Exception ex)
            {

            }
            return ret;
        }

        private async Task<string> ReadAsyncRaw()
        {
            string s = string.Empty;
            try
            {
                byte[] buffer = new byte[1024];
                var bytes = await reader.ReadAsync(buffer, 0, buffer.Length);
                var s1 = new Java.Lang.String(buffer, 0, bytes);
                s = s1.ToString();
                System.Diagnostics.Debug.WriteLine(s);
            }
            catch (Exception ex)
            {

            }
            return s;
        }

        /// <summary>
        /// With this method you're sending codes and receiving data that you want from vehicle. You can find all the codes on internet.
        /// These are just some of them. Reminder: Some of the codes won't work for some cars.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> MainMethod()
        {
            bool finished = false;

            try
            {
                string enginecoolantemperature, enginespeed, vehiclespeed, intakeairtemperature, maf;

                if (BthSocket.IsConnected)
                {
                    CanGetAgain = true;

                    enginecoolantemperature = await SendAndReceive("0105\r");
                    TransformAndWriteData(enginecoolantemperature);

                    enginespeed = await SendAndReceive("010C\r");
                    TransformAndWriteData(enginespeed);

                    vehiclespeed = await SendAndReceive("010D\r");
                    TransformAndWriteData(vehiclespeed);

                    intakeairtemperature = await SendAndReceive("010F\r");
                    TransformAndWriteData(intakeairtemperature);

                    maf = await SendAndReceive("0110\r");
                    TransformAndWriteData(maf);

                    finished = true;
                }
            }
            catch (Exception ex)
            {

            }
            CanGetAgain = false;
            return finished;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------



            /// <summary>
            /// 
            /// </summary>
            /// <param name="DataThatWeGet"></param>
        private void TransformAndWriteData(string DataThatWeGet)
        {
            string Data = string.Empty;
            string AfterDataIsReduced = string.Empty;
            string[] MoreBytes = null;
            int NumberOfBytes = 0;

            try
            {
                Data = WhatDoesItContains(DataThatWeGet);

                if (Data != string.Empty)
                    AfterDataIsReduced = Convert(Data, DataThatWeGet);

                if (AfterDataIsReduced != string.Empty)
                    MoreBytes = AfterDataIsReduced.Split();

                if (MoreBytes != null)
                    NumberOfBytes = MoreBytes.Length;

                if (NumberOfBytes > 0)
                {

                    //These strings: A, B, C, D are important for transforming hexa to regular data that you actually want and that means something.

                    if (NumberOfBytes == 1)
                        A = MoreBytes[0];

                    if (NumberOfBytes == 2)
                    {
                        A = MoreBytes[0];
                        B = MoreBytes[1];
                    }

                    if (NumberOfBytes == 3)
                    {
                        A = MoreBytes[0];
                        B = MoreBytes[1];
                        C = MoreBytes[2];
                    }

                    if (NumberOfBytes == 4)
                    {
                        A = MoreBytes[0];
                        B = MoreBytes[1];
                        C = MoreBytes[2];
                        D = MoreBytes[3];
                    }

                    string FinalResult = Calculation(Data);
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Here you're editing returned infromation from vehicle. It comes with lot of signs and marks and only what is important for will come after this method.
        /// </summary>
        /// <param name="WhatDoWeCalculate"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string Convert(string WhatDoWeCalculate, string data)
        {
            string Result = string.Empty;
            try
            {
                var stringWithoutEmptyLines = Regex.Replace(data, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                var SplitWithoutEmptyLines = stringWithoutEmptyLines.Split('\n');
                var LineThatWeNeed = SplitWithoutEmptyLines[SplitWithoutEmptyLines.Length - 2];

                string LineThatWeNeedString = LineThatWeNeed.ToString();

                if (LineThatWeNeedString.Contains(WhatDoWeCalculate))
                {
                    int StartingIndex = LineThatWeNeedString.IndexOf(WhatDoWeCalculate) + 5;
                    int LastIndex = LineThatWeNeedString.Length - 1;

                    Result = LineThatWeNeedString.Substring(StartingIndex, (LastIndex - StartingIndex));
                    Result = Result.Trim();
                }
            }
            catch (Exception ex)
            {

            }

            return Result;
        }

        /// <summary>
        /// Here you get final information about: vehicle speed, rpm, etc.
        /// </summary>
        /// <param name="DataToCalculate"></param>
        /// <returns></returns>
        private string Calculation(string DataToCalculate)
        {
            decimal Result = 0;
            string StringResult = string.Empty;

            try
            {
                decimal AHex = 0;
                decimal BHex = 0;
                decimal CHex = 0;
                decimal DHex = 0;

                if (A != string.Empty)
                    AHex = int.Parse(A, System.Globalization.NumberStyles.HexNumber);
                if (B != string.Empty)
                    BHex = int.Parse(B, System.Globalization.NumberStyles.HexNumber);
                if (C != string.Empty)
                    CHex = int.Parse(C, System.Globalization.NumberStyles.HexNumber);
                if (D != string.Empty)
                    DHex = int.Parse(D, System.Globalization.NumberStyles.HexNumber);

                if (DataToCalculate == "41 05")
                {
                    Result = AHex - 40;
                    StringResult = Result.ToString() + " °C";
                    en_EngineCoolantTemperature.Text = StringResult;
                }

                if (DataToCalculate == "41 0C")
                {
                    Result = ((256 * AHex) + BHex) / 4;
                    StringResult = Result.ToString() + " rpm";
                    en_EngineSpeed.Text = StringResult;
                }

                if (DataToCalculate == "41 0D")
                {
                    Result = AHex;
                    StringResult = Result.ToString() + " km/h";
                    en_VehicleSpeed.Text = StringResult;
                }

                if (DataToCalculate == "41 0F")
                {
                    Result = AHex - 40;
                    StringResult = Result.ToString() + " °C";
                    en_IntakeAirTemperature.Text = StringResult;
                }

                if (DataToCalculate == "41 10")
                {
                    Result = ((256 * AHex) + BHex) / 100;
                    StringResult = Result.ToString() + " g/sec";
                    en_MAF.Text = StringResult;
                }
            }
            catch (Exception ex)
            {

            }


            return StringResult;
        }

        /// <summary>
        /// Here yiu find out what actually your returned code means for regular user.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string WhatDoesItContains(string data)
        {
            string contains = string.Empty;

            try
            {
                if (data.Contains("41 05"))
                    contains = "41 05";
                if (data.Contains("41 0C"))
                    contains = "41 0C";
                if (data.Contains("41 0D"))
                    contains = "41 0D";
                if (data.Contains("41 0F"))
                    contains = "41 0F";
                if (data.Contains("41 10"))
                    contains = "41 10";
            }
            catch (Exception ex)
            {

            }

            return contains;
        }
    }
}
