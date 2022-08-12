using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace AudioBand.UI
{
    /// <summary>
    /// Show time tooltip behavior for a slider.
    /// </summary>
    internal class ShowTimeBehavior : Behavior<Slider>
    {
        /// <summary>
        /// Dependency property for <see cref="PrefixProperty"/>.
        /// </summary>
        public static readonly DependencyProperty PrefixProperty = DependencyProperty.Register(
            "Prefix",
            typeof(string),
            typeof(ShowTimeBehavior),
            new PropertyMetadata(default(string)));

        private Track _track;

        /// <summary>
        /// Gets or sets the prefix
        /// </summary>
        public string Prefix
        {
            get
            {
                return (string)GetValue(PrefixProperty);
            }
            set
            {
                SetValue(PrefixProperty, value);
            }
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            base.OnAttached();
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            _track.MouseMove -= TrackOnMouseMove;
            _track = null;
            base.OnDetaching();
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            _track = (Track)AssociatedObject.Template.FindName("PART_Track", AssociatedObject);
            _track.MouseMove += TrackOnMouseMove;
        }

        private void TrackOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var position = mouseEventArgs.GetPosition(_track);
            var valueFromPoint = _track.ValueFromPoint(position);
            var floorOfValueFromPoint = (int)Math.Floor(valueFromPoint);

            var time = TimeSpan.FromMilliseconds(floorOfValueFromPoint);
            var formattedTime = time.ToString("mm\\:ss");

            if (time >= TimeSpan.FromHours(1))
            {
                formattedTime = time.ToString("hh\\:mm\\:ss");
            }

            var toolTip = $"{Prefix}{formattedTime}";
            ToolTipService.SetToolTip(_track, toolTip);
            ToolTipService.SetShowDuration(_track, 100000);
        }
    }
}
