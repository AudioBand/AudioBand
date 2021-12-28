# AudioSource development
This page contains the `AudioBand.AudioSource` api documentation and documentation on developing an audio source for AudioBand.


## Building a new audio source project
1. Create a new class library project
2. [Install the audio source nuget package](https://www.nuget.org/packages/svr333.AudioBand.AudioSource/)
3. Create a class to implement `IAudioSource`. Api reference on the right.
```cs
public class AudioSource : IAudioSource
{
    // implementation here
}
```
4. The file `AudioSource.manifest` should be add to the project after installing the nuget package. Edit the file so that the name will matches your asembly file name.
```
AudioSource = "AudioSource.dll"
```

## Developing
In order to get it functional, you have to implement every function of `IAudioSource`.  
You can find out what they do by hovering over them in your IDE, it will show what the function is used for.  
(You can also reference [Dsafa's API refence](https://dsafa.github.io/audio-band/audiosource-api/AudioBand.AudioSource.html) because I don't know how to setup my own.)

You can also utilize the existing ones in the [Main AudioBand Repo](https://github.com/AudioBand/AudioBand/tree/master/src) as a template or guiding line.  
If you have any questions, feel free to contact me on [our Discord Server](https://discord.gg/yWDHdH2za5) or on GitHub through an issue.

## Deploying your new audio source.
**(Hopefully) coming soon:** An in-app way to download/install/distribute your custom audiosource via [This GitHub repo](https://github.com/AudioBand/CommunityAudiosources).

For now, AudioBand reads each sub folder under the `AudioSources` folder. To deploy your new audio source, place your files under a new subfolder in the `AudioSources` directory. **Ensure that your AudioSource.manifest file is also included. You also do not need to copy the AudioBand.AudioSource library files**

The file structure will look like this:
```
Audioband/
|--AudioSources/
   |--NewAudioSource/
      |--Audiosource.dll
      |--AudioSource.manifest
      |--other files
```
