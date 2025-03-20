using LogManager;
using System.Windows;
using System.Windows.Threading;

namespace Connect112
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        ILogger? _logger;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _logger = new Logger(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NextGen Prototypes", "Connect 112", "log.txt"));

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            _logger.LogMessage("Application started");

            //ICommunication _comm = new SerialCommunication();
            ICommunication _comm = new SerialCommunicationUnitTest(); // testing class
            var mainWindow = new MainWindow();
            var mainViewModel = new MainViewModel(_logger, _comm);
            mainWindow.DataContext = mainViewModel;
            mainViewModel.ScrollRequested += mainWindow.OnScrollIntoViewRequested;
            mainWindow.Show();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger?.LogException(e.ExceptionObject as Exception);
            Environment.Exit(1);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogException(e.Exception);
            e.Handled = true;
            Shutdown();
        }
    }
}
