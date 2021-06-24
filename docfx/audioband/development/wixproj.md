# Compiling into an .msi
I'm writing this down for anyone who wants to make an .msi on their own, and for future me.  
You don't have to do this step if you want to test/run your local build. You only need this if you want to distribute your version.

## Requirements
- [Wixtools](https://wixtoolset.org/releases/)
- [Visual Studio](https://visualstudio.microsoft.com/)
- [Wixtools Visual Studio Extension](https://wixtoolset.org/releases/)

Once Wixtools is installed it is recommended to [add their root folder to your path](https://helpdeskgeek.com/windows-10/add-windows-path-environment-variable/).

## Compiling the .wixproj
Once you have installed the Extension, you are now able to load in any .wixproj.  
If you did not add any AudioSources, it is pretty easy to compile. Set your environment to `Release`, and go to `Build` -> `Build Solution`. Once that is finished, you can `Build` the AudioBandInstaller project.

You will find your .msi in `src/AudioBandInstaller/bin/Release/`.

However, if you add a new AudioSource, you have to do some extra setup to make sure the .msi accounts for your new AudioSource.
The steps aren't too hard though, just make sure you don't miss any:

- Add YourAudioSourcePath as a constant to the `<DefineConstants>` tag inside of `.wixproj`
- Add a `<HeatDirectory>` tag inside of .wixproj for your AudioSource
- Add a project reference to your AudioSource
- Add a `<Feature>` tag inside of `Product.wxs` for your AudioSource

The XXXHeatGenerated.wxs files you see should be automatically generated, if not, also run while inside your AudioSource folder:
`heat project XXXAudioSource.csproj -ag -pog Binaries -template fragment -out ../AudioBandInstaller/XXXHeatGenerated.wxs`

From what I understand, [Heating Files](https://wixtoolset.org/documentation/manual/v3/overview/heat.html) is just a way to tell the project where to get all the correct files from.
