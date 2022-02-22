using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AudioBand.Commands;
using AudioBand.Messages;
using Hardcodet.Wpf.TaskbarNotification;

namespace AudioBand.UI
{
    /// <summary>
    /// The Service to handle the TrayIcon.
    /// </summary>
    public class TrayIconService
    {
        private IMessageBus _messages;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIconService"/> class.
        /// </summary>
        /// <param name="messages">The message bus.</param>
        public TrayIconService(IMessageBus messages)
        {
            _messages = messages;

            OpenSettingsMenuCommand = new RelayCommand(OnOpenSettingsMenuExecute);

            TaskbarIconToolTip = new ToolTip() { Content = "AudioBand" };

            TaskbarIcon = new TaskbarIcon()
            {
                TrayToolTip = TaskbarIconToolTip,
                IconSource = BitmapFrame.Create(new Uri("pack://application:,,,/AudioBand;component/audioband.ico")),
            };

            SetupContextMenu();
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

        /// <summary>
        /// Gets the On Settings Menu Command.
        /// </summary>
        public ICommand OpenSettingsMenuCommand { get; private set; }

        private void SetupContextMenu()
        {
            var settingsMenu = new MenuItem()
            {
                Header = "Settings",
                ToolTip = "Open Settings Menu",
                Icon = new Windows.UI.Xaml.Controls.FontIcon() { Glyph = "\uE115" },
                Command = OpenSettingsMenuCommand,
            };

            TaskbarIconContextMenu = new ContextMenu() { Items = { settingsMenu } };
        }

        private void OnOpenSettingsMenuExecute()
        {
            _messages.Publish(SettingsWindowMessage.OpenWindow);
        }
    }
}
