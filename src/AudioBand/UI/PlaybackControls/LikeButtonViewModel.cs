using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using AudioBand.AudioSource;
using AudioBand.Commands;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;

namespace AudioBand.UI
{
    /// <summary>
    /// View model for the like/dislike button.
    /// </summary>
    public class LikeButtonViewModel : ButtonViewModelBase<LikeButton>
    {
        private readonly IAppSettings _appSettings;
        private readonly IAudioSession _audioSession;
        private bool _isLiked;


        /// <summary>
        /// Initializes a new instance of the <see cref="LikeButtonViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">App settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="audioSession">The audio session.</param>
        /// <param name="messageBus">The message bus.</param>
        public LikeButtonViewModel(IAppSettings appSettings, IDialogService dialogService, IAudioSession audioSession, IMessageBus messageBus)
            : base(appSettings.CurrentProfile.LikeDislikeButton, dialogService, messageBus)
        {
            _appSettings = appSettings;
            _audioSession = audioSession;
            _audioSession.PropertyChanged += AudioSessionOnPropertyChanged;
            _appSettings.ProfileChanged += AppSettingsOnProfileChanged;
            LikeDislikeTrackCommand = new AsyncRelayCommand<object>(LikeDislikeTrackCommandOnExecute);

            InitializeButtonContents();
        }

        /// <summary>
        /// Gets or sets the view model for the button in the like state.
        /// </summary>
        public ButtonContentViewModel LikeContent { get; set; }

        /// <summary>
        /// Gets or sets the view model for the button in the dislike state.
        /// </summary>
        public ButtonContentViewModel DislikeContent { get; set; }

        /// <summary>
        /// Gets the like dislike command.
        /// </summary>
        public IAsyncCommand LikeDislikeTrackCommand { get; }

        /// <summary>
        /// Gets a value indicating whether a track is liked.
        /// </summary>
        [AlsoNotify(nameof(IsLikeButtonShown))]
        public bool IsLiked
        {
            get => _isLiked;
            private set => SetProperty(ref _isLiked, value);
        }

        /// <summary>
        /// Gets a value indicating whether the button is liked.
        /// </summary>
        public bool IsLikeButtonShown => !_isLiked;

        /// <inheritdoc />
        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            MapSelf(Model, _appSettings.CurrentProfile.LikeDislikeButton);
        }

        private void AppSettingsOnProfileChanged(object sender, EventArgs e)
        {
            Debug.Assert(IsEditing == false, "Should not be editing");
            MapSelf(_appSettings.CurrentProfile.LikeDislikeButton, Model);

            InitializeButtonContents();
            RaisePropertyChangedAll();
        }

        private void AudioSessionOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IAudioSession.IsLiked))
            {
                return;
            }

            OnIsLikedChanged(_audioSession.IsLiked);
        }

        private void OnIsLikedChanged(bool isLiked)
        {
            IsLiked = isLiked;
        }

        private async Task LikeDislikeTrackCommandOnExecute(object arg)
        {
            if (_audioSession.CurrentAudioSource == null)
            {
                return;
            }

            await _audioSession.CurrentAudioSource.SetLikeTrackAsync();
        }

        private void InitializeButtonContents()
        {
            var resetBase = new LikeButton();
            LikeContent = new ButtonContentViewModel(Model.LikeContent, resetBase.LikeContent, DialogService);
            DislikeContent = new ButtonContentViewModel(Model.DislikeContent, resetBase.DislikeContent, DialogService);

            TrackContentViewModel(LikeContent);
            TrackContentViewModel(DislikeContent);
        }
    }
}
