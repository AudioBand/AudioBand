using System.Text.RegularExpressions;

namespace AudioBand.Models
{
    /// <summary>
    /// A class to represent Semantic Versioning.
    /// </summary>
    public class SemanticVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        public SemanticVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
        /// </summary>
        /// <param name="version">Version in format v1.0.0.</param>
        public SemanticVersion(string version)
        {
            var regex = Regex.Match(version, "\\d+\\.\\d+\\.\\d+");

            if (!regex.Success)
            {
                Major = 0;
                Minor = 0;
                Patch = 0;
                return;
            }

            version = version.StartsWith("v") ? version.Remove(0, 1) : version;
            var splits = version.Split('.');

            Major = int.Parse(splits[0]);
            Minor = int.Parse(splits[1]);
            Patch = int.Parse(splits[2]);
        }

        /// <summary>
        /// Gets the Major version.
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// Gets the Minor version.
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// Gets the Patch version.
        /// </summary>
        public int Patch { get; private set; }

        /// <summary>
        /// Compares whether one version is newer than another.
        /// </summary>
        /// <param name="other">The other version to compare against.</param>
        /// <returns>Whether or not the current version is latest.</returns>
        public bool IsNewerVersionThan(SemanticVersion other)
        {
            return Major > other.Major
                    || (Major == other.Major && Minor > other.Minor)
                    || (Major == other.Major && Minor == other.Minor && Patch > other.Patch);
        }
    }
}