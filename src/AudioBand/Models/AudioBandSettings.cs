using System.Reflection;

namespace AudioBand.Models
{
    /// <summary>
    /// Collection of global AudioBand settings.
    /// </summary>
    public class AudioBandSettings
    {
        private string _lastNonIdleProfileName = "";

        /// <summary>
        /// Gets the last active profile name before going into idle.
        /// </summary>
        public string LastNonIdleProfileName
        {
            get => _lastNonIdleProfileName;
            set
            {
                if (value == IdleProfileName)
                {
                    return;
                }

                _lastNonIdleProfileName = value;
            }
        }

        /// <summary>
        /// Gets or sets the last known run version by the user.
        /// This can be used for version specific updates.
        /// </summary>
        public string LastKnownVersion { get; set; } = "v0.0.0";

        /// <summary>
        /// The profile that should enable when AudioBand goes into idle mode.
        /// </summary>
        public string IdleProfileName { get; set; } = "Idle";

        /// <summary>
        /// Gets or sets whether to use the Idle Profile.
        /// </summary>
        public bool UseAutomaticIdleProfile { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to hide the Idle profile in the Profiles quick menu.
        /// </summary>
        public bool HideIdleProfileInQuickMenu { get; set; } = true;

        /// <summary>
        /// Gets or sets the amount of time in seconds it should take for AudioBand to go idle.
        /// </summary>
        public int ShouldGoIdleAfterInSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets whether to clear the current session information when it goes into idle.
        /// </summary>
        public bool ClearSessionOnIdle { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show a popup when an update is available.
        /// </summary>
        public bool ShowPopupOnAvailableUpdate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show little help popups when something fails.
        /// </summary>
        public bool ShowInformationPopups { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to opt-in for pre-releases.
        /// </summary>
        public bool OptInForPreReleases { get; set; }
    }
}
