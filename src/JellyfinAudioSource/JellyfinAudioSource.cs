using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AudioBand.AudioSource;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Timer = System.Timers.Timer;

namespace JellyfinAudioSource
{
    /// <summary>
    /// AudioBand AudioSource plugin for Jellyfin.
    /// </summary>
    public class JellyfinAudioSource : IAudioSource
    {
        private Timer _checkJellyfinTimer;
        private string _serverUrl = "http://localhost:8096";
        private string _apiKey = "";
        private string _username = "";

        private HttpClient _httpClient;
        private JellyfinApiClient _jellyfinClient;

        private CancellationTokenSource _cts;
        private Task _pollTask;

        private string _lastItemId;
        private bool _lastIsPlaying;
        private TimeSpan _lastProgress;
        private string _activeSessionId;

        public JellyfinAudioSource()
        {
            _checkJellyfinTimer = new Timer(100)
            {
                Enabled = false,
                AutoReset = false
            };

            _checkJellyfinTimer.Elapsed += CheckJellyfin;
        }

        /// <inheritdoc/>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <inheritdoc/>
        public event EventHandler<TrackInfoChangedEventArgs> TrackInfoChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> IsPlayingChanged;

        /// <inheritdoc/>
        public event EventHandler<TimeSpan> TrackProgressChanged;

#pragma warning disable 00067 // Event is not used
        /// <inheritdoc/>
        public event EventHandler<bool> ShuffleChanged;

        /// <inheritdoc/>
        public event EventHandler<RepeatMode> RepeatModeChanged;

        /// <inheritdoc/>
        public event EventHandler<int> VolumeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> LikeChanged;
#pragma warning restore 00067 // Event is not used

        /// <inheritdoc />
        public string Name => "Jellyfin";

        /// <inheritdoc />
        public string Description => "";

        /// <inheritdoc />
        public string WindowClassName => "Jellyfin";

        /// <inheritdoc />
        public IAudioSourceLogger Logger { get; set; }

        [AudioSourceSetting("Server URL")]
        public string ServerUrl
        {
            get => _serverUrl;
            set { _serverUrl = value.TrimEnd('/'); RaiseSetting(nameof(ServerUrl)); }
        }

        [AudioSourceSetting("API Key")]
        public string ApiKey
        {
            get => _apiKey;
            set { _apiKey = value; RaiseSetting(nameof(ApiKey)); }
        }

        /// <summary>
        /// Optional username filter. Leave blank to use the first active music session found,
        /// or enter your Jellyfin username to restrict to your own sessions on a shared server.
        /// </summary>
        [AudioSourceSetting("Username")]
        public string Username
        {
            get => _username;
            set { _username = value; RaiseSetting(nameof(Username)); }
        }

        public Task ActivateAsync()
        {
            BuildSdkClient();

            _checkJellyfinTimer.Start();

            return Task.CompletedTask;
        }

        public Task DeactivateAsync()
        {
            _checkJellyfinTimer.Stop();

            _jellyfinClient?.Dispose();
            _httpClient?.Dispose();
            _jellyfinClient = null;
            _httpClient = null;

            ResetState();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PlayTrackAsync()
        {
            return SendCommandAsync("Unpause");
        }

        /// <inheritdoc />
        public Task PauseTrackAsync()
        {
            return SendCommandAsync("Pause");
        }

        /// <inheritdoc />
        public Task PreviousTrackAsync()
        {
            return SendCommandAsync("PreviousTrack");
        }

        /// <inheritdoc />
        public Task NextTrackAsync()
        {
            return SendCommandAsync("NextTrack");
        }

        /// <inheritdoc />
        public Task SetVolumeAsync(float newVolume)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetPlaybackProgressAsync(TimeSpan newProgress)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetShuffleAsync(bool shuffleOn)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetRepeatModeAsync(RepeatMode repeatMode)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetVolumeAsync(int newVolume)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetLikeTrackAsync()
        {
            return Task.CompletedTask;
        }

        private async void CheckJellyfin(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Exceptions in async void can crash the whole app
            try
            {
                await PollOnceAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task PollOnceAsync()
        {
            if (_jellyfinClient == null || string.IsNullOrWhiteSpace(_apiKey))
                return;

            var sessions = await _jellyfinClient.Sessions.GetAsync().ConfigureAwait(false);

            if (sessions == null || sessions.Count == 0)
            {
                HandleNoActiveSession();
                return;
            }

            // Find the first session with an active Audio NowPlayingItem
            SessionInfoDto activeSession = null;
            foreach (var session in sessions)
            {
                if (session.NowPlayingItem == null)
                    continue;

                if (session.NowPlayingItem.MediaType != BaseItemDto_MediaType.Audio)
                    continue;

                // Check if username matches
                if (!string.IsNullOrWhiteSpace(_username) &&
                    !string.Equals(session.UserName, _username, StringComparison.OrdinalIgnoreCase))
                    continue;

                activeSession = session;
                break;
            }

            if (activeSession == null)
            {
                HandleNoActiveSession();
                return;
            }

            // Store the session ID so playback commands can target the right client
            _activeSessionId = activeSession.Id?.ToString();

            var item = activeSession.NowPlayingItem;
            bool isPlaying = activeSession.PlayState?.IsPaused == false;
            long posTicks = activeSession.PlayState?.PositionTicks ?? 0L;
            var progress = TimeSpan.FromTicks(posTicks);

            // Check if new track
            var itemId = item.Id?.ToString();
            if (itemId != _lastItemId)
            {
                _lastItemId = itemId;

                var trackName = item.Name ?? "Unknown Track";
                var album = item.Album ?? string.Empty;
                var length = TimeSpan.FromTicks(item.RunTimeTicks ?? 0L);

                // Prefer AlbumArtist; fall back to the first entry in Artists
                var artist = item.AlbumArtist ?? string.Empty;
                if (string.IsNullOrEmpty(artist) && item.Artists?.Count > 0)
                    artist = item.Artists[0] ?? string.Empty;

                Image art = null;
                try { art = await FetchAlbumArtAsync(itemId).ConfigureAwait(false); }
                catch { /* album art is non-critical */ }

                TrackInfoChanged?.Invoke(this, new TrackInfoChangedEventArgs
                {
                    TrackName = trackName,
                    Artist = artist,
                    Album = album,
                    TrackLength = length,
                    AlbumArt = art,
                });
            }

            if (isPlaying != _lastIsPlaying)
            {
                _lastIsPlaying = isPlaying;
                IsPlayingChanged?.Invoke(this, isPlaying);
            }

            if (Math.Abs((progress - _lastProgress).TotalSeconds) >= 1.0)
            {
                _lastProgress = progress;
                TrackProgressChanged?.Invoke(this, progress);
            }
        }

        private void BuildSdkClient()
        {
            _jellyfinClient?.Dispose();
            _httpClient?.Dispose();

            var sdkSettings = new JellyfinSdkSettings();
            sdkSettings.Initialize(
                clientName: "AudioBand",
                clientVersion: "1.0.0",
                deviceName: "AudioBand",
                deviceId: "audioband-jellyfin-audiosource");

            sdkSettings.SetServerUrl(_serverUrl);
            sdkSettings.SetAccessToken(_apiKey);

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Kiota request adapter bridges the SDK client to the HttpClient
            var authProvider = new JellyfinAuthenticationProvider(sdkSettings);
            var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: _httpClient);
            requestAdapter.BaseUrl = _serverUrl;

            _jellyfinClient = new JellyfinApiClient(requestAdapter);
        }

        /// <summary>
        /// Fetches album art for <paramref name="itemId"/> from the Jellyfin image API.
        /// Uses the SDK's <see cref="JellyfinApiClient.BuildUri"/> helper so we never
        /// hand-craft URL strings.
        /// </summary>
        private async Task<Image> FetchAlbumArtAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _jellyfinClient == null)
                return null;

            var requestInfo = _jellyfinClient.Items[Guid.Parse(itemId)].Images["Primary"].ToGetRequestInformation();

            // Append size/quality parameters
            requestInfo.QueryParameters["fillWidth"] = "200";
            requestInfo.QueryParameters["fillHeight"] = "200";
            requestInfo.QueryParameters["quality"] = "90";

            var uri = _jellyfinClient.BuildUri(requestInfo);

            var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return Image.FromStream(new MemoryStream(bytes));
        }

        /// <summary>
        /// Sends a remote playstate command to the active Jellyfin session.
        /// </summary>
        private async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(_activeSessionId) || _jellyfinClient == null)
            {
                return;
            }

            try
            {
                await _jellyfinClient.Sessions[_activeSessionId].Playing[command].PostAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Debug($"[JellyfinAudioSource] Command '{command}' failed: {ex.Message}");
            }
        }

        private void HandleNoActiveSession()
        {
            if (_lastItemId == null)
            {
                return;
            }

            ResetState();

            IsPlayingChanged?.Invoke(this, false);
            TrackProgressChanged?.Invoke(this, TimeSpan.Zero);
        }

        private void ResetState()
        {
            _lastItemId = null;
            _activeSessionId = null;
            _lastIsPlaying = false;
            _lastProgress = TimeSpan.Zero;
        }

        private void RaiseSetting(string name)
            => SettingChanged?.Invoke(this, new SettingChangedEventArgs(name));
    }
}
