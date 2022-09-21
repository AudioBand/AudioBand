using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AudioBand.AudioSource;
using iTunesLib;
using Timer = System.Timers.Timer;

namespace iTunesAudioSource
{
    /// <summary>
    /// AudioSource for iTunes.
    /// </summary>
    public class iTunesAudioSource : IAudioSource
    {
        private Timer _checkiTunesTimer;
        private string _currentTrack;
        private bool _isPlaying;
        private int _volume;
        private bool _shuffle;
        private ITPlaylistRepeatMode _repeat;
        private bool _liked;
        private ITunesControls _itunesControls = new ITunesControls();

        /// <summary>
        /// Initializes a new instance of the <see cref="iTunesAudioSource"/> class.
        /// </summary>
        public iTunesAudioSource()
        {
            _checkiTunesTimer = new Timer(100)
            {
                Enabled = false,
                AutoReset = false
            };

            _checkiTunesTimer.Elapsed += CheckItunes;
        }

        /// <inheritdoc/>
        public event EventHandler<TrackInfoChangedEventArgs> TrackInfoChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> IsPlayingChanged;

        /// <inheritdoc/>
        public event EventHandler<TimeSpan> TrackProgressChanged;

#pragma warning disable 00067 // Event is not used
        /// <inheritdoc/>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;
#pragma warning restore 00067 // Event is not used

        /// <inheritdoc/>
        public event EventHandler<int> VolumeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> ShuffleChanged;

        /// <inheritdoc/>
        public event EventHandler<RepeatMode> RepeatModeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> LikeChanged;

        /// <inheritdoc/>
        public string Name => "iTunes";

        /// <inheritdoc/>
        public string Description => "";

        /// <inheritdoc/>
        public string WindowClassName => "iTunes";

        /// <inheritdoc/>
        public IAudioSourceLogger Logger { get; set; }

        /// <inheritdoc/>
        public Task ActivateAsync()
        {
            try
            {
                /* If AudioSource is activated and iTunes isn't open yet,
                 * it will open this manually and this can take a while.
                 * That is why we fire a separate thread for it.
                 */
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    _itunesControls.Start();
                }).Start();

                _checkiTunesTimer.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Error("Tried to activate the iTunes AudioSource but iTunes is not installed!");
                DeactivateAsync();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeactivateAsync()
        {
            try
            {
                _checkiTunesTimer.Stop();
                _itunesControls.Stop();
            }
            catch (Exception)
            {
                // iTunes not installed, ignore
            }

            _isPlaying = false;
            _currentTrack = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task NextTrackAsync()
        {
            _itunesControls.Next();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PauseTrackAsync()
        {
            _itunesControls.Pause();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PlayTrackAsync()
        {
            _itunesControls.Play();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PreviousTrackAsync()
        {
            _itunesControls.Previous();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetVolumeAsync(int newVolume)
        {
            _itunesControls.Volume = newVolume;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetPlaybackProgressAsync(TimeSpan newProgress)
        {
            _itunesControls.Progress = newProgress;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetShuffleAsync(bool shuffleOn)
        {
            _itunesControls.Shuffle = shuffleOn;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetRepeatModeAsync(RepeatMode newRepeatMode)
        {
            _itunesControls.RepeatMode = ToITRepeat(newRepeatMode);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetLikeTrackAsync()
        {
            // _itunesControls.Like();
            return Task.CompletedTask;
        }

        private RepeatMode ToRepeatMode(ITPlaylistRepeatMode mode)
        {
            switch (mode)
            {
                case ITPlaylistRepeatMode.ITPlaylistRepeatModeAll:
                    return RepeatMode.RepeatContext;
                case ITPlaylistRepeatMode.ITPlaylistRepeatModeOff:
                    return RepeatMode.Off;
                case ITPlaylistRepeatMode.ITPlaylistRepeatModeOne:
                    return RepeatMode.RepeatTrack;
                default:
                    Logger.Warn($"No case for {mode}");
                    return RepeatMode.Off;
            }
        }

        private ITPlaylistRepeatMode ToITRepeat(RepeatMode mode)
        {
            switch (mode)
            {
                case RepeatMode.Off:
                    return ITPlaylistRepeatMode.ITPlaylistRepeatModeOff;
                case RepeatMode.RepeatContext:
                    return ITPlaylistRepeatMode.ITPlaylistRepeatModeAll;
                case RepeatMode.RepeatTrack:
                    return ITPlaylistRepeatMode.ITPlaylistRepeatModeOne;
                default:
                    Logger.Warn($"No case for {mode}");
                    return ITPlaylistRepeatMode.ITPlaylistRepeatModeOff;
            }
        }

        private void NotifyTrackChange(Track track)
        {
            var trackInfo = new TrackInfoChangedEventArgs
            {
                Artist = track.Artist,
                Album = track.Album,
                AlbumArt = track.Artwork,
                TrackLength = track.Length,
                TrackName = track.Name,
            };

            TrackInfoChanged?.Invoke(this, trackInfo);
        }

        private bool IsNewTrack(Track track)
        {
            var trackname = track.Artist + track.Name;
            if (trackname == _currentTrack)
            {
                return false;
            }

            _currentTrack = trackname;
            return true;
        }

        private void NotifyPlayerState()
        {
            var playing = _itunesControls.IsPlaying;
            if (_isPlaying == playing)
            {
                return;
            }

            _isPlaying = playing;
            IsPlayingChanged?.Invoke(this, _isPlaying);
        }

        private void CheckItunes(object sender, ElapsedEventArgs eventArgs)
        {
            try
            {
                var track = _itunesControls.CurrentTrack;
                if (track == null)
                {
                    return;
                }

                NotifyPlayerState();
                NotifyVolume();
                NotifyShuffle();
                NotifyLike();
                if (IsNewTrack(track))
                {
                    NotifyTrackChange(track);
                }

                TrackProgressChanged?.Invoke(this, _itunesControls.Progress);
            }
            catch (Exception e)
            {
                Logger.Debug(e);
            }
            finally
            {
                _checkiTunesTimer.Enabled = true;
            }
        }

        private void NotifyLike()
        {
            var newLike = _itunesControls.GetLike();

            if (_liked == newLike)
            {
                return;
            }

            _liked = newLike;
            LikeChanged?.Invoke(this, newLike);
        }

        private void NotifyVolume()
        {
            int volume = _itunesControls.Volume;

            if (volume == _volume)
            {
                return;
            }

            _volume = volume;
            VolumeChanged?.Invoke(this, _volume);
        }

        private void NotifyShuffle()
        {
            bool shuffle = _itunesControls.Shuffle;

            if (shuffle == _shuffle)
            {
                return;
            }

            _shuffle = shuffle;
            ShuffleChanged?.Invoke(this, _shuffle);
        }

        private void NotifyRepeatMode()
        {
            ITPlaylistRepeatMode repeat = _itunesControls.RepeatMode;

            if (repeat == _repeat)
            {
                return;
            }

            _repeat = repeat;
            RepeatModeChanged?.Invoke(this, ToRepeatMode(_repeat));
        }
    }
}
