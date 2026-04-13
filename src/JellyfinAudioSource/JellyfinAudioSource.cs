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

namespace JellyfinAudioSource
{
    /// <summary>
    /// AudioBand AudioSource plugin for Jellyfin.
    /// </summary>
    public class JellyfinAudioSource : IAudioSource
    {
        private string _serverUrl = "http://localhost:8096";
        private string _apiKey = "";
        private string _username = "";
        private int _pollIntervalSeconds = 3;

        private HttpClient _httpClient;
        private JellyfinApiClient _jellyfinClient;

        private CancellationTokenSource _cts;
        private Task _pollTask;

        private string _lastItemId;
        private bool _lastIsPlaying;
        private TimeSpan _lastProgress;
        private string _activeSessionId;

        /// <inheritdoc/>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <inheritdoc/>
        public event EventHandler<TrackInfoChangedEventArgs> TrackInfoChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> IsPlayingChanged;

        /// <inheritdoc/>
        public event EventHandler<TimeSpan> TrackProgressChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> ShuffleChanged;

        /// <inheritdoc/>
        public event EventHandler<RepeatMode> RepeatModeChanged;

        /// <inheritdoc/>
        public event EventHandler<int> VolumeChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> LikeChanged;

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

        /// <summary>
        /// How often (in seconds) the plugin polls the /Sessions endpoint.
        /// </summary>
        [AudioSourceSetting("Jellyfin Polling Interval")]
        public int PollIntervalSeconds
        {
            get => _pollIntervalSeconds;
            set { _pollIntervalSeconds = Math.Max(1, value); RaiseSetting(nameof(PollIntervalSeconds)); }
        }

        public Task ActivateAsync()
        {
            BuildSdkClient();

            _cts = new CancellationTokenSource();
            _pollTask = PollLoopAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public async Task DeactivateAsync()
        {
            _cts?.Cancel();
            if (_pollTask != null)
            {
                try { await _pollTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            _jellyfinClient?.Dispose();
            _httpClient?.Dispose();
            _jellyfinClient = null;
            _httpClient = null;
            ResetState();
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

        private async Task PollLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await PollOnceAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Swallow — server may be temporarily unreachable
                    System.Diagnostics.Debug.WriteLine($"[JellyfinAudioSource] Poll error: {ex.Message}");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task PollOnceAsync(CancellationToken ct)
        {
            if (_jellyfinClient == null || string.IsNullOrWhiteSpace(_apiKey))
                return;

            // GET /Sessions — returns IList<SessionInfo> via the SDK
            var sessions = await _jellyfinClient.Sessions
                                                .GetAsync(cancellationToken: ct)
                                                .ConfigureAwait(false);

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
                try { art = await FetchAlbumArtAsync(itemId, ct).ConfigureAwait(false); }
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
        private async Task<Image> FetchAlbumArtAsync(string itemId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(itemId) || _jellyfinClient == null)
                return null;

            // Build a strongly-typed URI for GET /Items/{itemId}/Images/Primary
            var requestInfo = _jellyfinClient.Items[Guid.Parse(itemId)]
                                             .Images["Primary"]
                                             .ToGetRequestInformation();

            // Append size/quality parameters
            requestInfo.QueryParameters["fillWidth"] = "200";
            requestInfo.QueryParameters["fillHeight"] = "200";
            requestInfo.QueryParameters["quality"] = "90";

            var uri = _jellyfinClient.BuildUri(requestInfo);

            var response = await _httpClient.GetAsync(uri, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            // Wrap in a second MemoryStream so Image holds its own copy of the buffer
            return Image.FromStream(new MemoryStream(bytes));
        }

        /// <summary>
        /// Sends a remote playstate command to the active Jellyfin session.
        /// The SDK path is: <c>client.Sessions[sessionId].Playing[command].PostAsync()</c>
        /// </summary>
        private async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(_activeSessionId) || _jellyfinClient == null)
                return;

            try
            {
                await _jellyfinClient.Sessions[_activeSessionId]
                                     .Playing[command]
                                     .PostAsync(cancellationToken: CancellationToken.None)
                                     .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[JellyfinAudioSource] Command '{command}' failed: {ex.Message}");
            }
        }

        private void HandleNoActiveSession()
        {
            if (_lastItemId == null)
                return;

            ResetState();
            TrackInfoChanged?.Invoke(this, new TrackInfoChangedEventArgs
            {
                TrackName = string.Empty,
                Artist = string.Empty,
                Album = string.Empty,
                TrackLength = TimeSpan.Zero,
                AlbumArt = null,
            });
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
