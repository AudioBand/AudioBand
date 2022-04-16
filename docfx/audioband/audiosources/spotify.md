# Spotify

## Setup
1. Login to the [Spotify Dashboard](https://developer.spotify.com/dashboard/login) and create a new App. Fill in the details, you can name it whatever you want. This app will be just for AudioBand.
2. Go to the app you created and click `Edit Settings`. Add `http://localhost/callback` as a redirect url (see image below).
    1. If you use a different port than 80, you have to change the callback url to `http://localhost:PORT/callback`
3. Right click anywhere in the toolbar -> Audio Band Settings -> Audio Source Settings and fill in the fields `Spotify Client Id` and `Spotify Client Secret`. You can find them in the same dashboard page for the Spotify app you created.
4. Your browser should open asking you to login and allow your spotify app to access your currently playing songs.
    1. If your browser does not open automatically, Right click the toolbar and select Audio Sources > Spotify. It should now open in your default browser. 
5. Sign-in and accept and it should now display song information (make sure spotify is selected as the audio source).

>[!NOTE]
> If you have premium, all controls will work (except for volume when playing on mobile devices (Spotify limitation)).
> Without premium, only play/pause, next and previous song will work.

> Dashboard
![](~/images/spotify-dashboard.png)

> Callback settings
![](~/images/spotify-app-settings-callback.png)
