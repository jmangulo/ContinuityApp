using System.IO.Ports;
using System.Text;

namespace Connect112
{
    public class SerialCommunicationUnitTest : ICommunication
    {
        private readonly Random random = new Random();

        private readonly StringBuilder messageBuffer = new StringBuilder();

        private readonly List<string> portNames;

        private readonly List<string> validPortNames;

        private SerialPort? selectedPort;

        public EventHandler<string>? OnDataDetected { get; set; }
        public EventHandler<bool>? OnDeviceStatus { get; set; }

        public SerialCommunicationUnitTest()
        {
            portNames = new List<string>();
            validPortNames = new List<string>();
        }

        public void Close()
        {

        }

        public void Initialize()
        {
            portNames.Clear();
            string[] ports = new string[] { "COM3" };
            ports = ports.Distinct().ToArray();
            foreach (string port in ports)
            {
                portNames.Add(port);
            }

            foreach (string pn in portNames)
            {
                Thread.Sleep(250);
                validPortNames.Add(pn);
            }

            if (validPortNames.Count > 0)
            {
                selectedPort = GetSerialPort(validPortNames[0]);
                OnDeviceStatus?.Invoke(this, true);
            }
            else
            {
                OnDeviceStatus?.Invoke(this, false);
            }

            ResetDevice();
        }

        public bool IsDeviceFound()
        {
            return true;
        }

        public bool Open()
        {
            return true;
        }

        public bool Reset()
        {
            return ResetDevice();
        }

        public bool TestPin(int pin)
        {
            Task.Delay(250).Wait();
            return random.Next(100) < 50;
        }

        public bool TurnOffAutoTest()
        {
            Task.Delay(250).Wait();
            return true;
        }

        public bool TurnOnAutoTest()
        {
            Task.Delay(250).Wait();
            return true;
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

        private bool ResetDevice()
        {
            Thread.Sleep(250);
            return true;
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
                string completeMessage = messageBuffer.ToString();
                messageBuffer.Clear();
                OnDataDetected?.Invoke(this, completeMessage);
            }
        }
    }
}
