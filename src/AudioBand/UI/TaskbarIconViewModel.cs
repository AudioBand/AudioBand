using System.Windows.Input;
using AudioBand.Commands;
using AudioBand.Messages;

namespace AudioBand.UI
{
    /// <summary>
    /// The Service to handle the TrayIcon.
    /// </summary>
    public class TaskbarIconViewModel
    {
        private IMessageBus _messages;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskbarIconViewModel"/> class.
        /// </summary>
        /// <param name="messages">The message bus.</param>
        public TaskbarIconViewModel(IMessageBus messages)
        {
            _messages = messages;
            OpenSettingsMenuCommand = new RelayCommand(OnOpenSettingsMenuExecute);
        }

        /// <summary>
        /// Gets the On Settings Menu Command.
        /// </summary>
        public ICommand OpenSettingsMenuCommand { get; private set; }

        private void OnOpenSettingsMenuExecute()
        {
            _messages.Publish(SettingsWindowMessage.OpenWindow);
        }
    }
}
