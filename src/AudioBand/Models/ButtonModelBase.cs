using System.Windows.Media;

namespace AudioBand.Models
{
    /// <summary>
    /// Base model for buttons.
    /// </summary>
    public class ButtonModelBase : LayoutModelBase
    {
        /// <summary>
        /// Gets or sets the Corner Radius.
        /// </summary>
        public int CornerRadius { get; set; } = 10;

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        public Color BackgroundColor { get; set; } = Colors.Transparent;

        /// <summary>
        /// Gets or sets the background color when hovered.
        /// </summary>
        public Color HoveredBackgroundColor { get; set; } = Color.FromArgb(25, 255, 255, 255);

        /// <summary>
        /// Gets or sets the background color when clicked.
        /// </summary>
        public Color ClickedBackgroundColor { get; set; } = Color.FromArgb(15, 255, 255, 255);
    }
}
