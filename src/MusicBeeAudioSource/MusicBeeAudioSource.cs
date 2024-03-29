﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using AudioBand.AudioSource;
using Timer = System.Timers.Timer;

namespace MusicBeeAudioSource
{
    /// <summary>
    /// AudioSource for MusicBee.
    /// </summary>
    public class MusicBeeAudioSource : IAudioSource
    {
        private MusicBeeIPC _ipc;
        private Timer _checkMusicBeeTimer;
        private bool _isPlaying;
        private string _currentId;
        private int _volume;
        private bool _shuffle;
        private MusicBeeIPC.RepeatMode _repeatMode;
        private bool _showratinglove;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicBeeAudioSource"/> class.
        /// </summary>
        public MusicBeeAudioSource()
        {
            _ipc = new MusicBeeIPC();
            _checkMusicBeeTimer = new Timer(100)
            {
                AutoReset = false,
                Enabled = false
            };

            _checkMusicBeeTimer.Elapsed += CheckMusicBee;
        }

        /// <inheritdoc/>
        public event EventHandler<TrackInfoChangedEventArgs> TrackInfoChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> IsPlayingChanged;

        /// <inheritdoc/>
        public event EventHandler<TimeSpan> TrackProgressChanged;

#pragma warning disable 00067 // The event is never used
        /// <inheritdoc/>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;
#pragma warning restore 00067 // The event is never used

        /// <inheritdoc/>
        public event EventHandler<int> VolumeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> ShuffleChanged;

        /// <inheritdoc/>
        public event EventHandler<RepeatMode> RepeatModeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> LikeChanged;

        /// <inheritdoc/>
        public string Name => "Music Bee";

        /// <inheritdoc/>
        public string Description => "";

        /// <inheritdoc/>
        public string WindowClassName => "WindowsForms10.Window.8.app.0.2bf8098_r7_ad1";

        /// <inheritdoc/>
        public IAudioSourceLogger Logger { get; set; }

        /// <inheritdoc/>
        public Task ActivateAsync()
        {
            _checkMusicBeeTimer.Start();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeactivateAsync()
        {
            _checkMusicBeeTimer.Stop();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task NextTrackAsync()
        {
            _ipc.NextTrack();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PauseTrackAsync()
        {
            _ipc.Pause();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PlayTrackAsync()
        {
            _ipc.Play();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PreviousTrackAsync()
        {
            _ipc.PreviousTrack();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetVolumeAsync(int newVolume)
        {
            _ipc.SetVolume(newVolume);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetPlaybackProgressAsync(TimeSpan newProgress)
        {
            _ipc.SetPosition((int)newProgress.TotalMilliseconds);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetShuffleAsync(bool shuffleOn)
        {
            _ipc.SetShuffle(shuffleOn);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetRepeatModeAsync(RepeatMode newRepeatMode)
        {
            _ipc.SetRepeat(ToIpcRepeat(newRepeatMode));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetLikeTrackAsync()
        {
            return Task.CompletedTask;
        }

        private RepeatMode ToRepeatMode(MusicBeeIPC.RepeatMode mode)
        {
            switch (mode)
            {
                case MusicBeeIPC.RepeatMode.All:
                    return RepeatMode.RepeatContext;
                case MusicBeeIPC.RepeatMode.None:
                    return RepeatMode.Off;
                case MusicBeeIPC.RepeatMode.One:
                    return RepeatMode.RepeatTrack;
                default:
                    Logger.Warn($"No case for {mode}");
                    return RepeatMode.Off;
            }
        }

        private MusicBeeIPC.RepeatMode ToIpcRepeat(RepeatMode mode)
        {
            switch (mode)
            {
                case RepeatMode.Off:
                    return MusicBeeIPC.RepeatMode.None;
                case RepeatMode.RepeatContext:
                    return MusicBeeIPC.RepeatMode.All;
                case RepeatMode.RepeatTrack:
                    return MusicBeeIPC.RepeatMode.One;
                default:
                    Logger.Warn($"No case for {mode}");
                    return MusicBeeIPC.RepeatMode.None;
            }
        }

        private void CheckMusicBee(object sender, ElapsedEventArgs eventArgs)
        {
            try
            {
                if (Process.GetProcessesByName("MusicBee").Length == 0)
                {
                    return;
                }

                // The ipc plugin does not load right away
                if (string.IsNullOrEmpty(_ipc.GetPluginVersionStr()))
                {
                    return;
                }

                NotifyState();
                NotifyTrackChange();
                NotifyVolume();
                NotifyShuffle();
                NotifyRepeatMode();
                NotifyLikeModeChanged();

                var time = TimeSpan.FromMilliseconds(_ipc.GetPosition());
                TrackProgressChanged?.Invoke(this, time);
            }
            catch (Exception e)
            {
                Logger.Debug(e);
            }
            finally
            {
                _checkMusicBeeTimer.Enabled = true;
            }
        }

        private void NotifyState()
        {
            var isPlaying = _ipc.GetPlayState() == MusicBeeIPC.PlayState.Playing;

            if (isPlaying == _isPlaying)
            {
                return;
            }

            _isPlaying = isPlaying;
            IsPlayingChanged?.Invoke(this, _isPlaying);
        }

        private void NotifyTrackChange()
        {
            var track = _ipc.GetFileTag(MusicBeeIPC.MetaData.TrackTitle);
            var artist = _ipc.GetFileTag(MusicBeeIPC.MetaData.Artist);

            var id = artist + track;
            if (_currentId == id)
            {
                return;
            }

            _currentId = id;
            Image albumArt = null;

            try
            {
                var bytes = Convert.FromBase64String(_ipc.GetArtwork());
                using (var ms = new MemoryStream(bytes))
                {
                    albumArt = new Bitmap(ms);
                }
            }
            catch (ArgumentException e)
            {
                Logger.Warn($"The AlbumArt cover failed to load. Might be non existing or wrong. - {e.Message}");
            }

            TrackInfoChanged?.Invoke(this, new TrackInfoChangedEventArgs
            {
                Album = _ipc.GetFileTag(MusicBeeIPC.MetaData.Album),
                Artist = artist,
                AlbumArt = albumArt,
                TrackLength = ParseTimeFromIPC(),
                TrackName = track
            });
        }

        private void NotifyVolume()
        {
            var volume = _ipc.GetVolume();

            if (volume == _volume)
            {
                return;
            }

            _volume = volume;
            VolumeChanged?.Invoke(this, _volume);
        }

        private void NotifyShuffle()
        {
            bool shuffle = _ipc.GetShuffle();

            if (shuffle == _shuffle)
            {
                return;
            }

            _shuffle = shuffle;
            ShuffleChanged?.Invoke(this, _shuffle);
        }

        private void NotifyRepeatMode()
        {
            MusicBeeIPC.RepeatMode repeat = _ipc.GetRepeat();

            if (repeat == _repeatMode)
            {
                return;
            }

            _repeatMode = repeat;
            RepeatModeChanged?.Invoke(this, ToRepeatMode(_repeatMode));
        }

        private void NotifyLikeModeChanged()
        {
            bool like = _ipc.GetShowRatingLove();

            if (like == _showratinglove)
            {
                return;
            }

            _showratinglove = like;
            LikeChanged?.Invoke(this, like);
        }

        private TimeSpan ParseTimeFromIPC()
        {
            var timeString = _ipc.GetFileProperty(MusicBeeIPC.FileProperty.Duration);
            var times = timeString.Split(':');

            switch (times.Length)
            {
                case 1:
                    double.TryParse(times[0], out double seconds);
                    return TimeSpan.FromSeconds(seconds);
                case 2:
                    double.TryParse(times[0], out double minutes);
                    double.TryParse(times[1], out seconds);
                    return TimeSpan.FromSeconds((minutes * 60) + seconds);
                case 3:
                    double.TryParse(times[0], out double hours);
                    double.TryParse(times[1], out minutes);
                    double.TryParse(times[2], out seconds);
                    return TimeSpan.FromSeconds((((hours * 60) + minutes) * 60) + seconds);
                default:
                    Logger.Warn($"Unable to parse track length: {timeString}");
                    return TimeSpan.Zero;
            }
        }
    }
}
