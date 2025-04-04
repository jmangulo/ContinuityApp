﻿using Connect112.ChildViewModels;
using LogManager;
using System.Windows;
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

        private bool _debounce;

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

        private string? _selectedAutoOption;
        public string? SelectedAutoOption
        {
            get { return _selectedAutoOption; }
            set
            {
                if (_selectedAutoOption != value)
                {
                    _selectedAutoOption = value;
                    OnPropertyChanged(nameof(SelectedAutoOption));
                    SwitchAutoTestFeature(value == GetTestTypeString(TestType.Auto));
                }
            }
        }

        public ITestConnection TestConnection { get { return _testConnection; } }

        #region BUTTON PROPS AND EVENT BINDINGS

        public ICommand ConnectButton { get; }

        public ICommand StartTestButton { get; }

        public ICommand StopTestButton { get; }

        public ICommand TestPinButton { get; }

        public ICommand ClearButton { get; }

        public ICommand ExportButton { get; }

        public ICommand SelectionChangedEvent { get; }

        public ICommand PreviewKeyDownEvent { get; }

        public ICommand OnClosingApplicationEvent { get; }

        #endregion

        #endregion

        #region EVENTS

        public event EventHandler<int>? ScrollRequested;

        #endregion

        #region CONSTRUCTOR(S)

        public MainViewModel(ILogger logger, ICommunication comm)
        {
            _logger = logger;

            _comm = comm;
            _comm.OnDeviceStatus += OnDeviceUpdateConnectionStatus;
            _comm.OnDataDetected += OnDataReceived;
            _comm.Initialize();
            UpdateHeaderAfterStateChange(_comm.IsDeviceFound() ? TestState.None : TestState.ConnectionError);

            _debounce = false;

            SelectedAutoOption = GetTestTypeString(TestType.Manual);

            PinAction pinActions = new PinAction(_comm.TurnOnAutoTest, _comm.TurnOffAutoTest, TestPin);
            _testConnection = new TestConnection(pinActions);
            _testConnection.OnTestStateChanged += OnTestStateChangedHandled;

            ConnectButton = new RelayCommand(ConnectButtonClick);
            StartTestButton = new RelayCommand(StartTestButtonClick);
            StopTestButton = new RelayCommand(StopTestButtonClick);
            TestPinButton = new RelayCommand(TestPinButtonClick);
            ClearButton = new RelayCommand(ClearButtonClick);
            ExportButton = new RelayCommand(ExportButtonClick);
            SelectionChangedEvent = new RelayCommand(OnSelectionChangedHandled);
            PreviewKeyDownEvent = new RelayCommand(PreviewKeyDownEventHandled);
            OnClosingApplicationEvent = new RelayCommand(OnClosingApplicationEventHandled);
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
            if (_debounce)
            {
                return;
            }

            _debounce = true;
            _testConnection.StartTest();
            _comm.Open();
            if (GetTestType(_selectedAutoOption) == TestType.Auto)
            {
                _comm.TurnOnAutoTest();
            }
            else
            {
                _comm.TurnOffAutoTest();
            }
            _debounce = false;
        }

        private void StopTestButtonClick(object parameter)
        {
            if (_debounce)
            {
                return;
            }

            _debounce = true;

            var result = MessageBox.Show($"Are you sure you want to stop test, {TestConnection.TestName}? Doing so will abort the test and you will need to start over.", "Stop Test?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (!result.Equals(MessageBoxResult.Yes))
            {
                _debounce = false;
                return;
            }

            _testConnection.StopTest();
            _comm.TurnOffAutoTest();
            _comm.Close();
            _debounce = false;
        }

        private void ExportButtonClick(object parameter)
        {
            if (_debounce)
            {
                return;
            }

            _debounce = true;
            ExportViewModel.ExportToCSV(_testConnection.TestName, _testConnection.PinCollection);
            _debounce = false;
        }

        private void TestPinButtonClick(object parameter)
        {
            if (_debounce)
            {
                return;
            }

            _debounce = true;

            if (parameter is Pin pin)
            {

                _testConnection.TestPinConnection(pin);
            }

            _debounce = false;
        }

        private void ClearButtonClick(object parameter)
        {
            _comm.Reset();
            _testConnection.ClearTest();
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

        private void OnDataReceived(object? sender, string data)
        {
            if (data?.Contains("PASS:", StringComparison.CurrentCultureIgnoreCase) == true)
            {
                string[] splitResponse = data.Split(new char[] { ':' });
                if (splitResponse.Length > 1 && int.TryParse(splitResponse[1], out int pinIndex))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _testConnection.RegisterPinSuccess(pinIndex);
                        ScrollRequested?.Invoke(this, pinIndex);
                    });
                }
            }
        }

        private void OnSelectionChangedHandled(object parameter)
        {
            if (parameter is DataGrid grid)
            {
                if (grid.SelectedItem is not null)
                {
                    grid.ScrollIntoView(grid.SelectedItem);
                }
            }
        }

        private void PreviewKeyDownEventHandled(object parameter)
        {
            if (_selectedAutoOption == GetTestTypeString(TestType.Auto))
            {
                return;
            }

            if (parameter is KeyEventArgs key)
            {
                if (key.Key == Key.Enter)
                {
                    if (_debounce)
                    {
                        return;
                    }
                    _debounce = true;
                    DataGrid source = (DataGrid)key.Source;
                    _testConnection.TestPinConnection((Pin)source.SelectedItem);
                    _debounce = false;
                }
            }
        }

        private void OnClosingApplicationEventHandled(object parameter)
        {
            if (parameter is System.ComponentModel.CancelEventArgs e)
            {
                bool cancelClosing = false;
                if (_testConnection.TestState == TestState.Running)
                {
                    var response = MessageBox.Show("Are you sure you want to close down the application? You are currently running a test.", "Close Application?", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                    cancelClosing = !response.Equals(MessageBoxResult.Yes);
                }

                e.Cancel = cancelClosing;
            }
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

        private bool TestPin(int pinIndex)
        {
            return _comm.TestPin(pinIndex);
        }

        private void SwitchAutoTestFeature(bool enable)
        {
            if (enable)
            {
                _comm.TurnOnAutoTest();
            }
            else
            {
                _comm.TurnOffAutoTest();
            }
        }

        private string GetTestTypeString(TestType testType)
        {
            string? name = Enum.GetName(typeof(TestType), testType);

            return !string.IsNullOrEmpty(name) ? name : "";
        }

        private TestType GetTestType(string? testTypeName)
        {
            return (TestType)Enum.Parse(typeof(TestType), testTypeName ?? "");
        }
    }

    public enum TestType
    {
        Manual,
        Auto
    }
}
