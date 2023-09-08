using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioBand.Commands;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;

namespace AudioBand.UI
{
    /// <summary>
    /// View model for Global AudioBand Settings.
    /// </summary>
    public class GlobalSettingsViewModel : ViewModelBase
    {
        private IAppSettings _appSettings;
        private IMessageBus _messageBus;
        private GitHubHelper _gitHubHelper;
        private bool _updateIsAvailable;
        private bool _noUpdateFound;
        private bool _isDownloading;
        private int _downloadPercentage;
        private string _fileName;
        private readonly AudioBandSettings _model = new AudioBandSettings();
        private readonly AudioBandSettings _backup = new AudioBandSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSettingsViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="messageBus">The message bus.</param>
        /// <param name="gitHubHelper">The GitHub helper.</param>
        public GlobalSettingsViewModel(IAppSettings appSettings, IMessageBus messageBus, GitHubHelper gitHubHelper)
        {
            MapSelf(appSettings.AudioBandSettings, _model);
            _appSettings = appSettings;
            _messageBus = messageBus;
            _gitHubHelper = gitHubHelper;

            _messageBus.Subscribe<ProfilesUpdatedMessage>(OnProfilesUpdated);

            CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesCommandOnExecute);
            InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateCommandOnExecute);

            ProfileNames = new ObservableCollection<string>(appSettings.Profiles.Select(p => p.Name));
            SelectedProfileName = appSettings.AudioBandSettings.IdleProfileName;

            UseMessageBus(messageBus);
        }

        /// <summary>
        /// Gets the command that checks for updates.
        /// </summary>
        public ICommand CheckForUpdatesCommand { get; }

        /// <summary>
        /// Gets the command that will download and install the latest update.
        /// </summary>
        public ICommand InstallUpdateCommand { get; }

        /// <summary>
        /// Gets or sets whether to show a popup when an update is available.
        /// </summary>
        [TrackState]
        public bool ShowPopupOnAvailableUpdate
        {
            get => _model.ShowPopupOnAvailableUpdate;
            set => SetProperty(_model, nameof(_model.ShowPopupOnAvailableUpdate), value);
        }

        /// <summary>
        /// Gets or sets whether to show a popup when an update is available.
        /// </summary>
        [TrackState]
        public bool OptInForPreReleases
        {
            get => _model.OptInForPreReleases;
            set => SetProperty(_model, nameof(_model.OptInForPreReleases), value);
        }

        /// <summary>
        /// Gets or sets whether to use Automatic Idle Profile.
        /// </summary>
        [TrackState]
        public bool UseAutomaticIdleProfile
        {
            get => _model.UseAutomaticIdleProfile;
            set => SetProperty(_model, nameof(_model.UseAutomaticIdleProfile), value);
        }

        /// <summary>
        /// Gets or sets the selected profile name.
        /// </summary>
        [TrackState]
        public string SelectedProfileName
        {
            get => _model.IdleProfileName;
            set => SetProperty(_model, nameof(_model.IdleProfileName), value);
        }

        /// <summary>
        /// Gets or sets whether to hide the Idle Profile in the Quick Menu.
        /// </summary>
        [TrackState]
        public bool HideIdleProfileInQuickMenu
        {
            get => _model.HideIdleProfileInQuickMenu;
            set => SetProperty(_model, nameof(_model.HideIdleProfileInQuickMenu), value);
        }

        /// <summary>
        /// Gets or sets how to long to wait before AudioBand goes into an idle state.
        /// </summary>
        [TrackState]
        public int ShouldGoIdleAfterInSeconds
        {
            get => _model.ShouldGoIdleAfterInSeconds;
            set => SetProperty(_model, nameof(_model.ShouldGoIdleAfterInSeconds), value);
        }

        /// <summary>
        /// Gets or sets whether to clear the current session information when it goes into idle.
        /// </summary>
        [TrackState]
        public bool ClearSessionOnIdle
        {
            get => _model.ClearSessionOnIdle;
            set => SetProperty(_model, nameof(_model.ClearSessionOnIdle), value);
        }

        /// <summary>
        /// Gets or sets whether to show information popups to the user.
        /// </summary>
        [TrackState]
        public bool ShowInformationPopups
        {
            get => _model.ShowInformationPopups;
            set => SetProperty(_model, nameof(_model.ShowInformationPopups), value);
        }

        /// <summary>
        /// Gets or sets whether or not an update is available.
        /// </summary>
        public bool UpdateIsAvailable
        {
            get => _updateIsAvailable;
            set => SetProperty(ref _updateIsAvailable, value);
        }

        /// <summary>
        /// Gets or sets whether an update was found.
        /// </summary>
        public bool NoUpdateFound
        {
            get => _noUpdateFound;
            set => SetProperty(ref _noUpdateFound, value);
        }

        /// <summary>
        /// Gets or sets whether the user is downloading the latest update.
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            private set => SetProperty(ref _isDownloading, value);
        }

        /// <summary>
        /// Gets or sets the current download percentage.
        /// </summary>
        public int DownloadPercentage
        {
            get => _downloadPercentage;
            set => SetProperty(ref _downloadPercentage, value);
        }

        /// <summary>
        /// Gets the list of profile names.
        /// </summary>
        public ObservableCollection<string> ProfileNames { get; private set; }

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
            MapSelf(_model, _appSettings.AudioBandSettings);
        }

        private async Task CheckForUpdatesCommandOnExecute()
        {
            if (!await _gitHubHelper.IsOnLatestVersionAsync())
            {
                UpdateIsAvailable = true;
                NoUpdateFound = false;
            }
            else
            {
                NoUpdateFound = true;
                UpdateIsAvailable = false;
            }
        }

        private async Task InstallUpdateCommandOnExecute()
        {
            try
            {
                _fileName = Path.GetTempFileName().Replace(".tmp", ".msi");

                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += OnDownloadProgressChanged;
                    client.DownloadFileCompleted += OnDownloadCompleted;

                    IsDownloading = true;
                    var link = new Uri(await _gitHubHelper.GetLatestDownloadUrlAsync());
                    client.DownloadFileAsync(link, _fileName);
                }
            }
            catch (Exception e)
            {
                Logger.Info(e.Message);
                Logger.Error(e);
            }
        }

        private void OnDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            IsDownloading = false;

            if (e.Cancelled)
            {
                return;
            }

            try
            {
                var p = Process.Start(new ProcessStartInfo()
                {
                    FileName = "msiexec",
                    WorkingDirectory = @"C:\temp\",
                    Arguments = $"/i {_fileName}",
                    Verb = "Runas",
                });
            }
            catch (Exception exc)
            {
                Logger.Info(exc.Message);
                Logger.Error(exc);
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadPercentage = e.ProgressPercentage;
        }

        private void AppsettingsOnProfileChanged(object sender, EventArgs e)
        {
            Debug.Assert(IsEditing == false, "Should not be editing");
            MapSelf(_appSettings.AudioBandSettings, _model);
            RaisePropertyChangedAll();
        }

        private void OnProfilesUpdated(ProfilesUpdatedMessage msg)
        {
            ProfileNames = new ObservableCollection<string>(_appSettings.Profiles.Select(p => p.Name));
            RaisePropertyChanged(nameof(ProfileNames));

            // if idle profile was renamed, update it to the new one
            if (_appSettings.AudioBandSettings.IdleProfileName != SelectedProfileName)
            {
                SelectedProfileName = _appSettings.AudioBandSettings.IdleProfileName;
            }
        }
    }
}
