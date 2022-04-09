using Newtonsoft.Json;

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
        /// <param name="authors">The creator(s) of the profile.</param>
        /// <param name="imageUrl">The profile's image url.</param>
        /// <param name="isInstalled">Whether this profile is installed.</param>
        public CommunityProfile(string name, string description, string authors, string imageUrl, bool isInstalled)
        {
            Name = name;
            Description = description;
            Authors = authors;
            ImageUrl = imageUrl;
            IsInstalled = isInstalled;
        }

        /// <summary>
        /// Gets or sets the name of the Profile.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the Profile.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creators of the Profile.
        /// </summary>
        [JsonProperty("authors")]
        public string Authors { get; set; }

        /// <summary>
        /// Gets or sets the version of the Profile.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the Profile's example image url.
        /// </summary>
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the Profile's download url.
        /// </summary>
        [JsonProperty("profileUrl")]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this profile is already installed.
        /// </summary>
        [JsonIgnore]
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Gets or sets whether this profile is on its latest version.
        /// </summary>
        [JsonIgnore]
        public bool IsLatestVersion { get; set; }
    }
}
