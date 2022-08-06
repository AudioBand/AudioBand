namespace AudioBand.Models
{
    /// <summary>
    /// Represents a downloadable community audiosource.
    /// </summary>
    public class CommunityAudiosource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommunityAudiosource"/> class.
        /// </summary>
        /// <param name="app">The name of the app this audiosource is for.</param>
        /// <param name="description">The description of the audiosource.</param>
        /// <param name="creators">The creator(s) of the audiosource.</param>
        /// <param name="isInstalled">Whether this audiosource is installed.</param>
        /// <param name="downloadSize">The size of the audiosource.</param>
        public CommunityAudiosource(string app, string description, string creators, bool isInstalled, int downloadSize)
        {
            App = app;
            Description = description;
            Creators = creators;
            IsInstalled = isInstalled;
            DownloadSize = downloadSize;
        }

        /// <summary>
        /// Gets what app this audiosource is made for.
        /// </summary>
        public string App { get; }

        /// <summary>
        /// Gets the description of this audiosource.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the creator(s) of this audiosource.
        /// </summary>
        public string Creators { get; }

        /// <summary>
        /// Gets a value indicating whether the audiosource is currently installed.
        /// </summary>
        public bool IsInstalled { get; }

        /// <summary>
        /// Gets the total size of this audiosource.
        /// </summary>
        public int DownloadSize { get; }
    }
}
