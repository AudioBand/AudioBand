namespace AudioBand.Models
{
    /// <summary>
    /// Represents a configurable MouseBinding.
    /// </summary>
    public class MouseBinding
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use ctrl with this bind.
        /// </summary>
        public bool WithCtrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use shift with this bind.
        /// </summary>
        public bool WithShift { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use shift with this bind.
        /// </summary>
        public bool WithAlt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use xxx with this bind.
        /// </summary>
        public bool WithPower { get; set; }

        /// <summary>
        /// Gets or sets the type of mouse behaviour associated with this mouse binding.
        /// </summary>
        public MouseInputType MouseInputType { get; set; }

        /// <summary>
        /// Gets or sets the type of command to execute.
        /// </summary>
        public MouseBindingCommandType CommandType { get; set; }
    }
}
