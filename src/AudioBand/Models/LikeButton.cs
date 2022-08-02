namespace AudioBand.Models
{
    /// <summary>
    /// Model for the like/dislike button.
    /// </summary>
    public class LikeButton : ButtonModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LikeButton"/> class.
        /// </summary>
        public LikeButton()
        {
            IsVisible = false;
            XPosition = 170;
            YPosition = 3;
            Width = 40;
            Height = 15;
        }

        /// <summary>
        /// Gets or sets the button content for the like state.
        /// </summary>
        public ButtonContent LikeContent { get; set; } = new ButtonContent
        {
            Text = "",
        };

        /// <summary>
        /// Gets or sets the button content for the dislike state.
        /// </summary>
        public ButtonContent DislikeContent { get; set; } = new ButtonContent
        {
            Text = "",
        };
    }
}
