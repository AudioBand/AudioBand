﻿using AudioBand.AudioSource;
using AudioBand.Commands;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AudioBand.UI
{
    /// <summary>
    /// View model for the previous button.
    /// </summary>
    public class PreviousButtonViewModel : ButtonViewModelBase<PreviousButton>
    {
        private readonly IAppSettings _appSettings;
        private readonly IAudioSession _audioSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousButtonViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="audioSession">The audio session.</param>
        /// <param name="messageBus">The message bus.</param>
        public PreviousButtonViewModel(IAppSettings appSettings, IDialogService dialogService, IAudioSession audioSession, IMessageBus messageBus)
            : base(appSettings.CurrentProfile.PreviousButton, dialogService, messageBus)
        {
            _appSettings = appSettings;
            _audioSession = audioSession;
            _appSettings.ProfileChanged += AppsSettingsOnProfileChanged;

            PreviousTrackCommand = new AsyncRelayCommand<object>(PreviousTrackCommandOnExecute);
            InitializeButtonContents();
        }

        /// <summary>
        /// Gets or sets the button content.
        /// </summary>
        public ButtonContentViewModel Content { get; set; }

        /// <summary>
        /// Gets the previous track command.
        /// </summary>
        public IAsyncCommand PreviousTrackCommand { get; }

        /// <inheritdoc />
        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            MapSelf(Model, _appSettings.CurrentProfile.PreviousButton);
        }

        private async Task PreviousTrackCommandOnExecute(object arg)
        {
            if (_audioSession.CurrentAudioSource == null)
            {
                return;
            }

            await _audioSession.CurrentAudioSource.PreviousTrackAsync();
        }

        private void AppsSettingsOnProfileChanged(object sender, EventArgs e)
        {
            Debug.Assert(IsEditing == false, "Should not be editing");
            MapSelf(_appSettings.CurrentProfile.PreviousButton, Model);

            InitializeButtonContents();
            RaisePropertyChangedAll();
        }

        private void InitializeButtonContents()
        {
            Content = new ButtonContentViewModel(Model.Content, new PreviousButton().Content, DialogService);
            TrackContentViewModel(Content);
        }
    }
}
