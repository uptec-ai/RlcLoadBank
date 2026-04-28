using DevExpress.Xpf.Core;
using NLog;
using RLC_LoadBank.Models.Managers;
using RLC_LoadBank.Models.Modbus;
using RLC_LoadBank.Protocols.Modbus;
using RLC_LoadBank.Services.Modbus;
using RLC_LoadBank.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RLC_LoadBank
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MainView MainView { get; private set; }

        public StatusManager StatusManager { get; private set; }
        public IModbusService ModbusService { get; private set; }
        public ModbusProtocolDefinition RlcModbusProtocol { get; private set; }

        public readonly Logger nlog = LogManager.GetLogger(string.Empty);

        protected override void OnStartup(StartupEventArgs e)
        {
            RlcModbusProtocol = RlcModbusProtocolDefinition.CreateDefault();
            ModbusService = new ModbusTcpService(RlcModbusProtocol, RlcModbusProtocol.DefaultEndpoint);

            StatusManager = new StatusManager();
            StatusManager.Init();
            
            MainView = new MainView();

            base.OnStartup(e);
        }

        public App()
        {
            RegisterGlobalExceptionHandlers();
            CompatibilitySettings.UseLightweightThemes = true;
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        #region Exception Handling
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogFatalException("DispatcherUnhandledException", e.Exception);
            ShowFatalMessage(e.Exception);
            e.Handled = true;
            Shutdown(-1);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception");
            LogFatalException("AppDomain.CurrentDomain.UnhandledException", ex);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogFatalException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private void LogFatalException(string source, Exception ex)
        {
            try
            {
                nlog.Error(ex, $"Fatal exception at {source}");
            }
            catch
            {
            }

            try
            {
                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "fatal_startup.txt");
                var lines =
                    "==============================================================================================" + Environment.NewLine +
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}]" + Environment.NewLine +
                    ex + Environment.NewLine;

                File.AppendAllText(logPath, lines);
            }
            catch
            {
            }
        }

        private static void ShowFatalMessage(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "프로그램 실행 중 치명적인 오류가 발생했습니다." + Environment.NewLine +
                    "logs\\fatal_startup.txt 파일을 확인해주세요." + Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "RLC LoadBank 실행 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ModbusService?.Dispose();
            base.OnExit(e);
        }
        #endregion
    }
}
