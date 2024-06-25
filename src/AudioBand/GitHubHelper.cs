using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AudioBand.Logging;
using AudioBand.Models;
using AudioBand.Settings;
using AudioBand.UI;
using Newtonsoft.Json;
using NLog;
using Octokit;

namespace AudioBand
{
    /// <summary>
    /// Helper to interact with the GitHub api.
    /// </summary>
    public class GitHubHelper
    {
        private const string OrganizationName = "AudioBand";
        private const string CommunityProfilesRepository = "CommunityProfiles";
        private const string CommunityAudiosourcesRepository = "CommunityAudiosources";

        private static readonly ILogger Logger = AudioBandLogManager.GetLogger<GitHubHelper>();
        private readonly string _assetsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioBand", "Assets");

        private RepositoryContent[] _communityProfiles;
        private RepositoryContent[] _communityAudiosources;

        private IAppSettings _appSettings;
        private GitHubClient _client = new GitHubClient(new ProductHeaderValue(OrganizationName));

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubHelper"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.<param/>
        public GitHubHelper(IAppSettings appSettings)
        {
            _appSettings = appSettings;

            _client.Connection.SetRequestTimeout(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Gets the Download url of the latest release.
        /// </summary>
        /// <returns>A string with a Url of the latest release download.</returns>
        public async Task<string> GetLatestDownloadUrlAsync()
        {
            var release = await GetLatestRelease();
            return release.Assets.FirstOrDefault(x => x.Name == "AudioBand.msi")?.BrowserDownloadUrl;
        }

        /// <summary>
        /// Gets whether the user is currently running the latest version of AudioBand.
        /// </summary>
        /// <returns>Whether the user is on the latest version.</returns>
        public async Task<bool> IsOnLatestVersionAsync()
        {
            try
            {
                var currentVersion = GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                string latestVersion = "";

                latestVersion = (await GetLatestRelease()).Name.Split(' ')[1];

                // custom build or forgot to change version on compile, exclude from updates
                if (currentVersion == "$version$")
                {
                    return true;
                }

                var current = new SemanticVersion(currentVersion);
                var latest = new SemanticVersion(latestVersion);

                if (current.ParseFailed || latest.ParseFailed)
                {
                    return true;
                }

                return !latest.IsNewerVersionThan(current);
            }
            catch (Exception)
            {
                Logger.Warn("Could not check for updates, request to GitHub failed.");
                return true;
            }
        }

        /// <summary>
        /// Gets the community profiles found on the repository.
        /// </summary>
        /// <returns>An array of CommunityProfiles.</returns>
        public async Task<CommunityProfile[]> GetCommunityProfiles()
        {
            try
            {
                string json = "";

                // get json string
                using (var client = new HttpClient())
                {
                    json = await client.GetStringAsync("https://raw.githubusercontent.com/AudioBand/CommunityProfiles/master/ProfilesSummary.json");
                }

                // no need to replace the %AssetsFolder% placeholder
                var profiles = JsonConvert.DeserializeObject<CommunityProfile[]>(json);
                profiles = profiles?.OrderBy(x => x.Name).ToArray();

                // check whether we have this profile installed
                for (int i = 0; i < profiles?.Length; i++)
                {
                    var profileMatch = _appSettings.Profiles.FirstOrDefault(x => x.Name == profiles[i].Name);

                    if (profileMatch != null)
                    {
                        profiles[i].IsInstalled = true;
                        profiles[i].IsLatestVersion = !new SemanticVersion(profiles[i].Version).IsNewerVersionThan(new SemanticVersion(profileMatch.Version));
                    }
                }

                return profiles;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to get community profiles from the repository. Error message: {e.Message}");
                return new CommunityProfile[] { };
            }
        }

        /// <summary>
        /// Gets the Community Audiosources found on the repository.
        /// </summary>
        /// <returns>An array of the Community Audiosources.</returns>
        public async Task<CommunityAudiosource[]> GetCommunityAudiosourcesAsync()
        {
            return null;
            var profiles = new List<CommunityAudiosource>();

            // for each profile
            for (int i = 0; i < _communityProfiles.Length; i++)
            {
                var path = _communityProfiles[i].Path;
                var subContents = await _client.Repository.Content.GetAllContents("AudioBand", CommunityAudiosourcesRepository, path);

                var downloadSize = _communityProfiles[i].Size;

                profiles.Add(new CommunityAudiosource($"Audiosource {i}", "A description.", "someone", false, downloadSize));
            }

            return profiles.ToArray();
        }

        /// <summary>
        /// Downloads the Assets associated with a community profile.
        /// Throws exception when anything goes wrong.
        /// </summary>
        /// <returns>Returns a task.</returns>
        /// <param name="profile">The associated profile.</param>
        public async Task DownloadProfileAssetsAsync(CommunityProfile profile)
        {
            var files = await _client.Repository.Content.GetAllContents(OrganizationName, CommunityProfilesRepository, profile.AssetsUrl);
            var assetsFolderPath = Path.Combine(_assetsDirectory, profile.Name);

            await DownloadDirectoryAssetsAsync(files, assetsFolderPath, profile.AssetsUrl);
        }

        private async Task DownloadDirectoryAssetsAsync(IReadOnlyList<RepositoryContent> files, string assetsFolderPath, string assetsUrl)
        {
            if (!Directory.Exists(assetsFolderPath))
            {
                Directory.CreateDirectory(assetsFolderPath);
            }

            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].Type == ContentType.File)
                {
                    using var client = new WebClient();
                    var downloadPath = Path.Combine(assetsFolderPath, files[i].Name);

                    // install any font files
                    if (downloadPath.EndsWith(".ttf"))
                    {
                        client.DownloadFile(new Uri(files[i].DownloadUrl), downloadPath);
                        PopupService.Instance.ShowPopup("InstallFontTitle", "InstallFontDescription");

                        await Task.Delay(500).ContinueWith(x => Process.Start(downloadPath));
                        continue;
                    }

                    client.DownloadFileAsync(new Uri(files[i].DownloadUrl), downloadPath);
                }
                else if (files[i].Type == ContentType.Dir)
                {
                    var newAssetsUrl = Path.Combine(assetsUrl, files[i].Name);
                    var subFolder = await _client.Repository.Content.GetAllContents(OrganizationName, CommunityProfilesRepository, newAssetsUrl);
                    var directory = Path.Combine(assetsFolderPath, files[i].Name);

                    await DownloadDirectoryAssetsAsync(subFolder, directory, newAssetsUrl);
                }
            }
        }

        private async Task<Release> GetLatestRelease()
        {
            try
            {
                return _appSettings.AudioBandSettings.OptInForPreReleases
                    ? (await _client.Repository.Release.GetAll(OrganizationName, OrganizationName))[0]
                    : await _client.Repository.Release.GetLatest(OrganizationName, OrganizationName);
            }
            catch (Exception)
            {
                Logger.Warn("Could not check for updates, request to GitHub failed.");
                return null;
            }
        }
    }
}
