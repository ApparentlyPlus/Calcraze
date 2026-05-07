using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// Much easier than the main window VM, just holds the settings and builds 
// a config based on them when requested

namespace Calcraze.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly string[] Presets =
            [ "Easy", "Balanced", "Hard", "Custom", ];

        public static IReadOnlyList<string> PresetOptions => Presets;
        private string _selectedPresetName = "Balanced";
        public string SelectedPresetName
        {
            get => _selectedPresetName;
            set
            {
                if (_selectedPresetName == value) return;
                _selectedPresetName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomPreset));
            }
        }

        public bool IsCustomPreset => SelectedPresetName == "Custom";

        private int _roundDurationSeconds = 60;
        public int RoundDurationSeconds
        {
            get => _roundDurationSeconds;
            set
            {
                if (_roundDurationSeconds == value) return;
                _roundDurationSeconds = value;
                OnPropertyChanged();
            }
        }

        private int _customMaxDepth = ExpressionConfig.Balanced.MaxDepth;
        public int CustomMaxDepth
        {
            get => _customMaxDepth;
            set { _customMaxDepth = value; OnPropertyChanged(); }
        }

        private int _customTargetMin = ExpressionConfig.Balanced.TargetMin;
        public int CustomTargetMin
        {
            get => _customTargetMin;
            set { _customTargetMin = value; OnPropertyChanged(); }
        }

        private int _customTargetMax = ExpressionConfig.Balanced.TargetMax;
        public int CustomTargetMax
        {
            get => _customTargetMax;
            set { _customTargetMax = value; OnPropertyChanged(); }
        }

        private int _customLeafMin = ExpressionConfig.Balanced.LeafMin;
        public int CustomLeafMin
        {
            get => _customLeafMin;
            set { _customLeafMin = value; OnPropertyChanged(); }
        }

        private int _customLeafMax = ExpressionConfig.Balanced.LeafMax;
        public int CustomLeafMax
        {
            get => _customLeafMax;
            set { _customLeafMax = value; OnPropertyChanged(); }
        }

        private bool _customAllowNegative = ExpressionConfig.Balanced.AllowNegative;
        public bool CustomAllowNegative
        {
            get => _customAllowNegative;
            set { _customAllowNegative = value; OnPropertyChanged(); }
        }

        private int _customAddWeight = ExpressionConfig.Balanced.AddWeight;
        public int CustomAddWeight
        {
            get => _customAddWeight;
            set { _customAddWeight = value; OnPropertyChanged(); }
        }

        private int _customSubtractWeight = ExpressionConfig.Balanced.SubtractWeight;
        public int CustomSubtractWeight
        {
            get => _customSubtractWeight;
            set { _customSubtractWeight = value; OnPropertyChanged(); }
        }

        private int _customMultiplyWeight = ExpressionConfig.Balanced.MultiplyWeight;
        public int CustomMultiplyWeight
        {
            get => _customMultiplyWeight;
            set { _customMultiplyWeight = value; OnPropertyChanged(); }
        }

        private int _customDivideWeight = ExpressionConfig.Balanced.DivideWeight;
        public int CustomDivideWeight
        {
            get => _customDivideWeight;
            set { _customDivideWeight = value; OnPropertyChanged(); }
        }

        private double _customLeafBias = ExpressionConfig.Balanced.LeafBias;
        public double CustomLeafBias
        {
            get => _customLeafBias;
            set { _customLeafBias = value; OnPropertyChanged(); }
        }

        public ExpressionConfig BuildConfig()
        {
            return SelectedPresetName switch
            {
                "Easy" => ExpressionConfig.Easy.Copy(),
                "Hard" => ExpressionConfig.Hard.Copy(),
                "Custom" => BuildCustomConfig(),
                _ => ExpressionConfig.Balanced.Copy(),
            };
        }

        // Builds a custom config based on the current custom settings, 
        // applying validation and clamping as needed
        private ExpressionConfig BuildCustomConfig()
        {
            int maxDepth = Math.Clamp(CustomMaxDepth, 0, 6);

            int leafMin = Math.Clamp(CustomLeafMin, 1, 99);
            int leafMax = Math.Max(leafMin, Math.Clamp(CustomLeafMax, 1, 120));

            int targetMin = CustomTargetMin;
            int targetMax = Math.Max(targetMin, CustomTargetMax);

            if (!CustomAllowNegative)
            {
                if (targetMin < 1) targetMin = 1;
                if (targetMax < 1) targetMax = 1;
            }

            double leafBias = Math.Clamp(CustomLeafBias, 0.0, 0.9);

            return new ExpressionConfig
            {
                MaxDepth = maxDepth,
                TargetMin = targetMin,
                TargetMax = targetMax,
                LeafMin = leafMin,
                LeafMax = leafMax,
                AllowNegative = CustomAllowNegative,
                AddWeight = Math.Max(0, CustomAddWeight),
                SubtractWeight = Math.Max(0, CustomSubtractWeight),
                MultiplyWeight = Math.Max(0, CustomMultiplyWeight),
                DivideWeight = Math.Max(0, CustomDivideWeight),
                LeafBias = leafBias,
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
