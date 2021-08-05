using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BasicLogger.jnet.systems;

namespace TestLogger
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

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogEvent("Test Log", Logger.LogLevel.Info);
            Logger.LogEvent("Test Log", Logger.LogLevel.Error);
            Logger.LogEvent("Test Log", Logger.LogLevel.Critical);
            Logger.LogEvent("Test Log", Logger.LogLevel.System);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger.Load();
        }
    }
}
