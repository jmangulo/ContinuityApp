using System.IO.Ports;
using System.Text;

namespace Connect112
{
    public class SerialCommunicationUnitTest : ICommunication
    {
        private readonly Random _random = new Random();
        private System.Timers.Timer? _timer;
        private int _dummyChannel = 0;

        private readonly StringBuilder _messageBuffer = new StringBuilder();

        private readonly List<string> _portNames;

        private readonly List<string> _validPortNames;

        private SerialPort? selectedPort;

        public EventHandler<string>? OnDataDetected { get; set; }
        public EventHandler<bool>? OnDeviceStatus { get; set; }

        public SerialCommunicationUnitTest()
        {
            _portNames = new List<string>();
            _validPortNames = new List<string>();
        }

        public void Close()
        {

        }

        public void Initialize()
        {
            _portNames.Clear();
            string[] ports = new string[] { "COM3" };
            ports = ports.Distinct().ToArray();
            foreach (string port in ports)
            {
                _portNames.Add(port);
            }

            foreach (string pn in _portNames)
            {
                Thread.Sleep(250);
                _validPortNames.Add(pn);
            }

            if (_validPortNames.Count > 0)
            {
                selectedPort = GetSerialPort(_validPortNames[0]);
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
            return _random.Next(100) < 50;
        }

        public bool TurnOffAutoTest()
        {
            _timer?.Stop();
            _timer = null;
            Task.Delay(250).Wait();
            return true;
        }

        public bool TurnOnAutoTest()
        {
            Task.Delay(250).Wait();
            _dummyChannel = 0;
            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += OnGetDummyReading;
            _timer.AutoReset = true;
            _timer.Enabled = true;
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
            string completeMessage = _messageBuffer.ToString();
            _messageBuffer.Clear();
            OnDataDetected?.Invoke(this, $"PASS:{_dummyChannel}");

        }

        private void OnGetDummyReading(object? sender, System.Timers.ElapsedEventArgs e)
        {
            OnDataReceived(sender, null);
            _dummyChannel++;
            if (_dummyChannel > 111)
            {
                _timer?.Stop();
                _timer = null;
            }
        }
    }
}
