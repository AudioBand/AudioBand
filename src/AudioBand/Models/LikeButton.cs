using System.Windows.Media;

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
            XPosition = 0;
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
            TextColor = Color.FromRgb(255, 0, 0),
            HoveredTextColor = Color.FromRgb(221, 54, 54),
            ClickedTextColor = Color.FromRgb(165, 25, 25)
        };
    }
}
