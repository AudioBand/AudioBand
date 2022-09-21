using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AudioBand.AudioSource;
using AudioBand.Commands;
using AudioBand.Extensions;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;
using MouseBinding = AudioBand.Models.MouseBinding;

namespace AudioBand.UI
{
    /// <summary>
    /// The Mouse Binding ViewModel.
    /// </summary>
    public class MouseBindingsViewModel : ViewModelBase
    {
        private IAppSettings _appSettings;
        private IAudioSession _audioSession;
        private IMessageBus _messageBus;

        private ObservableCollection<MouseBinding> _mouseBindings = new ObservableCollection<MouseBinding>();
        private List<MouseBinding> _backup = new List<MouseBinding>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseBindingsViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="audioSession">The audio session.</param>
        /// <param name="messageBus">The message bus.</param>
        public MouseBindingsViewModel(IAppSettings appSettings, IAudioSession audioSession, IMessageBus messageBus)
        {
            MouseBindings = new ObservableCollection<MouseBinding>(appSettings.AudioBandSettings.MouseBindings);

            _appSettings = appSettings;
            _audioSession = audioSession;
            _messageBus = messageBus;

            MouseBindings = new ObservableCollection<MouseBinding>(appSettings.AudioBandSettings.MouseBindings);
            StartEditCommand = new RelayCommand(StartEditCommandOnExecute);

            LeftClickCommand = new RelayCommand(LeftClickCommandOnExecute);
            DoubleLeftClickCommand = new RelayCommand(DoubleLeftClickCommandOnExecute);
            MiddleClickCommand = new RelayCommand(MiddleClickCommandOnExecute);
            DoubleMiddleClickCommand = new RelayCommand(DoubleMiddleClickCommandOnExecute);
            MouseWheelCommand = new RelayCommand<MouseWheelEventArgs>(MouseWheelCommandOnExecute);

            UseMessageBus(messageBus);
        }

        /// <summary>
        /// Gets the Mouse Input Types.
        /// </summary>
        public IEnumerable<EnumDescriptor<MouseInputType>> MouseInputTypes { get; } = typeof(MouseInputType).GetEnumDescriptors<MouseInputType>();

        /// <summary>
        /// Gets the Mouse Binding Command types.
        /// </summary>
        public IEnumerable<EnumDescriptor<MouseBindingCommandType>> CommandTypes { get; } = typeof(MouseBindingCommandType).GetEnumDescriptors<MouseBindingCommandType>();

        /// <summary>
        /// Gets or sets the MouseBindings collection.
        /// </summary>
        [TrackState]
        public ObservableCollection<MouseBinding> MouseBindings
        {
            get => _mouseBindings;
            set => SetProperty(ref _mouseBindings, value);
        }

        /// <summary>
        /// Gets the command to start editing.
        /// </summary>
        public ICommand StartEditCommand { get; }

        /// <summary>
        /// Gets the command to handle left clicks.
        /// </summary>
        public ICommand LeftClickCommand { get; }

        /// <summary>
        /// Gets the command to handle double left clicks.
        /// </summary>
        public ICommand DoubleLeftClickCommand { get; }

        /// <summary>
        /// Gets the command to handle middle clicks.
        /// </summary>
        public ICommand MiddleClickCommand { get; }

        /// <summary>
        /// Gets the command to handle double middle clicks.
        /// </summary>
        public ICommand DoubleMiddleClickCommand { get; }

        /// <summary>
        /// Gets the command to handle mouse scrolling.
        /// </summary>
        public ICommand MouseWheelCommand { get; }

        /// <inheritdoc />
        protected override void OnBeginEdit()
        {
            base.OnBeginEdit();
            _backup = MouseBindings.ToList();
        }

        /// <inheritdoc />
        protected override void OnCancelEdit()
        {
            base.OnCancelEdit();
            MouseBindings = new ObservableCollection<MouseBinding>(_backup);
        }

        /// <inheritdoc />
        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            _appSettings.AudioBandSettings.MouseBindings = MouseBindings.ToList();
        }

        private void StartEditCommandOnExecute()
        {
            BeginEdit();
        }

        private void LeftClickCommandOnExecute()
        {
            CheckBindingsPerInputType(MouseInputType.LeftClick);
        }

        private void DoubleLeftClickCommandOnExecute()
        {
            CheckBindingsPerInputType(MouseInputType.LeftDoubleClick);
        }

        private void MiddleClickCommandOnExecute()
        {
            CheckBindingsPerInputType(MouseInputType.MiddleClick);
        }

        private void DoubleMiddleClickCommandOnExecute()
        {
            CheckBindingsPerInputType(MouseInputType.MiddleDoubleClick);
        }

        private void MouseWheelCommandOnExecute(MouseWheelEventArgs args)
        {
            if (args.Delta > 0)
            {
                CheckBindingsPerInputType(MouseInputType.ScrollUp);
                return;
            }

            CheckBindingsPerInputType(MouseInputType.ScrollDown);
        }

        private void CheckBindingsPerInputType(MouseInputType inputType)
        {
            var altDown = Keyboard.GetKeyStates(Key.LeftAlt) == KeyStates.Down || Keyboard.GetKeyStates(Key.RightAlt) == KeyStates.Down;
            var ctrlDown = Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down || Keyboard.GetKeyStates(Key.RightCtrl) == KeyStates.Down;
            var shiftDown = Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down || Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down || Keyboard.GetKeyStates(Key.CapsLock) == KeyStates.Toggled;

            for (int i = 0; i < _appSettings.AudioBandSettings.MouseBindings.Count; i++)
            {
                var mouseBinding = _appSettings.AudioBandSettings.MouseBindings[i];

                // If keys arent down correctly, continue
                if ((mouseBinding.WithAlt && !altDown) || (mouseBinding.WithCtrl && !ctrlDown) || (mouseBinding.WithShift && !shiftDown))
                {
                    continue;
                }

                if (mouseBinding.MouseInputType == inputType)
                {
                    ExecuteCommandBasedOnInputType(mouseBinding.CommandType);
                }
            }
        }

        private void ExecuteCommandBasedOnInputType(MouseBindingCommandType inputType)
        {
            switch (inputType)
            {
                case MouseBindingCommandType.NextProfile:
                    SwitchProfile();
                    break;
                case MouseBindingCommandType.PreviousProfile:
                    SwitchProfile(true);
                    break;
                case MouseBindingCommandType.Play:
                    _audioSession.CurrentAudioSource?.PlayTrackAsync();
                    break;
                case MouseBindingCommandType.Pause:
                    _audioSession.CurrentAudioSource?.PauseTrackAsync();
                    break;
                case MouseBindingCommandType.NextSong:
                    _audioSession.CurrentAudioSource?.NextTrackAsync();
                    break;
                case MouseBindingCommandType.PreviousSong:
                    _audioSession.CurrentAudioSource?.PreviousTrackAsync();
                    break;
                case MouseBindingCommandType.NextRepeatMode:
                    var nextRepeatMode = (int)(_audioSession.RepeatMode + 1) % 3;
                    _audioSession.CurrentAudioSource?.SetRepeatModeAsync((RepeatMode)nextRepeatMode);
                    break;
                case MouseBindingCommandType.PreviousRepeatMode:
                    var previousRepeatMode = (int)(_audioSession.RepeatMode - 1);
                    previousRepeatMode = previousRepeatMode < 0 ? 2 : previousRepeatMode;

                    _audioSession.CurrentAudioSource?.SetRepeatModeAsync((RepeatMode)previousRepeatMode);
                    break;
                case MouseBindingCommandType.ToggleShuffleMode:
                    _audioSession.CurrentAudioSource?.SetShuffleAsync(!_audioSession.IsShuffleOn);
                    break;
                case MouseBindingCommandType.VolumeHigher:
                    _audioSession.CurrentAudioSource?.SetVolumeAsync(_audioSession.Volume + 2);
                    break;
                case MouseBindingCommandType.VolumeLower:
                    _audioSession.CurrentAudioSource?.SetVolumeAsync(_audioSession.Volume - 2);
                    break;
                case MouseBindingCommandType.OpenAssociatedApp:
                    OpenAssociatedApp();
                    break;
                case MouseBindingCommandType.TogglePlayPause:
                    if (_audioSession.IsPlaying)
                    {
                        _audioSession.CurrentAudioSource?.PauseTrackAsync();
                    }
                    else
                    {
                        _audioSession.CurrentAudioSource?.PlayTrackAsync();
                    }

                    break;
                default:
                    break;
            }
        }

        private void OpenAssociatedApp()
        {
            if (_audioSession.CurrentAudioSource == null)
            {
                return;
            }

            var windowPtr = NativeMethods.FindWindow(_audioSession.CurrentAudioSource.WindowClassName, null);

            if (_audioSession.CurrentAudioSource.Name == "iTunes")
            {
                var itunesProcesses = Process.GetProcessesByName("itunes");
                var title = itunesProcesses.FirstOrDefault(x => !string.IsNullOrEmpty(x.MainWindowTitle))?.MainWindowTitle;

                if (string.IsNullOrEmpty(title))
                {
                    try
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        Process.Start($"{path}/iTunes/iTunes.exe");
                    }
                    catch (Exception)
                    {
                        Logger.Debug("Failed to open iTunes through path.");
                    }
                }

                windowPtr = NativeMethods.FindWindow(null, title);
            }
            else if (_audioSession.CurrentAudioSource.Name == "Music Bee")
            {
                var musicbeeProcesses = Process.GetProcessesByName("musicbee");
                var title = musicbeeProcesses.FirstOrDefault(x => !string.IsNullOrEmpty(x.MainWindowTitle))?.MainWindowTitle;

                if (string.IsNullOrEmpty(title))
                {
                    try
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                        Process.Start($"{path}/MusicBee/MusicBee.exe");
                    }
                    catch (Exception)
                    {
                        Logger.Debug("Failed to open MusicBee through path.");
                    }
                }

                windowPtr = NativeMethods.FindWindow(null, title);
            }

            // Spotify has some weird shenanigans with their windows, doing it like normal
            // results in the wrong window handle being returned.
            if (_audioSession.CurrentAudioSource.Name == "Spotify")
            {
                var spotifyProcesses = Process.GetProcessesByName("spotify");
                var title = spotifyProcesses.FirstOrDefault(x => !string.IsNullOrEmpty(x.MainWindowTitle))?.MainWindowTitle;

                if (string.IsNullOrEmpty(title))
                {
                    try
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var exe = $"{path}/Spotify/Spotify.exe";
                        Process.Start(File.Exists(exe) ? exe : "spotify://");
                    }
                    catch (Exception)
                    {
                        Logger.Debug("Failed to open Spotify through path or protocol.");
                    }
                }

                windowPtr = NativeMethods.FindWindow(null, title);
            }

            if (windowPtr == IntPtr.Zero)
            {
                Logger.Warn("Could not find the associated window to open with double click.");
                return;
            }

            if (NativeMethods.IsIconic(windowPtr))
            {
                NativeMethods.ShowWindow(windowPtr, 9);
            }

            NativeMethods.SetForegroundWindow(windowPtr);
        }

        private void SwitchProfile(bool previous = false)
        {
            var index = Array.FindIndex(_appSettings.Profiles.ToArray(), x => x.Name == _appSettings.CurrentProfile.Name);
            var amountOfProfiles = _appSettings.Profiles.Count();

            index = previous ? index - 1 : index + 1;

            // check if profile out of bounds, if so loop back to start/end
            index = index < 0 ? amountOfProfiles - 1 : index;
            index = index >= amountOfProfiles ? 0 : index;

            _appSettings.SelectProfile(_appSettings.Profiles.ElementAt(index).Name);
        }
    }
}
