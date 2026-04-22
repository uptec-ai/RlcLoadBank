using DevExpress.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace RLC_LoadBank.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            PlcStatusText = "통신 정상";
            PlcStatusBrush = CreateBrush("#2F9E6D");
            CurrentDateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            StateBadges = new ObservableCollection<StatusBadgeItem>
            {
                new StatusBadgeItem("비상정지", "대기", "#F3F5F7", "#D92D20", "#FDECEC"),
                new StatusBadgeItem("경보", "정상", "#F3F5F7", "#F08C00", "#FFF5E8"),
                new StatusBadgeItem("Heartbeat", "정상", "#ECFDF3", "#2F9E6D", "#ECFDF3")
            };

            ThreePhaseItems = new ObservableCollection<PhaseStatusItem>
            {
                new PhaseStatusItem("R", "저항 부하", "kW", 200.0, 187.5, 93.8, "현재 구성 16 STEP", "#D92D20"),
                new PhaseStatusItem("L", "리액터 부하", "kvar", 200.0, 187.5, 93.8, "현재 구성 16 STEP", "#F08C00"),
                new PhaseStatusItem("C", "콘덴서 부하", "kvar", 130.0, 130.0, 100.0, "현재 구성 2 STEP", "#1570EF"),
                new PhaseStatusItem("합계", "전체 용량", "kVA", 530.0, 505.0, 95.3, "R/L/C 합산 상태", "#0F8B94")
            };

            AutoTargets = new ObservableCollection<AutoTargetItem>
            {
                new AutoTargetItem("R", "kW", "200.0", "#D92D20"),
                new AutoTargetItem("L", "kvar", "200.0", "#F08C00"),
                new AutoTargetItem("C", "kvar", "130.0", "#1570EF"),
                new AutoTargetItem("PF", string.Empty, "0.95", "#0F8B94")
            };

            AutoModeActuals = new ObservableCollection<AutoModeActualItem>
            {
                new AutoModeActualItem("R", "187.5", "kW", "#D92D20"),
                new AutoModeActualItem("L", "187.5", "kvar", "#F08C00"),
                new AutoModeActualItem("C", "130.0", "kvar", "#1570EF")
            };

            AutoModeSummaryRows = new ObservableCollection<AutoModeSummaryRow>
            {
                new AutoModeSummaryRow("R (kW)", 200.0, 187.5, -12.5, "93.8 %"),
                new AutoModeSummaryRow("L (kvar)", 200.0, 187.5, -12.5, "93.8 %"),
                new AutoModeSummaryRow("C (kvar)", 130.0, 130.0, 0.0, "100.0 %"),
                new AutoModeSummaryRow("합계", 530.0, 505.0, -25.0, "95.3 %")
            };
        }

        public string PlcStatusText { get; }
        public Brush PlcStatusBrush { get; }
        public string CurrentDateTimeText { get; }
        public ObservableCollection<StatusBadgeItem> StateBadges { get; }
        public ObservableCollection<PhaseStatusItem> ThreePhaseItems { get; }
        public ObservableCollection<AutoTargetItem> AutoTargets { get; }
        public ObservableCollection<AutoModeActualItem> AutoModeActuals { get; }
        public ObservableCollection<AutoModeSummaryRow> AutoModeSummaryRows { get; }

        private static SolidColorBrush CreateBrush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }
    }

    public class StatusBadgeItem
    {
        public StatusBadgeItem(string label, string value, string borderHex, string indicatorHex, string backgroundHex)
        {
            Label = label;
            Value = value;
            BorderBrush = MainViewModelBrushes.Create(borderHex);
            IndicatorBrush = MainViewModelBrushes.Create(indicatorHex);
            BackgroundBrush = MainViewModelBrushes.Create(backgroundHex);
        }

        public string Label { get; }
        public string Value { get; }
        public Brush BorderBrush { get; }
        public Brush IndicatorBrush { get; }
        public Brush BackgroundBrush { get; }
    }

    public class PhaseStatusItem
    {
        public PhaseStatusItem(string title, string description, string unit, double targetValue, double actualValue, double usagePercent, string stepText, string accentHex)
        {
            Title = title;
            Description = description;
            Unit = unit;
            TargetValue = targetValue;
            ActualValue = actualValue;
            UsagePercent = usagePercent;
            StepText = stepText;
            AccentBrush = MainViewModelBrushes.Create(accentHex);
        }

        public string Title { get; }
        public string Description { get; }
        public string Unit { get; set; }
        public double TargetValue { get; set; }
        public double ActualValue { get; set; }
        public double UsagePercent { get; set; }
        public string StepText { get; }
        public Brush AccentBrush { get; }
    }

    public class AutoTargetItem
    {
        public AutoTargetItem(string label, string unit, string value, string accentHex)
        {
            Label = label;
            Unit = unit;
            Value = value;
            AccentBrush = MainViewModelBrushes.Create(accentHex);
        }

        public string Label { get; set; }
        public string Unit { get; set; }
        public string Value { get; set; }
        public Brush AccentBrush { get; set; }
    }

    public class AutoModeActualItem
    {
        public AutoModeActualItem(string label, string value, string unit, string accentHex)
        {
            Label = label;
            Value = value;
            Unit = unit;
            AccentBrush = MainViewModelBrushes.Create(accentHex);
        }

        public string Label { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public Brush AccentBrush { get; set; }
    }

    public class AutoModeSummaryRow
    {
        public AutoModeSummaryRow(string category, double targetValue, double actualValue, double errorValue, string usageText)
        {
            Category = category;
            TargetValue = targetValue;
            ActualValue = actualValue;
            ErrorValue = errorValue;
            UsageText = usageText;
        }

        public string Category { get; set; }
        public double TargetValue { get; set; }
        public double ActualValue { get; set; }
        public double ErrorValue { get; set; }
        public string UsageText { get; set; }
    }

    internal static class MainViewModelBrushes
    {
        public static SolidColorBrush Create(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }
    }
}
