using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioBand.Commands;
using AudioBand.Logging;
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

        private readonly AudioBandSettings _model = new AudioBandSettings();
        private readonly AudioBandSettings _backup = new AudioBandSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileRepoViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app's settings.</param>
        /// /// <param name="gitHub">The GitHub communicator.</param>
        public ProfileRepoViewModel(IAppSettings appSettings, GitHubHelper gitHub)
        {
            _appSettings = appSettings;
            _gitHub = gitHub;

            AvailableProfiles = new ObservableCollection<CommunityProfile>(_gitHub.GetCommunityProfiles().GetAwaiter().GetResult());
            InstallButtonCommand = new AsyncRelayCommand<string>(OnInstallButtonExecutedCommand);
        }

        /// <summary>
        /// Gets or sets the list of available profiles.
        /// </summary>
        public ObservableCollection<CommunityProfile> AvailableProfiles { get; set; }

        /// <summary>
        /// Gets or sets the install button command.
        /// </summary>
        public ICommand InstallButtonCommand { get; set; }

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

        private async Task OnInstallButtonExecutedCommand(string name)
        {
            var communityProfile = AvailableProfiles.FirstOrDefault(x => x.Name == name);

            if (communityProfile == null || communityProfile.IsInstalled)
            {
                return;
            }

            // Install profile
            try
            {
                using (var client = new HttpClient())
                {
                    var json = await client.GetStringAsync(communityProfile.DownloadUrl);

                    var profile = JsonConvert.DeserializeObject<UserProfile>(json);
                    _appSettings.CreateProfile(profile);

                    communityProfile.IsInstalled = true;
                    communityProfile.IsLatestVersion = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Tried to download and install using Profile Repo, but failed. User most likely offline or an error in the online profile.");
                Logger.Error(e);
                return;
            }

            ForceUpdateCollection();
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
