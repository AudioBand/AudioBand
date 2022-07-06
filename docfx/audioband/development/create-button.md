# Development Guide: Creating a Button
This guide will show you how to create a button for AudioBand, while this may not always be what you want to do, it's a good all-round example to see what AudioBand's structure looks like.

## Topic overview:
### Creating the button
1. Updating the AudioSource functionality
2. Creating the Button Model
3. Adding the Button Model to the UserProfile (also update MigrationMappings)
4. Adding the button visually inside of the Toolbar
5. Creating and registering the Button's ViewModel

### Allowing customization of the button
1. Creating the SettingsView inside of UI/PlaybackControls
2. Adding the button to the SettingsWindow.xaml

> [!WARNING]
> When the settings will be revamped, doing the settings will change heavily.

## Creating the button
### 1. Updating the AudioSource functionality
Every AudioSource, whether it's shipped with AudioBand or installed from a third party, adheres to the IAudioSource interface.  
This interface contains method signatures on how any AudioSource should implement anything from the Play Button to controlling the Volume.

When you want to add a new button, you also have to create a new signature on how the feature of that button should behave.  
For a volume button, you need to set a signature like so: `Task SetVolumeAsync(int newVolume);`, so each AudioSource knows we handle volume as an integer from 0-100, and not a float from 0-1.  
It is good practise to update all existing AudioSources when you create such a new signature. If an AudioSource doesn't have a feature, just return empty.

### 2. Creating the Button Model
The first step to creating a Button is to create a class inside of `src/AudioBand/Models` that inherits from the `ButtonModelBase` class. Take a look at `PlayPauseButton` / `NextButton` to see what exactly is needed inside of that class.  
You want to set `IsVisible` to false on these buttons, as it would otherwise mess up every existing profile there is (believe me I've made that mistake with an early release VolumeButton build).  
For each state of the button you want to add a ButtonContent property. This is by far the easiest way to handle it, as a lot will be handled for you automatically.

In case you want to add extra properties, you can take a look at the `VolumeButton` Model, it added a whole customizable popup.

### 3. Adding the Button Model to the UserProfile (also update MigrationMappings)
This part is very small, but quite important.  
The `UserProfile` class is the class that gets stored inside of the `profile.json` files. This is what will later allow it to be customizable. We need to add the new Button Model in there so people can customize it.  

People who update from a really old AudioBand build, can have an older version of settings (using TOML instead of JSON). To make sure they can still convert their old profiles into new ones, we also have to update the migrations that are in place.  
This is pretty easy to do, in `src/AudioBand/Settings/MappingProfiles/SettingsV3ToSettingsV4Profile`, copy `VolumeButton`'s line, but replace it with your Button Model.

### 4. Creating and registering the Button's ViewModel
This part requires a bit of copying, and then a bit of logic to actually implement the button. Depending on what kind of button is, this can get complex.  
The ViewModels are saved inside of the `src/AudioBand/UI/PlaybackControls` folder and need to inherit from the `ButtonViewModelBase<MODELBUTTON>` class. MODELBUTTON being your model made in step 2.  

Once you've made a ViewModel for your Button, copy the parameters from eg. `VolumeButtonViewModel` and create a private field for the IAppSettings and IAudioSession just like that class.  

You also need to copy over the `OnEndEdit` and the `AppSettingsOnProfileChanged` methods.  
Next up is the ButtonContentViewModels, for each `ButtonContent` you added inside of your Button Model earlier, you need to create a corresponding `ButtonContentViewModel`. You also need to initalize this/these just like in one of the other ViewModels. 

Last part of the creation part is to add a Command. I would like to explain what a command is but it's late at the time of writing, so I'm going to skip over the detailed explanation. But in short, a command is used to link a button click in the UI with logic in the backend.  
Take a look at the Command in the `NextButtonViewModel` and implement a similar Command in your ViewModel.  
The implementation of that command heavily depends on what your button is supposed to do, but usually it's a simple call to _audioSession.CurrentAudioSource.NewMethodFromStepOne();

---

Now for registering, you have to add it in `src/AudioBand/Deskband.cs` inside of the container.  
And then also add it to the `(I)ViewModelContainer` found in `src/AudioBand/UI/ViewModel/`