using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
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
    public class LikeDislikeButtonViewModel : ButtonViewModelBase<LikeDislikeButton>
    {
        private readonly IAppSettings _appSettings;
        private readonly IAudioSession _audioSession;
        private bool _isLiked;

        /// <summary>
        /// Initializes a new instance of the <see cref="LikeDislikeButtonViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">App settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="audioSession">The audio session.</param>
        /// <param name="messageBus">The message bus.</param>
        public LikeDislikeButtonViewModel(IAppSettings appSettings, IDialogService dialogService, IAudioSession audioSession, IMessageBus messageBus)
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

            if (IsLiked)
            {
                await _audioSession.CurrentAudioSource.DislikeTrackAsync();
            }
            else
            {
                await _audioSession.CurrentAudioSource.LikeTrackAsync();
            }
        }

        private void InitializeButtonContents()
        {
            var resetBase = new LikeDislikeButton();
            LikeContent = new ButtonContentViewModel(Model.LikeContent, resetBase.LikeContent, DialogService);
            DislikeContent = new ButtonContentViewModel(Model.DislikeContent, resetBase.DislikeContent, DialogService);

            TrackContentViewModel(LikeContent);
            TrackContentViewModel(DislikeContent);
        }

        /// <summary>
        /// Gets or sets the button's Corner Radius.
        /// </summary>
        [TrackState]
        public int CornerRadius
        {
            get => Model.CornerRadius;
            set => SetProperty(Model, nameof(Model.CornerRadius), value);
        }

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        [TrackState]
        public Color BackgroundColor
        {
            get => Model.BackgroundColor;
            set => SetProperty(Model, nameof(Model.BackgroundColor), value);
        }

        /// <summary>
        /// Gets or sets the background color when hovered.
        /// </summary>
        [TrackState]
        public Color HoveredBackgroundColor
        {
            get => Model.HoveredBackgroundColor;
            set => SetProperty(Model, nameof(Model.HoveredBackgroundColor), value);
        }

        /// <summary>
        /// Gets or sets the background color when clicked.
        /// </summary>
        [TrackState]
        public Color ClickedBackgroundColor
        {
            get => Model.ClickedBackgroundColor;
            set => SetProperty(Model, nameof(Model.ClickedBackgroundColor), value);
        }

        /// <summary>
        /// Gets the dialog service.
        /// </summary>
        public IDialogService DialogService { get; }

        /// <summary>
        /// Track the button content view models edit state.
        /// </summary>
        /// <param name="viewModel">The button content view model.</param>
        protected void TrackContentViewModel(ButtonContentViewModel viewModel)
        {
            Debug.Assert(!_contentViewModels.Contains(viewModel), "Already tracked this view model");
            _contentViewModels.Add(viewModel);
            viewModel.PropertyChanged += ButtonContentViewModelOnPropertyChanged;
        }

        /// <inheritdoc />
        protected override void OnCancelEdit()
        {
            base.OnCancelEdit();
            foreach (var buttonContentViewModel in _contentViewModels)
            {
                buttonContentViewModel.CancelEdit();
            }
        }

        /// <inheritdoc />
        protected override void OnBeginEdit()
        {
            base.OnBeginEdit();
            foreach (var buttonContentViewModel in _contentViewModels)
            {
                buttonContentViewModel.BeginEdit();
            }
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
            base.OnReset();
            foreach (var buttonContentViewModel in _contentViewModels)
            {
                buttonContentViewModel.Reset();
            }
        }

        private void ButtonContentViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IsEditing))
            {
                return;
            }

            var vm = (ButtonContentViewModel)sender;
            if (vm.IsEditing)
            {
                BeginEdit();
            }
        }
    }
}
