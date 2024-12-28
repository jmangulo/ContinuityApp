using Connect112.ChildViewModels;
using LogManager;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Connect112
{
    public class MainViewModel : UpdateUI
    {
        private readonly ILogger _logger;
        private readonly ICommunication _comm;
        private readonly ITestConnection _testConnection;

        #region UI PROPS

        private bool _isDeviceFound;
        public bool IsDeviceFound
        {
            get { return _isDeviceFound; }
            set
            {
                if (_isDeviceFound != value)
                {
                    _isDeviceFound = value;
                    OnPropertyChanged(nameof(IsDeviceFound));
                }
            }
        }

        private string? _testStateHeader;

        public string? TestStateHeader
        {
            get { return _testStateHeader; }
            set
            {
                if (_testStateHeader != value)
                {
                    _testStateHeader = value;
                    OnPropertyChanged(nameof(TestStateHeader));
                }
            }
        }

        private Brush? _testHeaderBackgroundBrush;
        public Brush? TestHeaderBackgroundBrush
        {
            get { return _testHeaderBackgroundBrush; }
            set
            {
                if (value != _testHeaderBackgroundBrush)
                {
                    _testHeaderBackgroundBrush = value;
                    OnPropertyChanged(nameof(TestHeaderBackgroundBrush));
                }
            }
        }

        private Brush? _testHeaderForegroundBrush;
        public Brush? TestHeaderForegroundBrush
        {
            get { return _testHeaderForegroundBrush; }
            set
            {
                if (value != _testHeaderForegroundBrush)
                {
                    _testHeaderForegroundBrush = value;
                    OnPropertyChanged(nameof(TestHeaderForegroundBrush));
                }
            }
        }

        public ITestConnection TestConnection { get { return _testConnection; } }

        #region BUTTON PROPS AND EVENT BINDINGS

        public ICommand ConnectButton { get; }

        public ICommand StartTestButton { get; }

        public ICommand StopTestButton { get; }

        public ICommand ExportButton { get; }

        public ICommand SelectionChangedEvent { get; }

        public ICommand PreviewKeyDownEvent { get; }

        #endregion

        #endregion

        #region CONSTRUCTOR(S)
        public MainViewModel(ILogger logger, ICommunication comm)
        {
            _logger = logger;

            _comm = comm;
            _comm.OnDeviceStatus += OnDeviceUpdateConnectionStatus;
            _comm.Initialize();
            UpdateHeaderAfterStateChange(_comm.IsDeviceFound() ? TestState.None : TestState.ConnectionError);

            _testConnection = new TestConnection(TestPinButtonClick);
            _testConnection.OnTestStateChanged += OnTestStateChangedHandled;

            ConnectButton = new RelayCommand(ConnectButtonClick);
            StartTestButton = new RelayCommand(StartTestButtonClick);
            StopTestButton = new RelayCommand(StopTestButtonClick);
            ExportButton = new RelayCommand(ExportButtonClick);
            SelectionChangedEvent = new RelayCommand(OnSelectionChangedHandled);
            PreviewKeyDownEvent = new RelayCommand(PreviewKeyDownEventHandled);
        }

        #endregion

        #region BUTTON CLICK HANDLERS

        private void ConnectButtonClick(object parameter)
        {
            _comm.Initialize();
            IsDeviceFound = _comm.IsDeviceFound();
            UpdateHeaderAfterStateChange(IsDeviceFound ? TestState.None : TestState.ConnectionError);
        }

        private void StartTestButtonClick(object parameter)
        {
            _testConnection.StartTest();
            _comm.Open();
        }

        private void StopTestButtonClick(object parameter)
        {
            _testConnection.StopTest();
            _comm.Close();
        }

        private void ExportButtonClick(object parameter)
        {

        }

        private void TestPinButtonClick(object parameter)
        {
            if (parameter is Pin pin)
            {
                TestPinConnection(pin);
            }
        }

        private void OnSelectionChangedHandled(object parameter)
        {
            if (parameter is DataGrid grid)
            {
                grid.ScrollIntoView(grid.SelectedItem);
            }
        }

        private void PreviewKeyDownEventHandled(object parameter)
        {
            if (parameter is KeyEventArgs key)
            {
                if (key.Key == Key.Enter)
                {
                    DataGrid source = (DataGrid)key.Source;
                    TestPinConnection((Pin)source.SelectedItem);
                }
            }
        }

        #endregion

        #region EVENT SUBSCRIPTIONS

        private void OnDeviceUpdateConnectionStatus(object? sender, bool status)
        {
            IsDeviceFound = status;
        }

        private void OnTestStateChangedHandled(object? sender, TestState testState)
        {
            UpdateHeaderAfterStateChange(testState);
        }

        #endregion

        private void UpdateHeaderAfterStateChange(TestState testState)
        {
            var colors = GetBannerColors(testState);

            switch (testState)
            {
                case TestState.None:
                    TestStateHeader = "Ready";
                    TestHeaderBackgroundBrush = colors.background;
                    TestHeaderForegroundBrush = colors.foreground;
                    break;

                case TestState.Running:
                    TestStateHeader = "Testing in progress";
                    TestHeaderBackgroundBrush = colors.background;
                    TestHeaderForegroundBrush = colors.foreground;
                    break;

                case TestState.Aborted:
                    TestStateHeader = "Test Aborted";
                    TestHeaderBackgroundBrush = colors.background;
                    TestHeaderForegroundBrush = colors.foreground;
                    break;

                case TestState.Completed:
                    TestStateHeader = "Test Completed";
                    TestHeaderBackgroundBrush = colors.background;
                    TestHeaderForegroundBrush = colors.foreground;
                    break;

                case TestState.ConnectionError:
                    TestStateHeader = "No device found";
                    TestHeaderBackgroundBrush = colors.background;
                    TestHeaderForegroundBrush = colors.foreground;
                    break;
            }
        }

        private (SolidColorBrush background, SolidColorBrush foreground) GetBannerColors(TestState testState)
        {
            SolidColorBrush background;
            SolidColorBrush foreground;

            switch (testState)
            {
                case TestState.Running:
                    foreground = Brushes.Black;
                    background = new SolidColorBrush(Color.FromRgb(176, 238, 151));
                    break;

                case TestState.Aborted:
                    foreground = Brushes.Black;
                    background = new SolidColorBrush(Color.FromRgb(236, 236, 236));
                    break;

                case TestState.Completed:
                    foreground = Brushes.Black;
                    background = new SolidColorBrush(Color.FromRgb(176, 238, 151));
                    break;

                case TestState.ConnectionError:
                    foreground = Brushes.White;
                    background = new SolidColorBrush(Color.FromRgb(45, 40, 40));
                    break;

                default:
                    foreground = Brushes.Black;
                    background = new SolidColorBrush(Color.FromRgb(255, 200, 117));
                    break;
            }

            return (background, foreground);
        }


        private void TestPinConnection(Pin pin)
        {
            int pinNumber = pin.PinIndex;
            pin.PinResult = _comm.TestPin(pinNumber) ? PinResult.Pass : PinResult.Fail;
            TestConnection.SelectNextPin();
        }
    }
}
