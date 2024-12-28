using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Connect112.ChildViewModels
{
    public interface ITestConnection
    {
        string TestName { get; set; }

        IList<Pin> PinCollection { get; }

        void StartTest();

        void StopTest();

        void SelectNextPin();

        void ClearTest();

        EventHandler<TestState>? OnTestStateChanged { get; set; }
    }

    public class TestConnection : UpdateUI, ITestConnection
    {
        private const int TOTAL_PINS = 112;

        private bool _hasUserTestedAnyPins
        {
            get
            {
                return Pins?.Any(pin => (int)pin.PinResult > 0) == true;
            }
        }

        private int _pinsTestedCount
        {
            get { return Pins != null ? Pins.Where(p => p.PinResult > 0).Count() : 0; }
        }

        #region UI PROPS

        private string? _testName;
        public string? TestName
        {
            get { return _testName; }
            set
            {
                if (_testName != value)
                {
                    _testName = value;
                    OnPropertyChanged(nameof(TestName));
                }
            }
        }

        private TestState _testState;
        public TestState TestState
        {
            get { return _testState; }
            set
            {
                if (_testState != value)
                {
                    _testState = value;
                    OnPropertyChanged(nameof(TestState));
                }
            }
        }

        private string? _testButtonContent;

        public string? TestButtonContent
        {
            get { return _testButtonContent; }
            set
            {
                if (value != _testButtonContent)
                {
                    _testButtonContent = value;
                    OnPropertyChanged(nameof(TestButtonContent));
                }
            }
        }

        public IList<Pin> Pins { get; set; }

        private Pin? _selectedPin;
        public Pin? SelectedPin
        {
            get { return _selectedPin; }
            set
            {
                if (_selectedPin != value)
                {
                    _selectedPin = value;
                    OnPropertyChanged(nameof(SelectedPin));

                    if (TestState == TestState.Running)
                    {
                        TestButtonContent = _selectedPin != null ? $"Test Pin {_selectedPin.PinName}" : "Test";
                    }
                }
            }
        }

        public IList<Pin> PinCollection
        {
            get
            {
                return Pins != null ? Pins.ToList() : new List<Pin> { };
            }
        }

        #region BUTTON PROPS

        public ICommand TestPinButton { get; }

        #endregion

        #endregion

        #region EVENTS

        public EventHandler<TestState>? OnTestStateChanged { get; set; }

        #endregion

        #region CONSTRUCTOR(S)
        internal TestConnection(Action<object> executeTestPinAction)
        {
            Pins = new List<Pin>();
            LoadPins();

            TestPinButton = new RelayCommand(executeTestPinAction);
        }

        #endregion

        public void StartTest()
        {
            if (_hasUserTestedAnyPins)
            {
                LoadPins();
            }

            SelectedPin = Pins.First();
            TestButtonContent = $"Test Pin {SelectedPin.PinName}";
            TestState = TestState.Running;
            OnTestStateChanged?.Invoke(this, _testState);
        }

        public void StopTest()
        {
            if (_hasUserTestedAnyPins)
            {
                TestState = TestState.Aborted;
            }
            else
            {
                TestState = TestState.None;
            }

            OnTestStateChanged?.Invoke(this, _testState);
        }

        public void SelectNextPin()
        {
            if (_selectedPin != null)
            {
                int pinIndex = _selectedPin.PinIndex;
                if (pinIndex < Pins.Count - 1)
                {
                    SelectedPin = Pins[pinIndex + 1];
                }

                if (_pinsTestedCount == TOTAL_PINS)
                {
                    TestState = TestState.Completed;
                    OnTestStateChanged?.Invoke(this, _testState);
                }
            }
        }

        public void ClearTest()
        {
            LoadPins();
            TestName = string.Empty;
            TestState = TestState.None;
            OnTestStateChanged?.Invoke(this, _testState);
        }

        private void LoadPins()
        {
            Pins = new ObservableCollection<Pin>();
            for (int i = 0; i < TOTAL_PINS; i++)
            {
                Pins.Add(new Pin
                {
                    PinIndex = i,
                    PinName = (i + 1).ToString(),
                    PinResult = PinResult.Untested
                });
            }

            OnPropertyChanged(nameof(Pins));
        }

        #region BUTTON CLICK HANDERS


        #endregion
    }

    public class Pin : UpdateUI
    {
        public int PinIndex { get; set; }

        [DisplayName("Pin")]
        public string? PinName { get; set; }

        private PinResult _pinResult;

        [DisplayName("Test Result")]
        public PinResult PinResult
        {
            get { return _pinResult; }
            set
            {
                if (_pinResult != value)
                {
                    _pinResult = value;
                    OnPropertyChanged(nameof(PinResult));
                }
            }
        }
    }

    public enum TestState
    {
        None,
        Running,
        Aborted,
        Completed,
        ConnectionError
    }

    public enum PinResult
    {
        Untested,
        Pass,
        Fail
    }
}
