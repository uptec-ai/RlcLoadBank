using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RLC_LoadBank.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        internal static readonly Brush SuccessBrushValue = CreateBrush("#00B26F");
        internal static readonly Brush WarningBrushValue = CreateBrush("#E57A00");
        internal static readonly Brush DangerBrushValue = CreateBrush("#D92D20");
        internal static readonly Brush NeutralBrushValue = CreateBrush("#61758A");
        internal static readonly Brush ResistanceBrushValue = CreateBrush("#E24A3B");
        internal static readonly Brush ReactorBrushValue = CreateBrush("#F59E0B");
        internal static readonly Brush CapacitorBrushValue = CreateBrush("#3182F6");
        internal static readonly Brush TotalBrushValue = CreateBrush("#FF757575");

        private readonly DispatcherTimer _modbusPollTimer;
        private bool _isPolling;
        private string _plcStatusText;
        private string _mcSummaryText;
        private string _mcCommunicationText;
        private string _mcLastUpdateText;

        public MainViewModel()
        {
            PlcStatusText = "Standby";
            McSummaryText = "MC status waiting";
            McCommunicationText = "Modbus DI 0~49 not received";
            McLastUpdateText = "Last update -";

            StateBadges = new ObservableCollection<StatusBadgeItem>
            {
                new StatusBadgeItem("E-Stop", "Normal", SuccessBrushValue),
                new StatusBadgeItem("Alarm", "None", WarningBrushValue),
                new StatusBadgeItem("Heartbeat", "Rx", SuccessBrushValue)
            };

            ThreePhaseItems = new ObservableCollection<PhaseStatusItem>
            {
                new PhaseStatusItem("R", "저항 부하", "16 STEP", "kW", 200.0, 187.5, ResistanceBrushValue),
                new PhaseStatusItem("L", "리액터 부하", "16 STEP", "kvar", 200.0, 187.5, ReactorBrushValue),
                new PhaseStatusItem("C", "커패시터 부하", "2 STEP", "kvar", 130.0, 130.0, CapacitorBrushValue),
                new PhaseStatusItem(" 종합", "", "2 STEP", "kvar", 130.0, 130.0, TotalBrushValue)
            };

            AutoTargets = new ObservableCollection<AutoTargetItem>
            {
                new AutoTargetItem("R", 200.0, "kW", ResistanceBrushValue),
                new AutoTargetItem("L", 200.0, "kvar", ReactorBrushValue),
                new AutoTargetItem("C", 130.0, "kvar", CapacitorBrushValue)
            };

            AutoModeActuals = new ObservableCollection<AutoModeActualItem>
            {
                new AutoModeActualItem("R", 187.5, "kW", ResistanceBrushValue),
                new AutoModeActualItem("L", 187.5, "kvar", ReactorBrushValue),
                new AutoModeActualItem("C", 130.0, "kvar", CapacitorBrushValue)
            };

            AutoModeSummaryRows = new ObservableCollection<AutoModeSummaryRow>
            {
                new AutoModeSummaryRow("R (kW)", 200.0, 187.5),
                new AutoModeSummaryRow("L (kvar)", 200.0, 187.5),
                new AutoModeSummaryRow("C (kvar)", 130.0, 130.0)
            };

            LineDiagramRows = new ObservableCollection<RlcDiagramPhaseRow>(CreatePhaseRows());
            CapacitorNodes = new ObservableCollection<McStatusNode>(CreateCapacitorNodes());
            RefreshMcSummary();

            _modbusPollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _modbusPollTimer.Tick += OnModbusPollTimerTick;

            if (Application.Current != null)
            {
                _modbusPollTimer.Start();
            }
        }

        public string PlcStatusText
        {
            get => _plcStatusText;
            private set => SetProperty(ref _plcStatusText, value);
        }

        public ObservableCollection<StatusBadgeItem> StateBadges { get; }

        public ObservableCollection<PhaseStatusItem> ThreePhaseItems { get; }

        public ObservableCollection<AutoTargetItem> AutoTargets { get; }

        public ObservableCollection<AutoModeActualItem> AutoModeActuals { get; }

        public ObservableCollection<AutoModeSummaryRow> AutoModeSummaryRows { get; }

        public ObservableCollection<RlcDiagramPhaseRow> LineDiagramRows { get; }
        public ObservableCollection<McStatusNode> CapacitorNodes { get; }

        public string McSummaryText
        {
            get => _mcSummaryText;
            private set => SetProperty(ref _mcSummaryText, value);
        }

        public string McCommunicationText
        {
            get => _mcCommunicationText;
            private set => SetProperty(ref _mcCommunicationText, value);
        }

        public string McLastUpdateText
        {
            get => _mcLastUpdateText;
            private set => SetProperty(ref _mcLastUpdateText, value);
        }

        private async void OnModbusPollTimerTick(object sender, EventArgs e)
        {
            if (_isPolling)
            {
                return;
            }

            _isPolling = true;
            try
            {
                await PollDiscreteInputsAsync();
            }
            finally
            {
                _isPolling = false;
            }
        }

        private async Task PollDiscreteInputsAsync()
        {
            var app = Application.Current as App;
            if (app?.ModbusService == null)
            {
                ApplyDisconnectedState("service not initialized");
                return;
            }

            try
            {
                var bits = await app.ModbusService.ReadDiscreteInputsAsync(0, 50);
                ApplyDiscreteInputs(bits);
            }
            catch (Exception ex)
            {
                ApplyDisconnectedState(ex.Message);
            }
        }

        private void ApplyDiscreteInputs(IReadOnlyList<bool> bits)
        {
            if (bits.Count < 50)
            {
                ApplyDisconnectedState("insufficient discrete input length");
                return;
            }

            foreach (var row in LineDiagramRows)
            {
                foreach (var node in row.Nodes)
                {
                    node.SetStatus(bits[node.Address], false);
                }

                row.RefreshSummary();
            }

            foreach (var node in CapacitorNodes)
            {
                node.SetStatus(bits[node.Address], false);
            }

            PlcStatusText = "Connected";
            McCommunicationText = "Modbus DI 0~49 online";
            McLastUpdateText = $"Last update {DateTime.Now:HH:mm:ss}";
            StateBadges[0].SetValue("Normal", SuccessBrushValue);
            StateBadges[1].SetValue("None", WarningBrushValue);
            StateBadges[2].SetValue("Rx", SuccessBrushValue);
            RefreshMcSummary();
        }

        private void ApplyDisconnectedState(string message)
        {
            foreach (var row in LineDiagramRows)
            {
                foreach (var node in row.Nodes)
                {
                    node.SetStatus(false, true);
                }

                row.RefreshSummary();
            }

            foreach (var node in CapacitorNodes)
            {
                node.SetStatus(false, true);
            }

            PlcStatusText = "Standby";
            McCommunicationText = $"Modbus wait: {message}";
            McLastUpdateText = $"Last try {DateTime.Now:HH:mm:ss}";
            StateBadges[0].SetValue("Wait", NeutralBrushValue);
            StateBadges[1].SetValue("Wait", WarningBrushValue);
            StateBadges[2].SetValue("No Rx", DangerBrushValue);
            RefreshMcSummary();
        }

        private void RefreshMcSummary()
        {
            var allNodes = LineDiagramRows.SelectMany(row => row.Nodes).Concat(CapacitorNodes).ToList();
            var activeCount = allNodes.Count(node => node.IsActive);
            var unavailableCount = allNodes.Count(node => node.IsUnavailable);
            McSummaryText = $"ON {activeCount} / TOTAL {allNodes.Count} | WAIT {unavailableCount}";
        }

        private static IEnumerable<RlcDiagramPhaseRow> CreatePhaseRows()
        {
            var rows = new List<RlcDiagramPhaseRow>();
            var capacityPattern = new[] { 0.83, 0.83, 1.67, 3.33, 5.0, 5.0, 8.33, 10.0 };
            var phaseLabels = new[] { "L1", "L2", "L3" };

            for (var phaseIndex = 0; phaseIndex < phaseLabels.Length; phaseIndex++)
            {
                var nodes = new List<McStatusNode>();

                for (var stepIndex = 0; stepIndex < capacityPattern.Length; stepIndex++)
                {
                    nodes.Add(new McStatusNode(
                        $"R{stepIndex + 1}", // Label
                        (ushort)(phaseIndex * 16 + stepIndex), // Address
                        $"{capacityPattern[stepIndex]:0.##} kW", // Capacity text
                        ResistanceBrushValue)); // Accent brush
                }

                for (var stepIndex = 0; stepIndex < capacityPattern.Length; stepIndex++)
                {
                    nodes.Add(new McStatusNode(
                        $"L{stepIndex + 1}", // Label
                        (ushort)(phaseIndex * 16 + 8 + stepIndex), // Address
                        $"{capacityPattern[stepIndex]:0.##} kvar", // Capacity text
                        ReactorBrushValue)); // Accent brush
                }

                switch (phaseIndex)
                {
                    case 0:
                        rows.Add(new RlcDiagramPhaseRow(phaseLabels[phaseIndex], nodes, 9, 75, true, 51,70,50));
                        break;
                    case 1:
                        rows.Add(new RlcDiagramPhaseRow(phaseLabels[phaseIndex], nodes, 9, 75,false,0,0,0));
                        break;
                    case 2:
                        rows.Add(new RlcDiagramPhaseRow(phaseLabels[phaseIndex], nodes, 0, 0, false,0,0,0));
                        break;
                }
                
            }

            return rows;
        }

        private static IEnumerable<McStatusNode> CreateCapacitorNodes()
        {
            return new[]
            {
                new McStatusNode(
                    "C1", 
                    48, 
                    "130 kvar", 
                    CapacitorBrushValue),
                new McStatusNode(
                    "C2", 
                    49, 
                    "130 kvar", 
                    CapacitorBrushValue)
            };
        }

        internal static Brush CreateBrush(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }
    }

    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusBadgeItem : ObservableObject
    {
        private string _value;
        private Brush _indicatorBrush;
        private Brush _backgroundBrush;
        private Brush _borderBrush;

        public StatusBadgeItem(string label, string value, Brush accentBrush)
        {
            Label = label;
            SetValue(value, accentBrush);
        }

        public string Label { get; }

        public string Value
        {
            get => _value;
            private set => SetProperty(ref _value, value);
        }

        public Brush IndicatorBrush
        {
            get => _indicatorBrush;
            private set => SetProperty(ref _indicatorBrush, value);
        }

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            private set => SetProperty(ref _backgroundBrush, value);
        }

        public Brush BorderBrush
        {
            get => _borderBrush;
            private set => SetProperty(ref _borderBrush, value);
        }

        public void SetValue(string value, Brush accentBrush)
        {
            Value = value;
            IndicatorBrush = accentBrush;
            BackgroundBrush = CreateTint(accentBrush, 0.14);
            BorderBrush = CreateTint(accentBrush, 0.32);
        }

        private static Brush CreateTint(Brush baseBrush, double alpha)
        {
            var solid = (SolidColorBrush)baseBrush;
            var tinted = new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), solid.Color.R, solid.Color.G, solid.Color.B));
            tinted.Freeze();
            return tinted;
        }
    }

    public class PhaseStatusItem
    {
        public PhaseStatusItem(string title, string description, string stepText, string unit, double targetValue, double actualValue, Brush accentBrush)
        {
            Title = title;
            Description = description;
            StepText = stepText;
            Unit = unit;
            TargetValue = targetValue;
            ActualValue = actualValue;
            AccentBrush = accentBrush;
        }

        public string Title { get; }
        public string Description { get; }
        public string StepText { get; }
        public string Unit { get; }
        public double TargetValue { get; set; }
        public double ActualValue { get; set; }
        public Brush AccentBrush { get; }
        public double UsagePercent => TargetValue <= 0 ? 0 : Math.Round(ActualValue / TargetValue * 100.0, 1);
    }

    public class AutoTargetItem : ObservableObject
    {
        private double _targetValue;

        public AutoTargetItem(string label, double targetValue, string unit, Brush accentBrush)
        {
            Label = label;
            _targetValue = targetValue;
            Unit = unit;
            AccentBrush = accentBrush;
        }

        public string Label { get; }
        public string Unit { get; }
        public Brush AccentBrush { get; }

        public double Value => TargetValue;

        public double TargetValue
        {
            get => _targetValue;
            set
            {
                if (SetProperty(ref _targetValue, value))
                {
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
    }

    public class AutoModeActualItem
    {
        public AutoModeActualItem(string label, double value, string unit, Brush accentBrush)
        {
            Label = label;
            Value = value;
            Unit = unit;
            AccentBrush = accentBrush;
        }

        public string Label { get; }
        public double Value { get; }
        public string Unit { get; }
        public Brush AccentBrush { get; }
    }

    public class AutoModeSummaryRow
    {
        public AutoModeSummaryRow(string category, double targetValue, double actualValue)
        {
            Category = category;
            TargetValue = targetValue;
            ActualValue = actualValue;
        }

        public string Category { get; }
        public double TargetValue { get; }
        public double ActualValue { get; }
        public double ErrorValue => Math.Round(ActualValue - TargetValue, 1);
        public string UsageText => TargetValue <= 0 ? "0.0%" : $"{ActualValue / TargetValue * 100.0:0.0}%";
    }

    public class RlcDiagramPhaseRow : ObservableObject
    {
        private string _summaryText;

        public RlcDiagramPhaseRow(string phaseLabel, IEnumerable<McStatusNode> nodes, double verticalInputY, double verticalArriveY, bool visibleEllipse, double connX1, double connX2, double connY)
        {
            PhaseLabel = phaseLabel;
            Nodes = new ObservableCollection<McStatusNode>(nodes);
            VerY1 = verticalInputY;
            VerY2 = verticalArriveY;
            VisibleEllipse = visibleEllipse;
            ConnX1 = connX1;
            ConnX2 = connX2;
            ConnY = connY;
            
            RefreshSummary();
        }
        
        public string PhaseLabel { get; }
        // vertical line
        public double VerY1 { get; }
        public double VerY2 { get; }
        // ellipse
        public bool VisibleEllipse { get; }
        // connection line
        public double ConnX1 { get; }
        public double ConnX2 { get; }
        public double ConnY { get; }


        public ObservableCollection<McStatusNode> Nodes { get; }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        public void RefreshSummary()
        {
            SummaryText = $"ON {Nodes.Count(node => node.IsActive)} / {Nodes.Count}";
        }
    }

    public class McStatusNode : ObservableObject
    {
        private bool _isActive;
        private bool _isUnavailable = true;

        public McStatusNode(string label, ushort address, string capacityText, Brush accentBrush)
        {
            Label = label;
            Address = address;
            CapacityText = capacityText;
            AccentBrush = accentBrush;
        }

        public string Label { get; }
        public ushort Address { get; }
        public string CapacityText { get; }
        public Brush AccentBrush { get; }

        public bool IsActive
        {
            get => _isActive;
            private set => SetProperty(ref _isActive, value);
        }

        public bool IsUnavailable
        {
            get => _isUnavailable;
            private set => SetProperty(ref _isUnavailable, value);
        }

        public Brush RingBrush => IsUnavailable ? MainViewModel.CreateBrush("#D0D7DE") : (IsActive ? AccentBrush : MainViewModel.CreateBrush("#AAB7C5"));

        public Brush FillBrush => IsUnavailable ? MainViewModel.CreateBrush("#F3F5F7") : (IsActive ? AccentBrush : Brushes.White);

        public Brush LabelBrush
        {
            get
            {
                if (IsUnavailable)
                {
                    return MainViewModel.CreateBrush("#98A2B3");
                }

                return IsActive ? Brushes.White : MainViewModel.CreateBrush("#344054");
            }
        }

        public string ToolTipText => $"{Label} | DI {Address} | {CapacityText}";

        public void SetStatus(bool isActive, bool isUnavailable)
        {
            var activeChanged = SetProperty(ref _isActive, isActive, nameof(IsActive));
            var unavailableChanged = SetProperty(ref _isUnavailable, isUnavailable, nameof(IsUnavailable));

            if (activeChanged || unavailableChanged)
            {
                OnPropertyChanged(nameof(RingBrush));
                OnPropertyChanged(nameof(FillBrush));
                OnPropertyChanged(nameof(LabelBrush));
            }
        }
    }
}
