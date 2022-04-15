using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioBand.Logging;
using AudioBand.Models;
using AudioBand.Settings.Migrations;
using AudioBand.UI;
using Nett;
using Newtonsoft.Json;
using NLog;

// Alias the current settings version
using OldTomlSettings = AudioBand.Settings.Models.V4.SettingsV4;

namespace AudioBand.Settings.Persistence
{
    /// <summary>
    /// Reads and writes persisted settings. The current type of the settings will change depending on the latest version.
    /// </summary>
    public class PersistentSettings : IPersistentSettings
    {
        private static readonly string MainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioBand");
        private static readonly string ProfilesDirectory = Path.Combine(MainDirectory, "Profiles");
        private static readonly string SettingsFilePath = Path.Combine(MainDirectory, "settings.json");
        private static readonly ILogger Logger = AudioBandLogManager.GetLogger<PersistentSettings>();

        private static readonly Dictionary<string, Type> SettingsTypeTable = new Dictionary<string, Type>()
        {
            { "0.1", typeof(Models.V1.AudioBandSettings) },
            { "2", typeof(Models.V2.SettingsV2) },
            { "3", typeof(Models.V3.SettingsV3) },
            { "4", typeof(Models.V4.SettingsV4) },
        };

        private PopupService _popups = PopupService.Instance;

        /// <inheritdoc />
        public void CheckAndConvertOldSettings()
        {
            var oldSettingsFilePath = Path.Combine(MainDirectory, "audioband.settings");

            if (!File.Exists(oldSettingsFilePath))
            {
                return;
            }

            // Make a backup so we can delete later
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            File.Copy(oldSettingsFilePath, Path.Combine(MainDirectory, $"old-audioband-settings-{unixTime}.backup"));
            var oldSettings = LoadOldSettings(oldSettingsFilePath);

            if (!File.Exists(SettingsFilePath))
            {
                WriteSettings(new Settings()
                {
                    CurrentAudioSource = oldSettings.AudioSource,
                    CurrentProfileName = oldSettings.CurrentProfileName,
                    AudioBandSettings = oldSettings.AudioBandSettings,
                    AudioSourceSettings = oldSettings.AudioSourceSettings
                });
            }

            if (!Directory.Exists(ProfilesDirectory) || GetAllProfileFiles().Length <= 0)
            {
                WriteProfiles(oldSettings.Profiles);
            }

            File.Delete(oldSettingsFilePath);
        }

        /// <inheritdoc />
        public Settings ReadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Directory.CreateDirectory(MainDirectory);
                var settings = new Settings();

                WriteSettings(settings);
                return settings;
            }

            string content = "";

            try
            {
                content = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<Settings>(content);

                if (settings == null)
                {
                    _popups.ShowPopup("FileCorruptedTitle", "FileCorruptedDescription", TimeSpan.FromSeconds(20));
                    throw new Exception("The setting file was null / corrupted.");
                }

                return settings;
            }
            catch (Exception e)
            {
                // take a backup of the file and reset settings
                Logger.Error(e);
                var backupPath = Path.Combine(MainDirectory, $"settings.json.backup-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                File.WriteAllText(backupPath, content);

                var newSettings = new Settings();
                WriteSettings(newSettings);
                return newSettings;
            }
        }

        /// <inheritdoc />
        public void WriteSettings(Settings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to write settings.");
                Logger.Error(e);
            }
        }

        /// <inheritdoc />
        public UserProfile ReadProfile(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserProfile>(json);
            }
            catch (Exception)
            {
                Logger.Error($"Failed to load profile from {path}.");
                return null;
            }
        }

        /// <inheritdoc />
        public IEnumerable<UserProfile> ReadProfiles()
        {
            var userProfiles = new List<UserProfile>();

            if (!Directory.Exists(ProfilesDirectory))
            {
                Directory.CreateDirectory(ProfilesDirectory);
                return userProfiles;
            }

            var fileNames = GetAllProfileFiles();

            for (int i = 0; i < fileNames.Length; i++)
            {
                try
                {
                    var json = File.ReadAllText(fileNames[i]);
                    var loadedProfile = JsonConvert.DeserializeObject<UserProfile>(json);

                    if (loadedProfile == null)
                    {
                        DeleteProfile(fileNames[i]);
                        continue;
                    }

                    userProfiles.Add(loadedProfile);
                }
                catch (Exception)
                {
                    // file might not be a userprofile, just skip it
                    Logger.Info($"{fileNames[i]} was found but could not be parsed into a working profile.");
                    continue;
                }
            }

            return userProfiles;
        }

        /// <inheritdoc />
        public void WriteProfiles(IEnumerable<UserProfile> userProfiles)
        {
            if (!Directory.Exists(ProfilesDirectory))
            {
                Directory.CreateDirectory(ProfilesDirectory);
            }

            var profiles = userProfiles.ToArray();

            for (int i = 0; i < profiles.Length; i++)
            {
                var json = JsonConvert.SerializeObject(profiles[i], Formatting.Indented);
                var path = Path.Combine(ProfilesDirectory, $"{profiles[i].Name}.profile.json");

                File.WriteAllText(path, json);
            }
        }

        /// <inheritdoc />
        public void DeleteProfile(string profileName)
        {
            try
            {
                var path = Path.Combine(ProfilesDirectory, $"{profileName}.profile.json");
                File.Delete(path);
            }
            catch (Exception) { }
        }

        private string[] GetAllProfileFiles()
            => Directory.GetFiles(ProfilesDirectory).Where(x => x.EndsWith(".profile.json")).ToArray();

        private OldTomlSettings LoadOldSettings(string path)
        {
            try
            {
                var tomlFile = Toml.ReadFile(path, TomlHelper.DefaultSettings);
                var version = tomlFile["Version"].Get<string>();

                if (version != "4")
                {
                    Toml.WriteFile(tomlFile, Path.Combine(MainDirectory, $"old-audioband.settings.{version}"), TomlHelper.DefaultSettings);
                    return SettingsMigration.MigrateSettings<OldTomlSettings>(tomlFile.Get(SettingsTypeTable[version]), version, "4");
                }

                return tomlFile.Get<OldTomlSettings>();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to read old settings file: {e.Message}");
                return new OldTomlSettings();
            }
        }
    }
}
