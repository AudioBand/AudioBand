namespace AudioBand.Models
{
    /// <summary>
    /// Model for the like/dislike button.
    /// </summary>
    public class LikeDislikeButton : ButtonModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LikeDislikeButton"/> class.
        /// </summary>
        public LikeDislikeButton()
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
            Text = "",
        };

        /// <summary>
        /// Gets or sets the button content for the dislike state.
        /// </summary>
        public ButtonContent DislikeContent { get; set; } = new ButtonContent
        {
            Text = "",
        };
    }
}
