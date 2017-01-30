﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using LightBulb.Models;
using LightBulb.Services;
using LightBulb.Services.Helpers;
using Tyrrrz.WpfExtensions;

namespace LightBulb.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly TemperatureService _temperatureService;
        private readonly WindowService _windowService;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable (GC)
        private readonly SyncedTimer _statusUpdateTimer;
        private readonly Timer _disableTemporarilyTimer;

        private bool _isEnabled;
        private bool _isBlocked;
        private string _statusText;
        private CycleState _cycleState;
        private double _cyclePosition;

        public Settings Settings => Settings.Default;
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Enables or disables the program
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (!Set(ref _isEnabled, value)) return;
                if (value) _disableTemporarilyTimer.IsEnabled = false;

                _temperatureService.IsRealtimeModeEnabled = value && !IsBlocked;
            }
        }

        /// <summary>
        /// Whether gamma control is blocked by something 
        /// </summary>
        public bool IsBlocked
        {
            get { return _isBlocked; }
            private set
            {
                if (!Set(ref _isBlocked, value)) return;

                _temperatureService.IsRealtimeModeEnabled = !value && IsEnabled;
            }
        }

        /// <summary>
        /// Current status text
        /// </summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { Set(ref _statusText, value); }
        }

        /// <summary>
        /// Current state in the day cycle
        /// </summary>
        public CycleState CycleState
        {
            get { return _cycleState; }
            private set { Set(ref _cycleState, value); }
        }

        /// <summary>
        /// Current position in the day cycle
        /// </summary>
        public double CyclePosition
        {
            get { return _cyclePosition; }
            private set { Set(ref _cyclePosition, value); }
        }

        // Commands
        public RelayCommand ShowMainWindowCommand { get; }
        public RelayCommand ExitApplicationCommand { get; }
        public RelayCommand AboutCommand { get; }
        public RelayCommand ToggleEnabledCommand { get; }
        public RelayCommand<double> DisableTemporarilyCommand { get; }

        public MainViewModel(
            TemperatureService temperatureService,
            WindowService windowService)
        {
            // Services
            _temperatureService = temperatureService;
            _windowService = windowService;

            _temperatureService.Updated += (sender, args) =>
            {
                UpdateStatusText();
                UpdateCycleState();
                UpdateCyclePosition();
            };
            _windowService.FullScreenStateChanged += (sender, args) =>
            {
                UpdateBlock();
            };

            // Timers
            _statusUpdateTimer = new SyncedTimer(TimeSpan.FromMinutes(1));
            _statusUpdateTimer.Tick += (sender, args) =>
            {
                UpdateStatusText();
                UpdateCycleState();
                UpdateCyclePosition();
            };
            _disableTemporarilyTimer = new Timer();
            _disableTemporarilyTimer.Tick += (sender, args) =>
            {
                IsEnabled = true;
            };

            // Commands
            ShowMainWindowCommand = new RelayCommand(() =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Application.Current.MainWindow.Show();
                    Application.Current.MainWindow.Activate();
                    Application.Current.MainWindow.Focus();
                });
            });
            ExitApplicationCommand = new RelayCommand(() =>
            {
                Application.Current.ShutdownSafe();
            });
            AboutCommand = new RelayCommand(() =>
            {
                Process.Start("https://github.com/Tyrrrz/LightBulb");
            });
            ToggleEnabledCommand = new RelayCommand(() =>
            {
                IsEnabled = !IsEnabled;
            });
            DisableTemporarilyCommand = new RelayCommand<double>(ms =>
            {
                _disableTemporarilyTimer.IsEnabled = false;
                _disableTemporarilyTimer.Interval = TimeSpan.FromMilliseconds(ms);
                IsEnabled = false;
                _disableTemporarilyTimer.IsEnabled = true;
            });

            // Init
            _statusUpdateTimer.IsEnabled = true;
            IsEnabled = true;
        }

        private void UpdateBlock()
        {
            IsBlocked = Settings.IsFullscreenBlocking && _windowService.IsForegroundFullScreen;

            Debug.WriteLine($"Updated block status (to {IsBlocked})", GetType().Name);
        }

        private void UpdateStatusText()
        {
            // Preview mode (24 hr cycle preview)
            if (_temperatureService.IsPreviewModeEnabled && _temperatureService.IsCyclePreviewRunning)
            {
                StatusText =
                    $"Temp: {_temperatureService.Temperature}K   Time: {_temperatureService.CyclePreviewTime:t}   (preview)";
            }
            // Preview mode
            else if (_temperatureService.IsPreviewModeEnabled)
            {
                StatusText = $"Temp: {_temperatureService.Temperature}K   (preview)";
            }
            // Not enabled
            else if (!IsEnabled)
            {
                StatusText = "Disabled";
            }
            // Blocked
            else if (IsBlocked)
            {
                StatusText = "Blocked";
            }
            // Realtime mode
            else
            {
                StatusText = $"Temp: {_temperatureService.Temperature}K";
            }
        }

        private void UpdateCycleState()
        {
            // Not enabled or blocked
            if (!IsEnabled || IsBlocked)
            {
                CycleState = CycleState.Disabled;
            }
            else
            {
                if (_temperatureService.Temperature >= Settings.MaxTemperature)
                {
                    CycleState = CycleState.Day;
                }
                else if (_temperatureService.Temperature <= Settings.MinTemperature)
                {
                    CycleState = CycleState.Night;
                }
                else
                {
                    CycleState = CycleState.Transition;
                }
            }
        }

        private void UpdateCyclePosition()
        {
            // Preview mode (24 hr cycle preview)
            if (_temperatureService.IsPreviewModeEnabled && _temperatureService.IsCyclePreviewRunning)
            {
                CyclePosition = _temperatureService.CyclePreviewTime.TimeOfDay.TotalHours/24;
            }
            // Preview mode
            else if (_temperatureService.IsPreviewModeEnabled)
            {
                CyclePosition = 0;
            }
            // Not enabled or blocked
            else if (!IsEnabled || IsBlocked)
            {
                CyclePosition = 0;
            }
            // Realtime mode
            else
            {
                CyclePosition = DateTime.Now.TimeOfDay.TotalHours/24;
            }
        }

        public void Dispose()
        {
            _disableTemporarilyTimer.Dispose();
        }
    }
}