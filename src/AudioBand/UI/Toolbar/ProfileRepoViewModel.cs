using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioBand.Commands;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;
using Newtonsoft.Json;

namespace AudioBand.UI
{
    /// <summary>
    /// ViewModel for ProfileRepo.
    /// </summary>
    public class ProfileRepoViewModel : ViewModelBase
    {
        private readonly IAppSettings _appSettings;
        private readonly GitHubHelper _gitHub;
        private readonly IMessageBus _messageBus;

        private readonly AudioBandSettings _model = new AudioBandSettings();
        private readonly AudioBandSettings _backup = new AudioBandSettings();

        private readonly string _assetsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioBand", "Assets").Replace(@"\", @"\\");
        private bool _isDownloading;
        private bool _refreshDisabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileRepoViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app's settings.</param>
        /// <param name="gitHub">The GitHub helper.</param>
        /// /// <param name="messageBus">The message bus.</param>
        public ProfileRepoViewModel(IAppSettings appSettings, GitHubHelper gitHub, IMessageBus messageBus)
        {
            _appSettings = appSettings;
            _gitHub = gitHub;
            _messageBus = messageBus;

            AvailableProfiles = new ObservableCollection<CommunityProfile>(_gitHub.GetCommunityProfiles().GetAwaiter().GetResult());
            InstallProfileCommand = new AsyncRelayCommand<string>(OnInstallProfileCommandExecuted);
            UpdateProfileCommand = new AsyncRelayCommand<string>(OnUpdateProfileCommandExecuted);
            DeleteProfileCommand = new RelayCommand<string>(OnDeleteProfileCommandExecuted);
            RefreshProfilesCommand = new AsyncRelayCommand(OnRefreshProfilesCommand);

            IsDownloading = false;
            IsRefreshDisabled = false;
        }

        /// <summary>
        /// Gets or sets the list of available profiles.
        /// </summary>
        public ObservableCollection<CommunityProfile> AvailableProfiles { get; set; }

        /// <summary>
        /// Gets or sets whether the user is downloading the latest update.
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            private set => SetProperty(ref _isDownloading, value);
        }

        /// <summary>
        /// Gets or sets whether Refresh button is still disabled.
        /// </summary>
        public bool IsRefreshDisabled
        {
            get => _refreshDisabled;
            private set => SetProperty(ref _refreshDisabled, value);
        }

        /// <summary>
        /// Gets or sets the Install Profile Command.
        /// </summary>
        public ICommand InstallProfileCommand { get; set; }

        /// <summary>
        /// Gets or sets the Update Profile Command.
        /// </summary>
        public ICommand UpdateProfileCommand { get; set; }

        /// <summary>
        /// Gets or sets the Delete Profile Command.
        /// </summary>
        public ICommand DeleteProfileCommand { get; set; }

        /// <summary>
        /// Gets or sets Refresh Profiles Command.
        /// </summary>
        public ICommand RefreshProfilesCommand { get; set; }

        /// <summary>
        /// Gets the command to open the link to the project.
        /// </summary>
        public ICommand OpenLinkCommand { get; } = new RelayCommand<string>(OpenLinkCommandOnExecute);

        /// <summary>
        /// Gets the Community Profiles link.
        /// </summary>
        public string CommunityProfileProjectLink => "https://github.com/AudioBand/CommunityProfiles";

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

        private async Task OnInstallProfileCommandExecuted(string profileName)
        {
            IsDownloading = true;
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == profileName);

            if (communityProfile == null || communityProfile.IsInstalled)
            {
                return;
            }

            await InstallProfileAsync(communityProfile);
            ForceUpdateCollection();
            IsDownloading = false;
        }

        private async Task OnUpdateProfileCommandExecuted(string profileName)
        {
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == profileName);

            if (communityProfile == null || !communityProfile.IsInstalled)
            {
                return;
            }

            var currentSelectedProfile = _appSettings.CurrentProfile.Name;

            _appSettings.DeleteProfile(profileName);
            await InstallProfileAsync(communityProfile);

            communityProfile.IsLatestVersion = true;
            ForceUpdateCollection();

            if (currentSelectedProfile == profileName)
            {
                _appSettings.SelectProfile(profileName);
            }
        }

        private void OnDeleteProfileCommandExecuted(string profileName)
        {
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == profileName);

            if (communityProfile == null || !communityProfile.IsInstalled)
            {
                return;
            }

            _appSettings.DeleteProfile(profileName);
            _messageBus.Publish(ProfilesUpdatedMessage.ProfileDeleted);

            communityProfile.IsInstalled = false;
            ForceUpdateCollection();
        }

        private async Task OnRefreshProfilesCommand()
        {
            IsRefreshDisabled = true;
            AvailableProfiles = new ObservableCollection<CommunityProfile>(await _gitHub.GetCommunityProfiles());
            ForceUpdateCollection();

            await Task.Delay(30000).ContinueWith(t => IsRefreshDisabled = false);
        }

        private async Task InstallProfileAsync(CommunityProfile communityProfile)
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(communityProfile.DownloadUrl);

                // check if profile uses external images
                if (!string.IsNullOrEmpty(communityProfile.AssetsUrl))
                {
                    // change out image paths placeholder to relative paths on the user's device
                    json = json.Replace("%AssetsFolder%", _assetsDirectory);

                    // make sure assets folder exists
                    if (!Directory.Exists(_assetsDirectory))
                    {
                        Directory.CreateDirectory(_assetsDirectory);
                    }

                    // download the associated assets
                    await _gitHub.DownloadProfileAssetsAsync(communityProfile);
                }

                var profile = JsonConvert.DeserializeObject<UserProfile>(json);
                _appSettings.CreateProfile(profile);

                communityProfile.IsInstalled = true;
                communityProfile.IsLatestVersion = true;
                _messageBus.Publish(ProfilesUpdatedMessage.ProfileCreated);
            }
            catch (Exception e)
            {
                Logger.Error($"Tried to download and install using Profile Repo, but failed. User most likely offline or an error in the online profile.");
                Logger.Error(e);
            }
        }

        private void ForceUpdateCollection()
        {
            var profiles = AvailableProfiles.ToArray();

            AvailableProfiles.Clear();
            AvailableProfiles = new ObservableCollection<CommunityProfile>(profiles);
            RaisePropertyChangedAll();
        }
        private static void OpenLinkCommandOnExecute(string link)
        {
            Process.Start(link);
        }
    }
}
