namespace AudioBand.Messages
{
    /// <summary>
    /// Messages for audiosource change.
    /// </summary>
    public enum AudioSourceUpdatedMessage
    {
        /// <summary>
        /// AudioSource was created.
        /// </summary>
        AudioSourceCreated,

        /// <summary>
        /// AudioSource was deleted.
        /// </summary>
        AudioSourceDeleted,

        /// <summary>
        /// A new AudioSource was selected.
        /// </summary>
        AudioSourceSelected
    }
}
