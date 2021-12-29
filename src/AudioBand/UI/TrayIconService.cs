using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace AudioBand.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class TrayIconService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIconService"/> class.
        /// </summary>
        public TrayIconService()
        {
            TaskbarIconToolTip = new ToolTip() { Content = "AudioBand" };

            TaskbarIcon = new TaskbarIcon()
            {
                TrayToolTip = TaskbarIconToolTip,
            };
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TrayIconService"/> class.
        /// </summary>
        ~TrayIconService()
        {
            TaskbarIcon.Dispose();
        }

        /// <summary>
        /// Gets the current TaskbarIcon.
        /// </summary>
        public TaskbarIcon TaskbarIcon { get; private set; }

        /// <summary>
        /// Gets the current ContextMenu used in the Taskbar
        /// </summary>
        public ContextMenu TaskbarIconContextMenu { get; private set; }

        /// <summary>
        /// Gets the current ToolTip used in the Taskbar.
        /// </summary>
        public ToolTip TaskbarIconToolTip { get; private set; }
    }
}
