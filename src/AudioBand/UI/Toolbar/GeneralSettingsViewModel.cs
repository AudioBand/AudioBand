using System;
using System.Diagnostics;
using System.Windows.Media;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;

namespace AudioBand.UI
{
    /// <summary>
    /// View model for the general application.
    /// </summary>
    public class GeneralSettingsViewModel : ViewModelBase
    {
        private readonly IAppSettings _appSettings;
        private readonly GeneralSettings _model = new GeneralSettings();
        private readonly GeneralSettings _backup = new GeneralSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralSettingsViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="messageBus">The message bus.</param>
        public GeneralSettingsViewModel(IAppSettings appSettings, IDialogService dialogService, IMessageBus messageBus)
        {
            MapSelf(appSettings.CurrentProfile.GeneralSettings, _model);

            DialogService = dialogService;
            _appSettings = appSettings;
            appSettings.ProfileChanged += AppsettingsOnProfileChanged;
            UseMessageBus(messageBus);
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [TrackState]
        public double Width
        {
            get => _model.Width;
            set => SetProperty(_model, nameof(_model.Width), value);
        }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [TrackState]
        public double Height
        {
            get => _model.Height;
            set => SetProperty(_model, nameof(_model.Height), value);
        }

        /// <summary>
        /// Gets or sets the Rotation Angle.
        /// </summary>
        [TrackState]
        public double RotationAngle
        {
            get => _model.RotationAngle;
            set => SetProperty(_model, nameof(_model.RotationAngle), value);
        }

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        [TrackState]
        public Color BackgroundColor
        {
            get => _model.BackgroundColor;
            set => SetProperty(_model, nameof(_model.BackgroundColor), value);
        }

        /// <summary>
        /// Gets the dialog service.
        /// </summary>
        public IDialogService DialogService { get; }

        /// <inheritdoc />
        protected override void OnReset()
        {
            base.OnReset();
            ResetObject(_model);
        }

        /// <inheritdoc />
        protected override void OnBeginEdit()
        {
            base.OnBeginEdit();
            MapSelf(_model, _backup);
        }

        /// <inheritdoc />
        protected override void OnCancelEdit()
        {
            base.OnCancelEdit();
            MapSelf(_backup, _model);
        }

        /// <inheritdoc />
        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            MapSelf(_model, _appSettings.CurrentProfile.GeneralSettings);
        }

        private void AppsettingsOnProfileChanged(object sender, EventArgs e)
        {
            Debug.Assert(IsEditing == false, "Should not be editing");
            MapSelf(_appSettings.CurrentProfile.GeneralSettings, _model);
            RaisePropertyChangedAll();
        }
    }
}
