using System.Windows;

namespace Connect112
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void OnScrollIntoViewRequested(object? sender, int index)
        {
            if (index >= 0 && index < PinDataGrid.Items.Count)
            {
                PinDataGrid.ScrollIntoView(PinDataGrid.Items[index]);
            }
        }
    }
}