using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AudioBand.Messages;
using AudioBand.Models;
using AudioBand.Settings.Persistence;

namespace AudioBand.Settings
{
    /// <summary>
    /// Manages application settings.
    /// </summary>
    public class AppSettings : IAppSettings
    {
        private readonly IPersistentSettings _persistSettings;
        private readonly IMessageBus _messageBus;
        private Dictionary<string, UserProfile> _profiles = new Dictionary<string, UserProfile>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings"/> class.
        /// </summary>
        /// <param name="persistSettings">The settings persistence object.</param>
        /// /// <param name="messageBus">The message bus.</param>
        public AppSettings(IPersistentSettings persistSettings, IMessageBus messageBus)
        {
            _persistSettings = persistSettings;
            _messageBus = messageBus;

            try
            {
                _persistSettings.CheckAndConvertOldSettings();
            }
            catch (Exception)
            {
                // log? - something went wrong, ignore
            }

            var settings = _persistSettings.ReadSettings();
            DoSettingsNullChecks(settings);

            AudioSource = settings.CurrentAudioSource;
            AudioSourceSettings = settings.AudioSourceSettings?.ToList() ?? new List<AudioSourceSettings>();
            AudioBandSettings = settings.AudioBandSettings ?? new AudioBandSettings();

            CheckAndLoadProfiles(settings, _persistSettings.ReadProfiles().ToArray());
            var profileName = AudioBandSettings.UseAutomaticIdleProfile && !string.IsNullOrEmpty(AudioBandSettings.LastNonIdleProfileName)
                            ? AudioBandSettings.LastNonIdleProfileName : settings.CurrentProfileName;
            SelectProfile(profileName);

            AudioBandSettings.LastKnownVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        /// <inheritdoc />
        public event EventHandler ProfileChanged;

        /// <inheritdoc />
        public string AudioSource { get; set; }

        /// <inheritdoc />
        public List<AudioSourceSettings> AudioSourceSettings { get; }

        /// <inheritdoc />
        public AudioBandSettings AudioBandSettings { get; }

        /// <inheritdoc />
        public UserProfile CurrentProfile { get; private set; }

        /// <inheritdoc />
        public IEnumerable<UserProfile> Profiles => _profiles.Values;

        /// <inheritdoc />
        public void SelectProfile(string profileName)
        {
            if (CurrentProfile?.Name == profileName || !_profiles.ContainsKey(profileName))
            {
                return;
            }

            CurrentProfile = _profiles[profileName];
            ProfileChanged?.Invoke(this, EventArgs.Empty);

            if (!string.IsNullOrEmpty(CurrentProfile?.Name))
            {
                AudioBandSettings.LastNonIdleProfileName = CurrentProfile.Name;
            }

            _messageBus.Publish(ProfilesUpdatedMessage.ProfileSelected);
            Save();
        }

        /// <inheritdoc />
        public void CreateProfile(string profileName)
        {
            if (profileName == null)
            {
                throw new ArgumentNullException(nameof(profileName));
            }

            if (_profiles.ContainsKey(profileName))
            {
                throw new ArgumentException("Profile name already exists", nameof(profileName));
            }

            _profiles.Add(profileName, UserProfile.CreateDefaultProfile(profileName));
        }

        /// <inheritdoc />
        public void CreateProfile(UserProfile profile)
        {
            if (_profiles.ContainsKey(profile.Name))
            {
                throw new ArgumentException("Profile name already exists", nameof(profile.Name));
            }

            _profiles.Add(profile.Name, profile);
            SelectProfile(profile.Name);
        }

        /// <inheritdoc />
        public void DeleteProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                throw new ArgumentNullException(nameof(profileName));
            }

            if (_profiles.Count == 1)
            {
                throw new InvalidOperationException("Must have at least one profile");
            }

            if (!_profiles.ContainsKey(profileName))
            {
                throw new ArgumentException($"Profile {profileName} does not exist", nameof(profileName));
            }

            if (AudioBandSettings.IdleProfileName == profileName)
            {
                AudioBandSettings.UseAutomaticIdleProfile = false;
                AudioBandSettings.IdleProfileName = _profiles.First().Key;
            }

            if (AudioBandSettings.LastNonIdleProfileName == profileName)
            {
                AudioBandSettings.LastNonIdleProfileName = _profiles.First().Key;
            }

            _profiles.Remove(profileName);
            _persistSettings.DeleteProfile(profileName);
        }

        /// <inheritdoc />
        public void RenameCurrentProfile(string newProfileName)
        {
            if (newProfileName == null)
            {
                throw new ArgumentNullException(nameof(newProfileName));
            }

            if (CurrentProfile == null)
            {
                throw new InvalidOperationException("No profile selected. Current profile is null");
            }

            if (_profiles.ContainsKey(newProfileName))
            {
                throw new ArgumentException("Profile already exists", nameof(newProfileName));
            }

            _persistSettings.DeleteProfile(CurrentProfile.Name);
            CurrentProfile.Name = newProfileName;
            Save();
        }

        /// <inheritdoc />
        public void Save()
        {
            _persistSettings.WriteSettings(new Persistence.Settings()
            {
                CurrentAudioSource = AudioSource,
                AudioBandSettings = AudioBandSettings,
                CurrentProfileName = CurrentProfile.Name,
                AudioSourceSettings = AudioSourceSettings
            });

            _persistSettings.WriteProfiles(Profiles);
        }

        /// <inheritdoc />
        public void ImportProfileFromPath(string path)
        {
            var profile = _persistSettings.ReadProfile(path);
            if (profile is null)
            {
                return;
            }

            var name = UserProfile.GetUniqueProfileName(_profiles.Keys, profile.Name);
            _profiles[name] = profile;
            _profiles[name].Name = name;
            _messageBus.Publish(ProfilesUpdatedMessage.ProfileCreated);
        }

        private void CheckAndLoadProfiles(Persistence.Settings settings, UserProfile[] profiles)
        {
            /* If there are no profiles, create new ones, they're automatically saved later.
             * Second line of if statement is for people who have reinstalled audioband
             * while their last version was pre-profiles (v0.9.6) update */
            if (profiles == null || profiles.Length == 0
            || (profiles.Length == 1 && profiles[0].Name == "Default Profile"))
            {
                settings.CurrentProfileName = UserProfile.DefaultProfileName;

                _profiles = new Dictionary<string, UserProfile>();
                var defaultProfiles = UserProfile.CreateDefaultProfiles();

                AudioBandSettings.IdleProfileName = UserProfile.DefaultIdleProfileName;

                for (int i = 0; i < defaultProfiles.Length; i++)
                {
                    _profiles.Add(defaultProfiles[i].Name, defaultProfiles[i]);
                }

                return;
            }
            else if (profiles.FirstOrDefault(x => x.Name == AudioBandSettings.IdleProfileName) == null)
            {
                // if the profile doesn't exist, disable idle profile
                AudioBandSettings.IdleProfileName = profiles.First().Name;
                AudioBandSettings.UseAutomaticIdleProfile = false;
            }

            // Check if we need to adapt DefaultProfile to include volume button.
            var defaultProfile = profiles.FirstOrDefault(x => x.Name == UserProfile.DefaultProfileName);
            var isAfterVolume = new SemanticVersion(AudioBandSettings.LastKnownVersion).IsNewerVersionThan(new SemanticVersion(1, 0, 3));

            if (defaultProfile != null && !isAfterVolume)
            {
                profiles[Array.IndexOf(profiles, defaultProfile)] = UserProfile.CreateDefaultProfile(UserProfile.DefaultProfileName);
            }

            DoProfileNullChecks(ref profiles);
            _profiles = profiles.ToDictionary(profile => profile.Name, profile => profile);

            if (settings.CurrentProfileName == null || !_profiles.ContainsKey(settings.CurrentProfileName))
            {
                settings.CurrentProfileName = _profiles.First().Key;
            }
        }

        private void DoSettingsNullChecks(Persistence.Settings settings)
        {
            if (settings.AudioBandSettings.MouseBindings == null || settings.AudioBandSettings.MouseBindings.Count == 0)
            {
                settings.AudioBandSettings.MouseBindings = new List<MouseBinding>()
                {
                    new MouseBinding()
                    {
                        MouseInputType = MouseInputType.LeftDoubleClick,
                        CommandType = MouseBindingCommandType.OpenAssociatedApp
                    },
                    new MouseBinding()
                    {
                        MouseInputType = MouseInputType.ScrollUp,
                        CommandType = MouseBindingCommandType.PreviousSong
                    },
                    new MouseBinding()
                    {
                        MouseInputType = MouseInputType.ScrollDown,
                        CommandType = MouseBindingCommandType.NextSong
                    },
                };
            }
        }

        private void DoProfileNullChecks(ref UserProfile[] profiles)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                profiles[i].VolumeButton ??= new VolumeButton();
                profiles[i].LikeButton ??= new LikeButton();
            }
        }
    }
}
