using DevExpress.Xpf.Core;
using Microsoft.VisualBasic.Logging;
using NLog;
using RLC_LoadBank.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
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


        public readonly Logger nlog = LogManager.GetLogger("");
        override protected void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
        public App()
        {
            RegisterGlobalExceptionHandlers();
            CompatibilitySettings.UseLightweightThemes = true;

        }
        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException; // UI 이벤트/XAML 로딩 중 예외 (버튼클릭, Loaded 이벤트, 바인딩 후 실현되는 UI코드 등..)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // 앱 전체에서 최종 미처리 예외
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // await 안 한 Task 내부 예외
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
                // NLog 실패 시 아래 fallback 파일 기록으로 남긴다.
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
                // fallback 기록도 실패하면 더 이상 할 수 있는 작업이 없다.
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
                    "EMS 실행 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // MessageBox 표시 실패 시 무시한다.
            }
        }
        #endregion
    }
}
