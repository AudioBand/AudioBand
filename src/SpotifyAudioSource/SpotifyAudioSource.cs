using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using AudioBand.AudioSource;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Http;
using static SpotifyAPI.Web.PlayerCurrentPlaybackRequest;
using static SpotifyAPI.Web.PlayerSetRepeatRequest;
using Image = System.Drawing.Image;
using Timer = System.Timers.Timer;

namespace SpotifyAudioSource
{
    /// <summary>
    /// Audio source for spotify.
    /// </summary>
    public class SpotifyAudioSource : IAudioSource
    {
        private readonly SpotifyControls _spotifyControls = new SpotifyControls();
        private Timer _checkSpotifyTimer = new Timer(1000);
        private int _checkForLikeCounter = 0;
        private Timer _volumeUpdateTimer = new Timer(750);
        private DateTime _lastVolumeUpdate = DateTime.UtcNow.AddMinutes(-1);

        private SpotifyClientConfig _spotifyConfig;
        private ISpotifyClient _spotifyClient;
        private HttpClient _httpClient = new HttpClient();
        private EmbedIOAuthServer _server = null;
        private bool _authIsInProcess = false;
        private string _currentItemId;
        private string _currentTrackName;
        private bool _currentIsPlaying;
        private int _currentProgress;
        private int _currentVolumePercent;
        private bool _currentShuffle;
        private string _currentRepeat;
        private bool _currentLikeState;
        private string _clientSecret;
        private string _clientId;
        private string _refreshToken;
        private int _pollingInterval = 1000;
        private bool _useProxy;
        private string _proxyHost;
        private int _localPort = 80;
        private int _proxyPort;
        private string _proxyUserName;
        private string _proxyPassword;
        private bool _isActive;
        private DateTime _lastAuthTime = DateTime.MinValue;
        private bool _forceRefreshAuth;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyAudioSource"/> class.
        /// </summary>
        public SpotifyAudioSource()
        {
            _checkSpotifyTimer.AutoReset = false;
            _checkSpotifyTimer.Elapsed += CheckSpotifyTimerOnElapsed;

            _volumeUpdateTimer.AutoReset = false;
            _volumeUpdateTimer.Elapsed += OnVolumeUpdaterElapsed;
        }

        /// <inheritdoc />
        public event EventHandler<TrackInfoChangedEventArgs> TrackInfoChanged;

        /// <inheritdoc />
        public event EventHandler<TimeSpan> TrackProgressChanged;

        /// <inheritdoc />
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <inheritdoc />
        public event EventHandler<int> VolumeChanged;

        /// <inheritdoc />
        public event EventHandler<bool> ShuffleChanged;

        /// <inheritdoc />
        public event EventHandler<RepeatMode> RepeatModeChanged;

        /// <inheritdoc />
        public event EventHandler<bool> IsPlayingChanged;

        /// <inheritdoc />
        public event EventHandler<bool> LikeChanged;

        /// <inheritdoc />
        public string Name => "Spotify";

        /// <inheritdoc />
        public string Description => "Please note that in order to use the controls, you require Spotify Premium.";

        /// <inheritdoc />
        public string WindowClassName => _spotifyControls.GetSpotifyWindowClassName();

        /// <inheritdoc />
        public IAudioSourceLogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the spotify client id.
        /// </summary>
        [AudioSourceSetting("Spotify Client ID")]
        public string ClientId
        {
            get => _clientId;
            set
            {
                if (value == _clientId)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(value))
                {
                    RefreshToken = "";
                }

                _clientId = value;
                Authorize();
            }
        }

        /// <summary>
        /// Gets or sets the spotify client secret.
        /// </summary>
        [AudioSourceSetting("Spotify Client secret")]
        public string ClientSecret
        {
            get => _clientSecret;
            set
            {
                if (value == _clientSecret)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(_clientSecret) && !string.IsNullOrEmpty(value))
                {
                    RefreshToken = "";
                }

                _clientSecret = value;
                Authorize();
            }
        }

        /// <summary>
        /// Gets or sets the Force Refresh Authentication Command.
        /// </summary>
        [AudioSourceSetting("Force Refresh Authentication")]
        public bool ForceRefreshAuth
        {
            get => _forceRefreshAuth;
            set
            {
                if (!value)
                {
                    return;
                }

                _forceRefreshAuth = false;

                RefreshToken = "";
                Authorize();
                OnSettingChanged("Force Refresh Authentication");
            }
        }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        [AudioSourceSetting("Spotify Refresh Token", Options = SettingOptions.ReadOnly | SettingOptions.Hidden, Priority = 9)]
        public string RefreshToken
        {
            get => _refreshToken;
            set
            {
                if (value == _refreshToken)
                {
                    return;
                }

                _refreshToken = value;
                OnSettingChanged("Spotify Refresh Token");
            }
        }

        /// <summary>
        /// Gets or sets the Polling Interval.
        /// </summary>
        [AudioSourceSetting("Spotify Polling Interval")]
        public int PollingInterval
        {
            get => _pollingInterval;
            set
            {
                if (value == _pollingInterval)
                {
                    return;
                }

                _pollingInterval = value;
                OnSettingChanged("Spotify Polling Interval");
                UpdatePollingInterval();
            }
        }

        /// <summary>
        /// Gets or sets the port for the local webserver used for authentication with spotify.
        /// </summary>
        [AudioSourceSetting("Callback Port", Priority = 10)]
        public int LocalPort
        {
            get => _localPort;
            set
            {
                if (value == _localPort)
                {
                    return;
                }

                _localPort = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use a proxy.
        /// </summary>
        [AudioSourceSetting("Use Proxy", Priority = 15)]
        public bool UseProxy
        {
            get => _useProxy;
            set
            {
                if (value == _useProxy)
                {
                    return;
                }

                _useProxy = value;
                UpdateProxy();
            }
        }

        /// <summary>
        /// Gets or sets the proxy host.
        /// </summary>
        [AudioSourceSetting("Proxy Host", Priority = 20)]
        public string ProxyHost
        {
            get => _proxyHost;
            set
            {
                if (value == _proxyHost)
                {
                    return;
                }

                _proxyHost = value;
                UpdateProxy();
            }
        }

        /// <summary>
        /// Gets or sets the proxy port.
        /// </summary>
        [AudioSourceSetting("Proxy Port", Priority = 20)]
        public int ProxyPort
        {
            get => _proxyPort;
            set
            {
                if (value == _proxyPort)
                {
                    return;
                }

                _proxyPort = value;
                UpdateProxy();
            }
        }

        /// <summary>
        /// Gets or sets the proxy username.
        /// </summary>
        [AudioSourceSetting("Proxy Username", Priority = 20)]
        public string ProxyUserName
        {
            get => _proxyUserName;
            set
            {
                if (value == _proxyUserName)
                {
                    return;
                }

                _proxyUserName = value;
                UpdateProxy();
            }
        }

        /// <summary>
        /// Gets or sets the proxy password.
        /// </summary>
        [AudioSourceSetting("Proxy Password", Options = SettingOptions.Sensitive, Priority = 20)]
        public string ProxyPassword
        {
            get => _proxyPassword;
            set
            {
                if (value == _proxyPassword)
                {
                    return;
                }

                _proxyPassword = value;
                UpdateProxy();
            }
        }

        /// <inheritdoc />
        public async Task ActivateAsync()
        {
            _isActive = true;
            _authIsInProcess = false;

            Authorize();
            await UpdatePlayer();

            _checkSpotifyTimer.Start();
        }

        /// <inheritdoc />
        public Task DeactivateAsync()
        {
            _isActive = false;
            _authIsInProcess = false;

            _checkSpotifyTimer.Stop();
            _currentItemId = null;
            _currentTrackName = null;
            Logger.Debug("Spotify has been deactivated.");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task PlayTrackAsync()
        {
            // Play/pause/next/back controls use the desktop, if that fails, use api
            // api does require spotify premium
            try
            {
                if (!await _spotifyClient.Player.ResumePlayback())
                {
                    _spotifyControls.TryPlay();
                }
            }
            catch (Exception)
            {
                // exception is only thrown when user was idle for too long
                _spotifyControls.TryPlay();
            }

            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc />
        public async Task PauseTrackAsync()
        {
            try
            {
                if (!await _spotifyClient.Player.PausePlayback())
                {
                    _spotifyControls.TryPause();
                }
            }
            catch (Exception)
            {
                _spotifyControls.TryPause();
            }

            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc />
        public async Task PreviousTrackAsync()
        {
            try
            {
                if (!await _spotifyClient.Player.SkipPrevious())
                {
                    _spotifyControls.TryPrevious();
                }
            }
            catch (Exception)
            {
                _spotifyControls.TryPrevious();
            }
        }

        /// <inheritdoc />
        public async Task NextTrackAsync()
        {
            try
            {
                if (!await _spotifyClient.Player.SkipNext())
                {
                    _spotifyControls.TryNext();
                }
            }
            catch (Exception)
            {
                _spotifyControls.TryNext();
            }
        }

        /// <inheritdoc />
        public async Task SetVolumeAsync(int newVolume)
        {
            if (_volumeUpdateTimer.Enabled)
            {
                _currentVolumePercent = newVolume;
                return;
            }

            _volumeUpdateTimer.Start();
            _lastVolumeUpdate = DateTime.UtcNow;

            _currentVolumePercent = newVolume;
            await _spotifyClient.Player.SetVolume(new PlayerVolumeRequest(newVolume));
            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc />
        public async Task SetPlaybackProgressAsync(TimeSpan newProgress)
        {
            if (_spotifyClient == null)
            {
                Authorize();
                return;
            }

            await OnPlayerCommandFailed(()
                => _spotifyClient.Player.SeekTo(new PlayerSeekToRequest((long)newProgress.TotalMilliseconds)), "SetPlaybackProgress");

            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc />
        public async Task SetShuffleAsync(bool shuffleOn)
        {
            if (_spotifyClient == null)
            {
                Authorize();
                return;
            }

            await OnPlayerCommandFailed(()
                => _spotifyClient.Player.SetShuffle(new PlayerShuffleRequest(shuffleOn)), "SetShuffle");

            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc />
        public async Task SetRepeatModeAsync(RepeatMode newRepeatMode)
        {
            if (_spotifyClient == null)
            {
                Authorize();
                return;
            }

            await OnPlayerCommandFailed(()
                => _spotifyClient.Player.SetRepeat(new PlayerSetRepeatRequest(ToRepeatState(newRepeatMode))), "SetRepeatMode");

            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        /// <inheritdoc/>
        public async Task SetLikeTrackAsync()
        {
            if (_spotifyClient == null)
            {
                Authorize();
                return;
            }

            if ((await _spotifyClient.Library.CheckTracks(new LibraryCheckTracksRequest(new List<string>() { _currentItemId }))).FirstOrDefault())
            {
                await OnPlayerCommandFailed(()
                    => _spotifyClient.Library.RemoveTracks(new LibraryRemoveTracksRequest(new List<string>() { _currentItemId })), "RemoveLike");
            }
            else
            {
                await OnPlayerCommandFailed(()
                   => _spotifyClient.Library.SaveTracks(new LibrarySaveTracksRequest(new List<string>() { _currentItemId })), "AddLike");
            }

            _checkForLikeCounter = 0;
            await Task.Delay(110).ContinueWith(async t => await UpdatePlayer());
        }

        private RepeatMode ToRepeatMode(State state)
        {
            switch (state)
            {
                case State.Off:
                    return RepeatMode.Off;
                case State.Context:
                    return RepeatMode.RepeatContext;
                case State.Track:
                    return RepeatMode.RepeatTrack;
                default:
                    Logger.Warn($"No case for {state}");
                    return RepeatMode.Off;
            }
        }

        private RepeatMode ToRepeatMode(string state)
        {
            switch (state)
            {
                case "off":
                    return RepeatMode.Off;
                case "context":
                    return RepeatMode.RepeatContext;
                case "track":
                    return RepeatMode.RepeatTrack;
                default:
                    Logger.Warn($"No case for {state}");
                    return RepeatMode.Off;
            }
        }

        private State ToRepeatState(RepeatMode mode)
        {
            switch (mode)
            {
                case RepeatMode.Off:
                    return State.Off;
                case RepeatMode.RepeatContext:
                    return State.Context;
                case RepeatMode.RepeatTrack:
                    return State.Track;
                default:
                    Logger.Warn($"No case for {mode}");
                    return State.Off;
            }
        }

        private void Authorize()
        {
            if (!_isActive)
            {
                return;
            }

            if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            {
                Logger.Error($"Cannot connect to Spotify because either ClientId or ClientSecret is empty.");
                return;
            }

            if (!string.IsNullOrEmpty(RefreshToken))
            {
                Logger.Debug("Using RefreshToken from previous auth to connect to Spotify.");
                RefreshAccessTokenOnClient().GetAwaiter().GetResult();
                return;
            }

            try
            {
                FirstTimeAuth().GetAwaiter().GetResult();
                _authIsInProcess = false;
            }
            catch (Exception e)
            {
                Logger.Error($"Error while trying to authenticate, user most likely offline ~ {e.Message}");
                _authIsInProcess = false;
                throw;
            }
            finally
            {
                _authIsInProcess = false;
            }
        }

        private void UpdateSpotifyHttpClient()
        {
            if (string.IsNullOrEmpty(ProxyHost) || ProxyPort == 0
            || string.IsNullOrEmpty(ProxyUserName) || string.IsNullOrEmpty(ProxyPassword))
            {
                return;
            }

            var httpClient = new NetHttpClient(new ProxyConfig(ProxyHost, ProxyPort)
            {
                User = ProxyUserName,
                Password = ProxyPassword
            });

            _spotifyConfig.WithHTTPClient(httpClient);
        }

        private void UpdateProxy()
        {
            Logger.Debug("Updating proxy configuration.");

            UpdateSpotifyHttpClient();
            if (_spotifyConfig is null)
                return;
            _spotifyClient = new SpotifyClient(_spotifyConfig);
        }

        private async Task<CurrentlyPlayingContext> GetPlaybackAsync()
        {
            try
            {
                if (_spotifyClient is null)
                {
                    if (!_authIsInProcess)
                    {
                        Authorize();
                    }

                    return null;
                }

                var playback = await _spotifyClient.Player.GetCurrentPlayback(new PlayerCurrentPlaybackRequest(AdditionalTypes.All));

                return playback;
            }
            catch (HttpRequestException)
            {
                // Gets thrown when internet cuts out, will retry every 30 seconds.
                Logger.Info("Tried to update Spotify Playback, but user has no internet connection. Update Interval is 30 seconds.");
                _checkSpotifyTimer.Interval = 30000;
            }
            catch (APIUnauthorizedException)
            {
                Authorize();
            }
            catch (APITooManyRequestsException e)
            {
                _checkSpotifyTimer.Interval = e.RetryAfter.TotalMilliseconds < 100 ? 100 : e.RetryAfter.TotalMilliseconds;
            }
            catch (TaskCanceledException e) { }
            catch (APIException e)
            {
                Logger.Error(e.Response.Body);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    Logger.Error(e.InnerException.Message);
                }

                Logger.Error(e.Message);
                Logger.Error(e);
            }

            return null;
        }

        private async Task NotifyTrackUpdate(FullTrack track)
        {
            // Local files have no id so we use name
            if (track.Id == _currentItemId && track.Name == _currentTrackName)
            {
                return;
            }

            _currentItemId = track.Id;
            _currentTrackName = track.Name;

            string albumArtUrl = track.Album?.Images.FirstOrDefault()?.Url;
            Image albumArtImage = albumArtUrl is null ? null : await GetAlbumArtAsync(new Uri(albumArtUrl));

            string artists = string.Join(", ", track.Artists?.Select(a => a?.Name));
            var trackLength = TimeSpan.FromMilliseconds(track.DurationMs);

            var trackUpdateInfo = new TrackInfoChangedEventArgs
            {
                Artist = artists,
                TrackName = track.Name,
                AlbumArt = albumArtImage,
                Album = track.Album?.Name,
                TrackLength = trackLength,
            };

            TrackInfoChanged?.Invoke(this, trackUpdateInfo);
        }

        private async Task NotifyEpisodeUpdate(FullEpisode episode)
        {
            _currentItemId = episode.Id;
            _currentTrackName = episode.Name;

            var imageUrl = episode.Images?.FirstOrDefault()?.Url;

            var trackUpdateInfo = new TrackInfoChangedEventArgs
            {
                Artist = episode.Show.Publisher,
                TrackName = episode.Name,
                AlbumArt = await GetPodcastAlbumArtAsync(imageUrl),
                Album = episode.Show.Name,
                TrackLength = TimeSpan.FromMilliseconds(episode.DurationMs),
            };

            TrackInfoChanged?.Invoke(this, trackUpdateInfo);
        }

        private void NotifyPlayState(bool isPlaying)
        {
            if (_currentIsPlaying == isPlaying)
            {
                return;
            }

            _currentIsPlaying = isPlaying;
            IsPlayingChanged?.Invoke(this, _currentIsPlaying);
        }

        private void NotifyTrackProgress(CurrentlyPlayingContext context)
        {
            _currentProgress = context.ProgressMs;
            TrackProgressChanged?.Invoke(this, TimeSpan.FromMilliseconds(_currentProgress));
        }

        private void NotifyVolume(CurrentlyPlayingContext context)
        {
            // if timer is enabled or last volume update was less than 2 seconds ago, temporarily do not update
            if (context.Device == null
            || _volumeUpdateTimer.Enabled
            || _lastVolumeUpdate.AddSeconds(2) > DateTime.UtcNow)
            {
                return;
            }

            var newVolume = context.Device.VolumePercent.HasValue ? context.Device.VolumePercent.Value : 0;

            if (_currentVolumePercent == newVolume)
            {
                return;
            }

            _currentVolumePercent = newVolume;
            VolumeChanged?.Invoke(this, _currentVolumePercent);
        }

        private void NotifyShuffle(CurrentlyPlayingContext context)
        {
            _currentShuffle = context.ShuffleState;
            ShuffleChanged?.Invoke(this, _currentShuffle);
        }

        private void NotifyRepeat(CurrentlyPlayingContext context)
        {
            _currentRepeat = context.RepeatState;
            RepeatModeChanged?.Invoke(this, ToRepeatMode(_currentRepeat));
        }

        private async Task NotifyLike()
        {
            if (string.IsNullOrEmpty(_currentItemId))
            {
                return;
            }

            // this is a heavy api call, limit it to once every 10 cycles
            if (_checkForLikeCounter < 10)
            {
                return;
            }

            _checkForLikeCounter = 0;
            var isLiked = (await _spotifyClient.Library.CheckTracks(new LibraryCheckTracksRequest(new List<string>() { _currentItemId }))).FirstOrDefault();

            if (isLiked == _currentLikeState)
            {
                return;
            }

            _currentLikeState = isLiked;
            LikeChanged?.Invoke(this, _currentLikeState);
        }

        private async Task<Image> GetAlbumArtAsync(Uri albumArtUrl)
        {
            if (albumArtUrl == null)
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync(albumArtUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn("Response was not successful when getting album art: " + response);
                    return null;
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return Image.FromStream(stream);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private async Task<Image> GetPodcastAlbumArtAsync(string imageUrl = null)
        {
            try
            {
                HttpResponseMessage response;

                if (string.IsNullOrEmpty(imageUrl))
                {
                    response = await _httpClient.GetAsync("https://i.imgur.com/FZG4OtK.png");
                }
                else
                {
                    response = await _httpClient.GetAsync(new Uri(imageUrl));
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return Image.FromStream(stream);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        private async void CheckSpotifyTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // Spotify api does not provide a way to get realtime player status updates, so we have to resort to polling.
            await UpdatePlayer();
        }

        private void OnVolumeUpdaterElapsed(object sender, ElapsedEventArgs e)
        {
            _volumeUpdateTimer.Stop();

            _spotifyClient.Player.SetVolume(new PlayerVolumeRequest(_currentVolumePercent));
        }

        private async Task UpdatePlayer()
        {
            _checkSpotifyTimer.Stop();

            if (!_isActive)
            {
                _checkSpotifyTimer.Interval = 2500;
                return;
            }

            try
            {
                var currentSpotifyWindowTitle = _spotifyControls.GetSpotifyWindowTitle();
                if (string.IsNullOrEmpty(currentSpotifyWindowTitle))
                {
                    // reduce number of calls when paused since we have to poll for track changes.
                    _checkSpotifyTimer.Interval = 3000;
                }
                else
                {
                    _checkSpotifyTimer.Interval = PollingInterval;
                }

                var playback = await GetPlaybackAsync();

                if (playback == null)
                {
                    NotifyPlayState(false);
                    return;
                }

                if (playback.Item == null || playback.CurrentlyPlayingType == "ad")
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    return;
                }

                NotifyPlayState(playback.IsPlaying);
                NotifyTrackProgress(playback);
                NotifyVolume(playback);
                NotifyShuffle(playback);
                NotifyRepeat(playback);

                if (playback.Item.Type == ItemType.Track)
                {
                    await NotifyTrackUpdate(playback.Item as FullTrack);
                }
                else
                {
                    await NotifyEpisodeUpdate(playback.Item as FullEpisode);
                }

                // put last in case someone hasn't updated auth yet
                await NotifyLike();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                // check again in case
                if (_isActive)
                {
                    try
                    {
                        _checkSpotifyTimer.Interval = _checkSpotifyTimer.Interval < 100
                                                ? 100 : _checkSpotifyTimer.Interval;
                        _checkSpotifyTimer.Start();
                    }
                    catch
                    {
                        // normally this should never fire but its here just in case
                        _checkSpotifyTimer = new Timer(PollingInterval);
                    }
                }
            }
        }

        private async Task RefreshAccessTokenOnClient()
        {
            AuthorizationCodeRefreshResponse response;

            try
            {
                var request = new AuthorizationCodeRefreshRequest(ClientId, ClientSecret, RefreshToken);
                response = await new OAuthClient().RequestToken(request);
            }
            catch (Exception e)
            {
                Logger.Error($"Error while refreshing access token, user most likely offline ~ {e.Message}");
                return;
            }

            _spotifyClient = new SpotifyClient(response.AccessToken);

            var expiresIn = TimeSpan.FromSeconds(response.ExpiresIn);
            Logger.Debug($"Received new access token. Expires in: {expiresIn} (At {DateTime.Now + expiresIn})");
        }

        private async Task FirstTimeAuth()
        {
            if (_lastAuthTime.AddSeconds(30) > DateTime.UtcNow)
            {
                return;
            }

            _lastAuthTime = DateTime.UtcNow;
            _authIsInProcess = true;
            _server = new EmbedIOAuthServer(new Uri($"http://localhost:{LocalPort}/callback"), LocalPort);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackState, Scopes.UserReadPlaybackPosition, Scopes.UserModifyPlaybackState, Scopes.UserLibraryRead, Scopes.UserLibraryModify },
            };

            BrowserUtil.Open(request.ToUri());
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            try
            {
                await _server.Stop();
                var config = SpotifyClientConfig.CreateDefault();
                var tokenResponse = await new OAuthClient(config)
                    .RequestToken(new AuthorizationCodeTokenRequest(
                        ClientId, ClientSecret, response.Code, new Uri($"http://localhost:{LocalPort}/callback")));

                if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
                {
                    _authIsInProcess = false;
                    return;
                }

                RefreshToken = tokenResponse.RefreshToken;
                _spotifyConfig = config.WithAuthenticator(new AuthorizationCodeAuthenticator(ClientId, ClientSecret, tokenResponse));
                _spotifyClient = new SpotifyClient(_spotifyConfig);
                _authIsInProcess = false;
            }
            catch (Exception e)
            {
                _authIsInProcess = false;
                Logger.Error("It appears that the Spotify Services are down. Please try again later.");
                Logger.Error(e);
            }
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            _authIsInProcess = false;
            Logger.Error($"Error while authenticating: {error}");
            await _server.Stop();
        }

        private void OnSettingChanged(string settingName)
        {
            SettingChanged?.Invoke(this, new SettingChangedEventArgs(settingName));
        }

        private async Task OnPlayerCommandFailed(Func<Task<bool>> command, [CallerMemberName] string caller = null)
        {
            var hasError = await command();
            if (hasError)
            {
                Logger.Warn($"Something went wrong with player command [{caller}].");
            }
        }

        private void UpdatePollingInterval()
        {
            _checkSpotifyTimer.Stop();
            _checkSpotifyTimer.Interval = PollingInterval;
            _checkSpotifyTimer.Start();
        }
    }
}
