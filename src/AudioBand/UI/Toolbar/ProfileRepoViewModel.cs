using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioBand.Commands;
using AudioBand.Logging;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings;
using Newtonsoft.Json;
using NLog;

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
        }

        /// <summary>
        /// Gets or sets the list of available profiles.
        /// </summary>
        public ObservableCollection<CommunityProfile> AvailableProfiles { get; set; }

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
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == profileName);

            if (communityProfile == null || communityProfile.IsInstalled)
            {
                return;
            }

            await InstallProfileAsync(communityProfile);
            ForceUpdateCollection();
        }

        private async Task OnUpdateProfileCommandExecuted(string profileName)
        {
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == profileName);

            if (communityProfile == null || !communityProfile.IsInstalled)
            {
                return;
            }

            _appSettings.DeleteProfile(profileName);
            await InstallProfileAsync(communityProfile);

            communityProfile.IsLatestVersion = true;
            ForceUpdateCollection();
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

        private async Task InstallProfileAsync(CommunityProfile communityProfile)
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(communityProfile.DownloadUrl);

                var profile = JsonConvert.DeserializeObject<UserProfile>(json);
                _appSettings.CreateProfile(profile);

                communityProfile.IsInstalled = true;
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
    }
}
