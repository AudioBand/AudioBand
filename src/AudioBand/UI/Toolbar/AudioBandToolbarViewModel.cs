﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioBand.AudioSource;
using AudioBand.Commands;
using AudioBand.Logging;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;
using NLog;

namespace AudioBand.UI
{
    /// <summary>
    /// View model for audio band toolbar.
    /// </summary>
    public class AudioBandToolbarViewModel : ObservableObject
    {
        private static readonly ILogger Logger = AudioBandLogManager.GetLogger<AudioBandToolbarViewModel>();
        private readonly IAppSettings _appSettings;
        private readonly IAudioSourceManager _audioSourceManager;
        private readonly IMessageBus _messageBus;
        private readonly IAudioSession _audioSession;
        private readonly PopupService _popups;
        private readonly GitHubHelper _github;
        private IAudioSource _selectedAudioSource;
        private UserProfile _selectedUserProfile;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBandToolbarViewModel"/> class.
        /// </summary>
        /// <param name="viewModels">The view models.</param>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="audioSourceManager">The audio source mananger.</param>
        /// <param name="messageBus">The message bus.</param>
        /// <param name="audioSession">The audio session.</param>
        /// <param name="popups">The popup service.</param>
        /// <param name="github">The GitHub helper.</param>
        public AudioBandToolbarViewModel(IViewModelContainer viewModels, IAppSettings appSettings, IAudioSourceManager audioSourceManager, IMessageBus messageBus, IAudioSession audioSession, PopupService popups, GitHubHelper github)
        {
            _appSettings = appSettings;
            _audioSourceManager = audioSourceManager;
            _messageBus = messageBus;
            _audioSession = audioSession;
            _popups = popups;
            _github = github;
            ViewModels = viewModels;

            _messageBus.Subscribe<ProfilesUpdatedMessage>(OnProfilesUpdated);
            _messageBus.Subscribe<AudioSourceUpdatedMessage>(OnAudioSourceUpdated);

            ShowSettingsWindowCommand = new RelayCommand(ShowSettingsWindowCommandOnExecute);
            LoadCommand = new AsyncRelayCommand<object>(LoadCommandOnExecute);
            SelectAudioSourceCommand = new AsyncRelayCommand<IInternalAudioSource>(SelectAudioSourceCommandOnExecute);
            SelectProfileCommand = new RelayCommand<string>(SelectProfileCommandOnExecute);
        }

        /// <summary>
        /// Gets a list of all the ViewModels.
        /// </summary>
        public IViewModelContainer ViewModels { get; }

        /// <summary>
        /// Gets a collection of all available AudioSources.
        /// </summary>
        public ObservableCollection<IInternalAudioSource> AudioSources { get; private set; }

        /// <summary>
        /// Gets a collection of all possible UserProfiles.
        /// </summary>
        public ObservableCollection<UserProfile> Profiles { get; private set; }

        /// <summary>
        /// Gets the command to show the settings window.
        /// </summary>
        public ICommand ShowSettingsWindowCommand { get; }

        /// <summary>
        /// Gets the command to initialize loading.
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Gets the command to select an audio source.
        /// </summary>
        public ICommand SelectAudioSourceCommand { get; }

        /// <summary>
        /// Gets the command to select a profile.
        /// </summary>
        public ICommand SelectProfileCommand { get; }

        /// <summary>
        /// Gets or sets the selected audio source.
        /// </summary>
        public IAudioSource SelectedAudioSource
        {
            get => _selectedAudioSource;
            set
            {
                if (SetProperty(ref _selectedAudioSource, value))
                {
                    _appSettings.AudioSource = value?.Name;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected profile.
        /// </summary>
        public UserProfile SelectedProfile
        {
            get => _selectedUserProfile;
            set => SetProperty(ref _selectedUserProfile, value);
        }

        private void OnAudioSourceUpdated(AudioSourceUpdatedMessage msg)
        {
        }

        private void ShowSettingsWindowCommandOnExecute()
        {
            _messageBus.Publish(SettingsWindowMessage.OpenWindow);
        }

        private async Task LoadCommandOnExecute(object arg)
        {
            Logger.Debug("Loading audio sources");

            var audioSources = await _audioSourceManager.LoadAudioSourcesAsync();
            AudioSources = new ObservableCollection<IInternalAudioSource>(audioSources);

            foreach (var audioSource in audioSources)
            {
                ViewModels.AudioSourceSettingsViewModel.CreateViewModelForAudioSource(audioSource);

                // If user was using this audio source last, then automatically activate it
                var savedAudioSourceName = _appSettings.AudioSource;
                if (savedAudioSourceName == null || audioSource.Name != savedAudioSourceName)
                {
                    continue;
                }

                SelectAudioSourceCommand.Execute(audioSource);
            }

            // Raise property changed after everything is set up so audio source settings can't be changed by the user in the middle.
            RaisePropertyChanged(nameof(AudioSources));
            Logger.Debug("Audio sources loaded. Loaded {num} sources", AudioSources.Count);

            // Initalize Profiles
            InitializeProfiles();

            SelectedProfile = _appSettings.CurrentProfile;
            _appSettings.AudioBandSettings.LastNonIdleProfileName = SelectedProfile.Name;

            if (_appSettings.AudioBandSettings.UseAutomaticIdleProfile && !_audioSession.IsPlaying)
            {
                SelectProfileCommand.Execute(_appSettings.AudioBandSettings.IdleProfileName);
            }

            Logger.Debug($"Loaded {Profiles.Count} profiles. (may exclude idle profile)");

            if (!await _github.IsOnLatestVersionAsync())
            {
                _popups.ShowPopup("UpdatePopupTitle", "UpdatePopupDescription", TimeSpan.FromSeconds(180));
            }
        }

        private void InitializeProfiles()
        {
            Profiles = _appSettings.AudioBandSettings.HideIdleProfileInQuickMenu
                ? new ObservableCollection<UserProfile>(_appSettings.Profiles.Where(x => x.Name != _appSettings.AudioBandSettings.IdleProfileName))
                : new ObservableCollection<UserProfile>(_appSettings.Profiles);

            RaisePropertyChanged(nameof(Profiles));
        }

        private async Task SelectAudioSourceCommandOnExecute(IInternalAudioSource audioSource)
        {
            if (SelectedAudioSource != null)
            {
                Logger.Debug("Deactivating current audio source {audiosource}", SelectedAudioSource.Name);
                try
                {
                    await SelectedAudioSource.DeactivateAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error deactivating audio source");
                }
            }

            if (audioSource == null || audioSource == SelectedAudioSource)
            {
                if (_appSettings.AudioBandSettings.UseAutomaticIdleProfile)
                {
                    SelectProfileCommand.Execute(_appSettings.AudioBandSettings.IdleProfileName);
                }

                SelectedAudioSource = null;
                _appSettings.Save();
                return;
            }

            _audioSession.CurrentAudioSource = audioSource;

            Logger.Debug("Activating new audio source {audiosource}", audioSource.Name);
            try
            {
                if (_appSettings.AudioBandSettings.UseAutomaticIdleProfile)
                {
                    SelectProfileCommand.Execute(_appSettings.AudioBandSettings.LastNonIdleProfileName);
                }

                await audioSource.ActivateAsync();
                SelectedAudioSource = audioSource;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error activating audio source");
                SelectedAudioSource = null;
            }
            finally
            {
                _appSettings.Save();
            }
        }

        private void SelectProfileCommandOnExecute(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                return;
            }

            _appSettings.SelectProfile(profileName);
            SelectedProfile = _appSettings.CurrentProfile;
        }

        private void OnProfilesUpdated(ProfilesUpdatedMessage msg)
        {
            if (msg == ProfilesUpdatedMessage.ProfileSelected)
            {
                Logger.Info($"Switching from profile \"{SelectedProfile}\" to \"{_appSettings.CurrentProfile}\"");

                SelectedProfile = _appSettings.CurrentProfile;
                return;
            }

            InitializeProfiles();
        }
    }
}
