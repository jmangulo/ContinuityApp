using System.IO.Ports;
using System.Text;

namespace Connect112
{
    public interface ICommunication
    {
        bool Open();

        void Close();

        bool IsDeviceFound();

        void Initialize();

        bool TestPin(int pin);

        bool TurnOnAutoTest();

        bool TurnOffAutoTest();

        bool Reset();

        EventHandler<string>? OnDataDetected { get; set; }

        EventHandler<bool>? OnDeviceStatus { get; set; }
    }

    public class SerialCommunication : ICommunication
    {
        private readonly StringBuilder messageBuffer = new StringBuilder();

        private readonly List<string> portNames;

        private readonly List<string> validPortNames;

        private SerialPort? selectedPort;

        public EventHandler<string>? OnDataDetected { get; set; }
        public EventHandler<bool>? OnDeviceStatus { get; set; }

        public SerialCommunication()
        {
            portNames = new List<string>();
            validPortNames = new List<string>();
        }

        public void Initialize()
        {
            LoadAvailableComports();
            AutoDetectDevice();
            AutoSelectDevice();
            ResetDevice();
            MonitorPort(true);
        }

        public bool IsDeviceFound()
        {
            return selectedPort != null;
        }

        public bool TestPin(int pin)
        {
            string cmd = $"READ:{pin}";
            return SendCommandAndRead(cmd) == "1";
        }

        public bool TurnOnAutoTest()
        {
            string cmd = "AUTO_ON";
            return SendCommandAndRead(cmd) == "P";
        }

        public bool TurnOffAutoTest()
        {
            string cmd = "AUTO_OFF";
            return SendCommandAndRead(cmd) == "P";
        }

        public bool Open()
        {
            return OpenPort(selectedPort);
        }

        public void Close()
        {
            Close(selectedPort);
        }

        public bool Reset()
        {
            return ResetDevice();
        }

        private void LoadAvailableComports()
        {
            portNames.Clear();
            string[] ports = SerialPort.GetPortNames();
            ports = ports.Distinct().ToArray();
            foreach (string port in ports)
            {
                portNames.Add(port);
            }
        }

        private void AutoDetectDevice()
        {
            foreach (string pn in portNames)
            {
                using SerialPort sp = GetSerialPort(pn);
                bool isOpen = OpenPort(sp);
                if (isOpen)
                {
                    Write(sp, "IDEN");
                    Thread.Sleep(250);
                    string response = Read(sp);
                    if (response?.Contains("CONNECT_112") == true)
                    {
                        validPortNames.Add(pn);
                    }
                }
                Close(sp);

                if (validPortNames.Count > 0)
                {
                    break;
                }
            }
        }

        private void AutoSelectDevice()
        {
            if (validPortNames.Count > 0)
            {
                selectedPort = GetSerialPort(validPortNames[0]);
                OnDeviceStatus?.Invoke(this, true);
            }
            else
            {
                OnDeviceStatus?.Invoke(this, false);
            }
        }

        private SerialPort GetSerialPort(string portName)
        {
            SerialPort port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = 9600;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            return port;
        }

        private bool OpenPort(SerialPort? port)
        {
            bool isOpenSuccessful = false;
            try
            {
                if (port != null)
                {
                    if (!port.IsOpen)
                    {
                        port.Open();
                    }
                    isOpenSuccessful = true;
                }
            }
            catch (Exception) { }

            return isOpenSuccessful;
        }

        private void Write(SerialPort? port, string cmd)
        {
            port?.Write(cmd + "\n");
        }

        private string CleanMessage(string message)
        {
            message = message.Trim();
            message = message.TrimEnd(new char[] { '\r', '\n' });
            message = message.TrimStart('\0');

            return message;
        }

        private string Read(SerialPort? port)
        {
            if (port == null)
                return string.Empty;
            if (!port.IsOpen)
                return string.Empty;
            string? incoming = port.ReadExisting();
            incoming = incoming?.Trim();
            incoming = incoming?.TrimEnd(new char[] { '\r', '\n' });
            incoming = incoming?.TrimStart('\0');
            return incoming == null ? "" : incoming;
        }

        private void Close(SerialPort? port)
        {
            if (port?.IsOpen == true)
            {
                port.Close();
            }
        }

        private string SendCommandAndRead(string cmd)
        {
            MonitorPort(false);
            Write(selectedPort, cmd);
            Task.Delay(250).Wait();
            string res = Read(selectedPort);
            MonitorPort(true);
            return res;
        }

        private void MonitorPort(bool on)
        {
            if (selectedPort != null)
            {
                if (on)
                {
                    selectedPort.DataReceived += OnDataReceived;
                }
                else
                {
                    selectedPort.DataReceived -= OnDataReceived;
                }
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (selectedPort == null)
                return;
            string receivedData = selectedPort.ReadExisting();
            messageBuffer.Append(receivedData);

            if (messageBuffer.ToString().Contains("\n"))
            {
                string completeMessage = CleanMessage(messageBuffer.ToString());
                messageBuffer.Clear();
                OnDataDetected?.Invoke(this, completeMessage);
            }
        }

        private bool ResetDevice()
        {
            string response = "";
            if (OpenPort(selectedPort))
            {
                Write(selectedPort, "RESET");
                Task.Delay(250).Wait();
                response = Read(selectedPort);
            }

            return response.Trim() == "INIT";
        }
    }
}
