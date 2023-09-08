﻿namespace AudioBand.Models
{
    /// <summary>
    /// Model for the album art.
    /// </summary>
    public class AlbumArt : LayoutModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumArt"/> class.
        /// </summary>
        public AlbumArt()
        {
            Width = 30;
            Height = 30;
            XPosition = 210;
            YPosition = 0;
        }

        /// <summary>
        /// Gets or sets the path of the placeholder image.
        /// </summary>
        public string PlaceholderPath { get; set; } = "";

        /// <summary>
        /// Gets or sets the Corner Radius.
        /// </summary>
        public int CornerRadius { get; set; } = 4;
    }
}
