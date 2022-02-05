namespace AudioBand.Models
{
    /// <summary>
    /// Represents a downloadable community profile.
    /// </summary>
    public class CommunityProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommunityProfile"/> class.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="description">The description of the profile.</param>
        /// <param name="creators">The creator(s) of the profile.</param>
        /// <param name="imageUrl">The profile's image url.</param>
        /// <param name="isInstalled">Whether this profile is installed.</param>
        public CommunityProfile(string name, string description, string creators, string imageUrl, bool isInstalled)
        {
            Name = name;
            Description = description;
            Creators = creators;
            ImageUrl = imageUrl;
            IsInstalled = isInstalled;
        }

        /// <summary>
        /// Gets the name of the Profile.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the Profile.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the creators of the Profile.
        /// </summary>
        public string Creators { get; }

        /// <summary>
        /// Gets the Profile's example image url.
        /// </summary>
        public string ImageUrl { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this profile is already installed.
        /// </summary>
        public bool IsInstalled { get; set; }
    }
}
