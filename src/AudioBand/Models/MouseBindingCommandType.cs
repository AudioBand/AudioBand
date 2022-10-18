namespace AudioBand.Models
{
    /// <summary>
    /// The command type related to the mouse binding.
    /// </summary>
    public enum MouseBindingCommandType
    {
        /// <summary>
        /// Switches to the next profile.
        /// </summary>
        NextProfile = 0,

        /// <summary>
        /// Switches to the previous profile.
        /// </summary>
        PreviousProfile = 1,

        /// <summary>
        /// Plays/Resumes whatever is playing.
        /// </summary>
        Play = 2,

        /// <summary>
        /// Pauses whatever is playing.
        /// </summary>
        Pause = 3,

        /// <summary>
        /// Switches to the next song.
        /// </summary>
        NextSong = 4,

        /// <summary>
        /// Switches to the previous song.
        /// </summary>
        PreviousSong = 5,

        /// <summary>
        /// Switches to the next repeat mode.
        /// </summary>
        NextRepeatMode = 6,

        /// <summary>
        /// Switches to the previous repeat mode.
        /// </summary>
        PreviousRepeatMode = 7,

        /// <summary>
        /// Toggles shuffle mode.
        /// </summary>
        ToggleShuffleMode = 8,

        /// <summary>
        /// Puts the volume higher.
        /// </summary>
        VolumeHigher = 9,

        /// <summary>
        /// Puts the volume lower.
        /// </summary>
        VolumeLower = 10,

        /// <summary>
        /// Opens the associated music app if possible.
        /// </summary>
        OpenAssociatedApp = 11,

        /// <summary>
        /// Toggles the players play/pause.
        /// </summary>
        TogglePlayPause = 12,

        /// <summary>
        /// Switches to the next AudioSource.
        /// </summary>
        NextAudioSource = 13,

        /// <summary>
        /// Switches to the previous AudioSource.
        /// </summary>
        PreviousAudioSource = 14,
    }
}
