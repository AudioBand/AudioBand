namespace AudioBand.Messages
{
    /// <summary>
    /// Messages for profile change.
    /// </summary>
    public enum ProfilesUpdatedMessage
    {
        /// <summary>
        /// Profile was created.
        /// </summary>
        ProfileCreated,

        /// <summary>
        /// Profile was deleted.
        /// </summary>
        ProfileDeleted,

        /// <summary>
        /// Profile was renamed.
        /// </summary>
        ProfileRenamed,

        /// <summary>
        /// A new profile was selected.
        /// </summary>
        ProfileSelected
    }
}
