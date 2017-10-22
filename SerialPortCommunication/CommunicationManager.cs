using System;
using System.Text;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
namespace PCComm
{
    class CommunicationManager
    {
        #region Manager Enums
        public enum MessageType { Incoming, Outgoing, Normal};
        #endregion

        #region Manager Variables
        //property variables
        private string _baudRate = string.Empty;
        private string _parity = string.Empty;
        private string _stopBits = string.Empty;
        private string _dataBits = string.Empty;
        private string _portName = string.Empty;
        private RichTextBox _displayWindow;
        //global manager variables
        private Color[] MessageColor = { Color.Blue, Color.Green, Color.Black };
        private SerialPort comPort = new SerialPort();
        private int SECOND_DELAY = 1000;
        #endregion

        #region Manager Properties
        public string BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        public string Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }

        public string PortName
        {
            get { return _portName; }
            set { _portName = value; }
        }

        public RichTextBox DisplayWindow
        {
            get { return _displayWindow; }
            set { _displayWindow = value; }
        }
        #endregion

        #region Manager Constructors

        public CommunicationManager()
        {
            _baudRate = string.Empty;
            _parity = string.Empty;
            _stopBits = string.Empty;
            _dataBits = string.Empty;
            _portName = string.Empty;
            _displayWindow = null;
            comPort.DataReceived += new SerialDataReceivedEventHandler(comPort_DataReceived);
        }
        #endregion

        #region WriteData
        public void WriteData(string msg)
        {
            waitFree();
            if (!(comPort.IsOpen == true)) comPort.Open();
            byte[] msgArray = Encoding.ASCII.GetBytes(msg);
            byte[] tempByte = new byte[1];
            foreach(Byte item in msgArray){
                for(int numberOfAttempt = 0; ; numberOfAttempt++) {
                    tempByte[0] = item;
                    comPort.Write(tempByte, 0, 1);
                    DisplayData(MessageType.Outgoing, Encoding.UTF8.GetString(tempByte));
                    Thread.Sleep(10);
                    if (isCollide()) {
                        tempByte[0] = 125;
                        comPort.Write(tempByte, 0, 1);
                        Delay(delaySending(numberOfAttempt));
                    } else break;
                }
            }
            tempByte[0] = 126;
            comPort.Write(tempByte, 0, 1);
            DisplayData(MessageType.Outgoing, "\n");
        }
        #endregion
        public bool isCollide() {
            if (timeOdd()) {
                System.Console.WriteLine("x");
                Console.Read();
                return true;
            }
            return false;
        }

        public bool isChannelBusy() {
            return timeOdd();
        }

        public void waitFree(){
            if (isChannelBusy()) {
                System.Console.WriteLine("Channel is busy");
                Thread.Sleep(SECOND_DELAY);
            }
            System.Console.WriteLine("Channel is free");
            Console.Read();
        }

        public int delaySending(int number)
        {
            return new Random().Next((int)Math.Pow(2, Math.Min(10, number))) * 10;
        }
        public void Delay(int delay){
            Thread.Sleep(delay);
        }

        private bool timeOdd()
        {
            return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds % 2) == 0;
        }

        #region DisplayData
        [STAThread]
        private void DisplayData(MessageType type, string msg)
        {
            _displayWindow.Invoke(new EventHandler(delegate
            {
                _displayWindow.SelectedText = string.Empty;
                _displayWindow.SelectionFont = new Font(_displayWindow.SelectionFont, FontStyle.Bold);
                _displayWindow.SelectionColor = MessageColor[(int)type];
                _displayWindow.AppendText(msg);
                _displayWindow.ScrollToCaret();
            }));
        }
        #endregion

        #region OpenPort
        public bool OpenPort()
        {
            try
            {
                if (comPort.IsOpen == true) comPort.Close();
                comPort.PortName = _portName; 
                comPort.BaudRate = int.Parse(_baudRate); 
                comPort.DataBits = 8;
                comPort.StopBits = (StopBits)1;
                comPort.Parity = (Parity)Enum.Parse(typeof(Parity), _parity);
                comPort.Open();
                DisplayData(MessageType.Normal, "Port opened at " + DateTime.Now + "\n");
                return true;
            }
            catch (Exception ex)
            {
                DisplayData(MessageType.Normal, ex.Message);
                return false;
            }
        }
        #endregion

        #region SetParityValues
        public void SetParityValues(object obj)
        {
            foreach (string str in Enum.GetNames(typeof(Parity)))
            {
                ((ComboBox)obj).Items.Add(str);
            }
        }
        #endregion

        #region SetPortNameValues
        public void SetPortValues(object obj)
        {

            foreach (string str in SerialPort.GetPortNames())
            {
                ((ComboBox)obj).Items.Add(str);
            }
        }
        #endregion

        #region comPort_DataReceived
        void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var receiveBuffer = new List<Byte>();
            while(true){
                byte[] b = new byte[1];
                if (comPort.BytesToRead == 0)
                {
                    continue;
                }
                comPort.Read(b, 0, 1);
                if(b[0] == 126)
                    break;
                if (b[0] == 125)
                {
                    receiveBuffer.RemoveAt(receiveBuffer.Count - 1);
                    continue;
                }
                receiveBuffer.Add(b[0]);
            }
            var message = Encoding.UTF8.GetString(receiveBuffer.ToArray());
            DisplayData(MessageType.Incoming, message + "\n");
        }
        #endregion
    }
}
