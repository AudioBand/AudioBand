using System;
using System.Globalization;
using System.Windows.Data;

namespace AudioBand.UI
{
    /// <summary>
    /// Halfs an integer.
    /// </summary>
    [ValueConversion(typeof(int), typeof(int))]
    public class HalfIntConverter : IValueConverter
    {
        /// <summary>
        /// Returns half of the provided integer.
        /// </summary>
        /// <param name="value">The value of the int.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Ignorable.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The integer divided by two.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value) / 2;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value) * 2;
        }
    }
}
