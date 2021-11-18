# Windows 10
No specific setup required for Windows 10

The Windows 10 AudioSource uses the [SMTC API](https://docs.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols) to control media on your desktop.
Practically, anytime you can use your media keys to control any form of media, this AudioSource will work.

## Limitations
Sadly, the `SMTC API` is still being developed and improved and is not a fine-tuned product yet, it also depends on the media sources to correctly implement these.

[Right Here](https://github.com/ModernFlyouts-Community/ModernFlyouts/blob/main/docs/GSMTC-Support-And-Popular-Apps.md) you can find a list of tested out AudioSources which will/will not work with this AudioSource.

Before filing a Bug Report saying that Windows10 AudioSource doesn't work, please check on [The List]((https://github.com/ModernFlyouts-Community/ModernFlyouts/blob/main/docs/GSMTC-Support-And-Popular-Apps.md)) first if it has full support.

>[!NOTE]
>This will only work with apps who also have media hardware keys enabled. (eg. Using On-Keyboard key to pause/play music or `fn + f-key` on laptop.)
