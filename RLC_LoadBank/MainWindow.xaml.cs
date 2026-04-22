using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RLC_LoadBank
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Btn_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            App app = (App)Application.Current;

            app.MainWindow = this;
            NaviFrame.Content = app.MainView;
        }

        private void Btn_SystemStatus_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_History_Click(object sender, RoutedEventArgs e)
        {

        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            App app = (App)Application.Current;

            MessageBoxResult msgResult = WinUIMessageBox.Show(
                GetWindow(app.MainWindow),
                "모니터링 중입니다.\r\n그래도 종료 하시겠습니까?",
                null,
                MessageBoxButton.YesNo,
                MessageBoxImage.None,
                MessageBoxResult.None,
                MessageBoxOptions.None,
                FloatingMode.Window);

            if (msgResult == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }
}
