using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;

// Calcraze might be calculating crazy expressions, 
// but this code was crazier than any expression

namespace Calcraze.ViewModels
{
    // Minimal ICommand implementation (just ReactiveUI becauze no need)
    public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Action _execute = execute;
        private readonly Func<bool>? _canExecute = canExecute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter)    => _execute();
        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // Game state
    public enum GameState { Start, Playing, Settings }

    // ViewModel
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // Infra

        private readonly ExpressionGenerator _generator = new ExpressionGenerator();

        // HP timer for smooth progress bar animation 
        private readonly DispatcherTimer _gameTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
        // Short pulse timer that hides the feedback label
        private readonly DispatcherTimer _feedbackTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };

        // Defaults
        private double _roundDurationMs = 60_000;
        private double _timeLeftMs = 60_000;
        private double _shortAnswerWindowMs = 2500;

        // State
        private GameState _state = GameState.Start;
        private int _score = 0;
        private int _streak = 0;
        private int _currentAnswer;
        private string _expressionText = "";
        private string _answerText = "";
        private DateTime _questionStartUtc = DateTime.UtcNow;

        private ExpressionConfig _baseConfig = ExpressionConfig.Balanced;
        private ExpressionConfig _ceilingConfig = ExpressionConfig.Hard;
        private ExpressionConfig _currentConfig = ExpressionConfig.Balanced;
        private double _difficultyMeter;
        private double _lastDifficultyScore;

        private readonly SettingsViewModel _settings = new();

        // ctorrrrr
        public MainWindowViewModel()
        {
            StartGameCommand  = new RelayCommand(StartGame);
            SubmitAnswerCommand = new RelayCommand(SubmitAnswer);
            OpenSettingsCommand = new RelayCommand(OpenSettings, () => _state == GameState.Start);
            CloseSettingsCommand = new RelayCommand(CloseSettings, () => _state == GameState.Settings);

            _gameTimer.Tick += GameTimerTick;
            _feedbackTimer.Tick += (_, _) =>
            {
                FeedbackOpacity = 0.0;
                _feedbackTimer.Stop();
            };

            _settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsViewModel.RoundDurationSeconds))
                    OnPropertyChanged(nameof(RoundDurationDisplay));
            };
        }

        // Screen switching

        public double  StartOpacity => _state == GameState.Start ? 1.0 : 0.0;
        public double  GameOpacity => _state == GameState.Playing ? 1.0 : 0.0;
        public double  SettingsOpacity => _state == GameState.Settings ? 1.0 : 0.0;
        public bool    StartHitTest => _state == GameState.Start;
        public bool    GameHitTest => _state == GameState.Playing;
        public bool    SettingsHitTest => _state == GameState.Settings;

        // Settings
        public SettingsViewModel Settings => _settings;
        public bool IsSettingsOpen => _state == GameState.Settings;

        // Score history
        private int _finalScore;
        public int FinalScore
        {
            get => _finalScore;
            private set { _finalScore = value; OnPropertyChanged(); }
        }
        private int _highScore;
        public int HighScore
        {
            get => _highScore;
            private set { _highScore = value; OnPropertyChanged(); }
        }
        private bool _hasPlayedBefore;
        public bool HasPlayedBefore
        {
            get => _hasPlayedBefore;
            private set { _hasPlayedBefore = value; OnPropertyChanged(); }
        }

        // Gap above the START button shrinks when the score is shown so
        // the overall layout stays balanced, hell yeah
        public double StartButtonGap => HasPlayedBefore ? 28.0 : 60.0;
        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(); }
        }

        // Expression display
        public string ExpressionText
        {
            get => _expressionText;
            set { _expressionText = value; OnPropertyChanged(); }
        }

        public string AnswerText
        {
            get => _answerText;
            set { _answerText = value; OnPropertyChanged(); }
        }

        // Timer display
        public double TimerProgress => _roundDurationMs <= 0
            ? 0.0
            : Math.Clamp(_timeLeftMs / _roundDurationMs, 0.0, 1.0);

        // Whole seconds, always rounds up so "0" only shows at the very end
        public string TimeDisplay
        {
            get
            {
                int seconds = (int)Math.Ceiling(_timeLeftMs / 1000.0);
                return $"{seconds / 60:D2}:{seconds % 60:D2}";
            }
        }

        public string RoundDurationDisplay => $"{Settings.RoundDurationSeconds} seconds";

        // Timer bar shifts amber to red as time runs low
        private IBrush _timerBarColor = new SolidColorBrush(Color.Parse("#EFA94A"));
        public IBrush TimerBarColor
        {
            get => _timerBarColor;
            private set { _timerBarColor = value; OnPropertyChanged(); }
        }

        // Feedback flash
        private string _feedbackText = "";
        public string FeedbackText
        {
            get => _feedbackText;
            set { _feedbackText = value; OnPropertyChanged(); }
        }
        private double _feedbackOpacity;
        public double FeedbackOpacity
        {
            get => _feedbackOpacity;
            set { _feedbackOpacity = value; OnPropertyChanged(); }
        }
        private IBrush _feedbackForeground = Brushes.Transparent;
        public IBrush FeedbackForeground
        {
            get => _feedbackForeground;
            set { _feedbackForeground = value; OnPropertyChanged(); }
        }

        // Commands

        public ICommand StartGameCommand { get; }
        public ICommand SubmitAnswerCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand CloseSettingsCommand { get; }

        // Fun fun fun fun fun fun
        private void StartGame()
        {
            ApplySettingsSnapshot();
            Score = 0;
            _streak = 0;
            _timeLeftMs = _roundDurationMs;
            if (_state == GameState.Settings)
                _state = GameState.Start;

            _state = GameState.Playing;
            NotifyScreenChange();

            TimerBarColor = new SolidColorBrush(Color.Parse("#EFA94A"));
            OnPropertyChanged(nameof(TimerProgress));
            OnPropertyChanged(nameof(TimeDisplay));
            GenerateExpression();
            _gameTimer.Start();
        }

        private void SubmitAnswer()
        {
            if (_state != GameState.Playing) return;

            string raw = AnswerText.Trim();
            AnswerText = ""; // clear immediately so it feels responsive

            if (!int.TryParse(raw, out int userAnswer)) return;

            double responseMs = (DateTime.UtcNow - _questionStartUtc).TotalMilliseconds;

            // Feedback and scoring
            if (userAnswer == _currentAnswer)
            {
                _streak++;
                double speedBonus = GetSpeedBonus(responseMs);
                int points = CalculatePoints(_lastDifficultyScore, speedBonus);
                Score += points;
                Flash($"+{points}", correct: true);
                IncreaseDifficulty(_lastDifficultyScore, speedBonus);
            }
            else
            {
                _streak = 0;
                DecreaseDifficulty();
                Flash($"✗  {_currentAnswer}", correct: false);
            }

            GenerateExpression();
        }

        // Internals
        private void GenerateExpression()
        {
            _currentConfig = BuildDynamicConfig();
            _lastDifficultyScore = ComputeDifficultyScore(_currentConfig);

            var expr = _generator.Generate(_currentConfig);
            _currentAnswer = expr.Evaluate();
            ExpressionText = $"{expr.Render()}  =";
            _questionStartUtc = DateTime.UtcNow;
        }

        private void Flash(string text, bool correct)
        {
            FeedbackText = text;
            FeedbackForeground = correct
                ? new SolidColorBrush(Color.Parse("#5BCB82"))
                : new SolidColorBrush(Color.Parse("#E05A5A"));
            FeedbackOpacity = 1.0;

            _feedbackTimer.Stop();
            _feedbackTimer.Start();
        }

        private void GameTimerTick(object? sender, EventArgs e)
        {
            _timeLeftMs -= _gameTimer.Interval.TotalMilliseconds;

            if (_timeLeftMs <= 0)
            {
                _timeLeftMs = 0;
                _gameTimer.Stop();
                EndGame();
                return;
            }

            // Shift timer bar to red in the final 10 seconds
            if (_timeLeftMs < 10_000)
                TimerBarColor = new SolidColorBrush(Color.Parse("#E05A5A"));

            OnPropertyChanged(nameof(TimerProgress));
            OnPropertyChanged(nameof(TimeDisplay));
        }

        // Timer ran out, show final score and return to start screen
        private void EndGame()
        {
            FinalScore = Score;
            HasPlayedBefore = true;
            if (Score > HighScore) HighScore = Score;

            _state = GameState.Start;
            NotifyScreenChange();
            OnPropertyChanged(nameof(StartButtonGap));
        }

        // Opens the settings screen, only from the start screen
        private void OpenSettings()
        {
            if (_state != GameState.Start) return;
            _state = GameState.Settings;
            NotifyScreenChange();
            UpdateCommandStates();
        }

        // Closes the settings screen and applies the new settings, only from the settings screen
        private void CloseSettings()
        {
            if (_state != GameState.Settings) return;
            _state = GameState.Start;
            NotifyScreenChange();
            UpdateCommandStates();
        }

        // Applies the current settings to the game configuration and difficulty meter, called at the start of each game
        private void ApplySettingsSnapshot()
        {
            int clampedSeconds = Math.Clamp(Settings.RoundDurationSeconds, 15, 300);
            if (Settings.RoundDurationSeconds != clampedSeconds)
                Settings.RoundDurationSeconds = clampedSeconds;

            _roundDurationMs = clampedSeconds * 1000.0;
            _baseConfig = Settings.BuildConfig();
            _ceilingConfig = CreateCeilingConfig(_baseConfig);
            _difficultyMeter = Settings.SelectedPresetName switch
            {
                "Easy" => 0.05,
                "Hard" => 0.65,
                "Custom" => 0.2,
                _ => 0.35,
            };

            _shortAnswerWindowMs = ComputeShortAnswerWindowMs(_roundDurationMs, _baseConfig);
            OnPropertyChanged(nameof(RoundDurationDisplay));
        }

        // The shorter the round duration and the higher the base difficulty, the shorter the short answer window, but it will never be shorter than 900ms to avoid being unfair
        private static double ComputeShortAnswerWindowMs(double roundDurationMs, ExpressionConfig baseConfig)
        {
            double difficulty = ComputeDifficultyScore(baseConfig);
            double baseWindow = roundDurationMs * 0.06;
            double scaled = baseWindow * (1.1 - difficulty * 0.6);
            return Math.Clamp(scaled, 900.0, 5000.0);
        }

        // Points are based on a base of 6, plus up to 12 for difficulty, up to 6 for speed, and up to 4 for streaks, 
        // with a minimum of 3 points for any correct answer
        private int CalculatePoints(double difficultyScore, double speedBonus)
        {
            double points = 6.0 + (12.0 * difficultyScore) + (6.0 * speedBonus) + Math.Min(_streak, 4);
            return (int)Math.Max(3, Math.Round(points));
        }

        // Speed bonus is a value from 0.0 to 1.0 based on how quickly the user answered relative to the short answer window, 
        // with 1.0 being instant and 0.0 being at or beyond the short answer window
        private double GetSpeedBonus(double responseMs)
        {
            if (_shortAnswerWindowMs <= 0) return 0.0;
            return Math.Clamp(1.0 - (responseMs / _shortAnswerWindowMs), 0.0, 1.0);
        }

        // Increases the difficulty meter based on the difficulty score of the current question and the speed bonus, 
        // with diminishing returns as the meter gets higher
        private void IncreaseDifficulty(double difficultyScore, double speedBonus)
        {
            double easeFactor = 1.0 - difficultyScore;
            double ramp = 0.03 + (speedBonus * 0.07);
            double delta = ramp * (0.6 + easeFactor);
            _difficultyMeter = Math.Clamp(_difficultyMeter + delta, 0.0, 1.0);
        }

        // Opposite of IncreaseDifficulty, but it doesn't take any parameters 
        // into account and just decreases the meter by a flat amount to make 
        // sure the player feels a noticeable relief on a wrong answer
        private void DecreaseDifficulty()
        {
            _difficultyMeter = Math.Clamp(_difficultyMeter - 0.05, 0.0, 1.0);
        }

        // Builds a dynamic config for the current question by interpolating between the 
        // base config and the ceiling config based on the difficulty meter
        private ExpressionConfig BuildDynamicConfig()
        {
            double t = Math.Clamp(_difficultyMeter, 0.0, 1.0);
            return LerpConfig(_baseConfig, _ceilingConfig, t);
        }

        // Creates a ceiling config that is at least as hard as the base config but has 
        // more extreme values to ensure a noticeable difficulty increase as the meter approaches 1.0
        private static ExpressionConfig CreateCeilingConfig(ExpressionConfig baseConfig)
        {
            var hard = ExpressionConfig.Hard;
            return new ExpressionConfig
            {
                MaxDepth = Math.Max(baseConfig.MaxDepth + 1, hard.MaxDepth),
                TargetMin = Math.Min(baseConfig.TargetMin, hard.TargetMin),
                TargetMax = Math.Max(baseConfig.TargetMax, hard.TargetMax),
                LeafMin = Math.Min(baseConfig.LeafMin, hard.LeafMin),
                LeafMax = Math.Max(baseConfig.LeafMax, hard.LeafMax),
                AllowNegative = baseConfig.AllowNegative || hard.AllowNegative,
                AddWeight = Math.Max(baseConfig.AddWeight, hard.AddWeight),
                SubtractWeight = Math.Max(baseConfig.SubtractWeight, hard.SubtractWeight),
                MultiplyWeight = Math.Max(baseConfig.MultiplyWeight, hard.MultiplyWeight),
                DivideWeight = Math.Max(baseConfig.DivideWeight, hard.DivideWeight),
                LeafBias = Math.Min(baseConfig.LeafBias, hard.LeafBias),
            };
        }

        // Linearly interpolates between two configs based on t, which should be between 0.0 
        // and 1.0, where 0.0 returns config a, 1.0 returns config b, and values in between 
        // return a mix of the two
        private static ExpressionConfig LerpConfig(ExpressionConfig a, ExpressionConfig b, double t)
        {
            int LerpInt(int from, int to) => (int)Math.Round(from + (to - from) * t);
            double LerpDouble(double from, double to) => from + (to - from) * t;

            int maxDepth = LerpInt(a.MaxDepth, b.MaxDepth);
            int targetMin = LerpInt(a.TargetMin, b.TargetMin);
            int targetMax = LerpInt(a.TargetMax, b.TargetMax);
            int leafMin = LerpInt(a.LeafMin, b.LeafMin);
            int leafMax = LerpInt(a.LeafMax, b.LeafMax);

            if (leafMax < leafMin) leafMax = leafMin;
            if (targetMax < targetMin) targetMax = targetMin;

            return new ExpressionConfig
            {
                MaxDepth = maxDepth,
                TargetMin = targetMin,
                TargetMax = targetMax,
                LeafMin = leafMin,
                LeafMax = leafMax,
                AllowNegative = a.AllowNegative || (t > 0.55 && b.AllowNegative),
                AddWeight = Math.Max(0, LerpInt(a.AddWeight, b.AddWeight)),
                SubtractWeight = Math.Max(0, LerpInt(a.SubtractWeight, b.SubtractWeight)),
                MultiplyWeight = Math.Max(0, LerpInt(a.MultiplyWeight, b.MultiplyWeight)),
                DivideWeight = Math.Max(0, LerpInt(a.DivideWeight, b.DivideWeight)),
                LeafBias = Math.Clamp(LerpDouble(a.LeafBias, b.LeafBias), 0.0, 0.95),
            };
        }

        // Computes a difficulty score between 0.0 and 1.0 based on the config, where 0.0 is very easy and 1.0 is very hard,
        // taking into account factors like max depth, target range, operator complexity, and whether negatives are allowed
        private static double ComputeDifficultyScore(ExpressionConfig config)
        {
            double depthScore = Math.Clamp((config.MaxDepth - 1) / 3.0, 0.0, 1.0);
            double rangeScore = Math.Clamp((config.TargetMax - config.TargetMin) / 140.0, 0.0, 1.0);
            double opScore = Math.Clamp((config.MultiplyWeight + config.DivideWeight) / 8.0, 0.0, 1.0);
            double negScore = config.AllowNegative ? 0.2 : 0.0;
            double leafScore = Math.Clamp(1.0 - config.LeafBias, 0.0, 1.0);

            double score = (0.3 * depthScore) + (0.25 * rangeScore) + (0.25 * opScore) + (0.1 * negScore) + (0.1 * leafScore);

            return Math.Clamp(score, 0.0, 1.0);
        }

        // Notifies the view of all properties that depend on the current game state, called after changing the state
        private void NotifyScreenChange()
        {
            OnPropertyChanged(nameof(StartOpacity));
            OnPropertyChanged(nameof(GameOpacity));
            OnPropertyChanged(nameof(SettingsOpacity));
            OnPropertyChanged(nameof(StartHitTest));
            OnPropertyChanged(nameof(GameHitTest));
            OnPropertyChanged(nameof(SettingsHitTest));
            OnPropertyChanged(nameof(IsSettingsOpen));
            UpdateCommandStates();
        }

        // Raises CanExecuteChanged on the commands that depend on the game state to 
        // enable or disable them as needed
        private void UpdateCommandStates()
        {
            (OpenSettingsCommand as RelayCommand)?.Raise();
            (CloseSettingsCommand as RelayCommand)?.Raise();
        }

        // INPC boilerplate
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}