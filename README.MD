# Music Discord Rich Presence
TL;DR: Run this by navigating to the [latest release](https://github.com/jojo2357/Music-Discord-Rich-Presence/releases/latest) and downloading the release zip. unzip the contents and use the bat to run. The settings are in the `DiscordPresenceConfig.ini` file

### [**Video Tutorial**](https://youtu.be/mhgKXxdhYoA) 

This program has been designed to work with many music players in the windows environment, including:
- Groove Music (Win 10)
- Windows Media Player (Win 11)
- Spotify
- Music Bee ([Special Plugin](https://github.com/jojo2357/MDRP-MusicBee-Bridge) Required, but has bonus features)
- Apple Music
- Wavelink
- Tidal Player\*

There may be more to come, time will tell.

\* Note for Tidal, may not work for some users. Has been successfully tested on windows 10 and 11, on the free version on computers that MDRP works with either Groove or WMP. Paid versions of Tidal may experience issues, and older versions of Tidal will not work because they changed the exe name.

### If you have a version below 1.7, please refer to the [old readme](https://github.com/jojo2357/Music-Discord-Rich-Presence/blob/1.6.4/README.MD) or update your entire MDRP installation from scratch

## Features
- Can pull your album art from ITunes and display it just like Spotify!
- Allows you to provide your own album art to display instead.
- Shows the title, album, and artist in your Discord Rich Presence for any supported player, with configurable style
- Includes batch scripts to run MDRP when your music player is started, and will close MDRP when your player closes
- Can toggle on or off certain music players
- Can change the default name of the application in Discord, so you could have `Playing Music` instead of `Playing Groove Music`

## Setup
0. Download the [latest release](https://github.com/jojo2357/Music-Discord-Rich-Presence/releases/latest)
1. Unzip the zip file
2. Navigate to the unzipped contents
3. Run `Music_DRP_Launcher.bat`. Your antivirus may flag it as potentially dangerous, so optionally run a scan first, or live on the edge and select "Run Anyway" (you may need to google how to do so)
4. Select the option you would like, and play some tunes. It is recommended that you do *not* run hidden on win 11 or until you are confortable using MDRP
5. Profit

# Extended Setup
This is probably what you are here for :)

Once you have completed the initial setup, you may want to get adventurous, maybe even try out all that MDRP has to offer. Here is a step-by-step guide on how to do that.

## Linking Music Players and MDRP
If you are updating from an older version, run the associated unlinker first.

Q: What is linking?\
A: Linking is the process of telling Windows to run/close MDRP when your music player is opened/closed. **THIS HAS NO IMPACT ON *HOW* MDRP RUNS, ONLY *WHEN* IT GETS AUTOMATICALLY STARTED**

Q: Why doesn't it work for me?\
A: There is no easy way to get this feature, and it is known to be finicky. It works well for some and poorly for others, so if it doesn't work for you, please understand that it is not your fault.

To link a player to MDRP, close your music player, run the associated linker option from the main runner menu and follow instructions there.

## Custom Album Art
My now outdated tutorials can be found here:
- [1.5](https://www.youtube.com/watch?v=_qkxjf3T8Pw) Demos how to make a Discord app
- [1.6](https://youtu.be/GucN2WiteOM) Covers up-to-date Musicbee plugin setup

To get your album art to show up, there are one of two ways to do so. The old way is still supported, where you upload album arts to discord and then put those in a file, but there is a new, op way.

### The Lazy Way
You can set in the settings file `get remote artwork` and `create cache file`. This will key all your songs when you play them, but be warned, there is a chance these arts may be wrong.

### The Library Tool Way
Right now you can only use the old library, so don't use this for now.

### The Spotify Way
If you would like to key all of your Spotify playlists, go to the [MDRP website](https://mdrp.tech/fetchalbumart) (under construction, please be patient) and click `Regenerate Token`, then select either `Include Artists` (recommended) or `Exclude Artists` and then download the dat (may take up to 5 minutes if you have a ton of songs) and place it in your clientdata folder.

## Changing MDRP Settings
To change any MDRP settings, they will all be located in your `DiscordPresenceConfig.ini` file.\
Data in this file is in `key=value` pairs so any line that does not have a `=` will be ignored.

### Changing default background
In the ini config file, there is the option to change the default background on a per-player setting. The schema is `large <player> asset=url/application asset`. For example, to change the default beckground of MusicBee to the album cover of "My Head is an Animal", you would put: 

`large musicbee asset=https://is3-ssl.mzstatic.com/image/thumb/Music125/v4/11/50/a7/1150a7a6-ad88-1985-2869-7ae3d66391d5/12UMGIM10062.rgb.jpg/512x512bb.jpg` 

And to change it for groove, you would change `musicbee` to `music.ui` and for Windows Media Player, this would change to `windows.media.player`

To apply this to the small icon, you should change `large` to `small`

**NOTE:** if you would like a per-application paused icon, background icon, etc, you should change the default to be an asset instead of a link. This will look in that application for that asset instead, and by doing this, you can have different image assets by that name in each app to achieve this behavior.

### Get Desktop Notifications
The `verbose` setting enables desktop notifications about the following:

- Incorrectly keyed data file
- Unkeyed/Incorrectly keyed album art
- New update available
- Depreciated settings/key style

### Changing Rich Presence Format
If you would like, you can change how your Rich Presence is shown in Discord. You are limited to two lines (by discord, not MDRP) and these can be changed in the ini. The two examples will render the song King And Lionheard by Of Monsters And Men on the ablum My Head is An Animal as

Spotify-style:
line 1: `King And Lionheart by Of Monsters And Men`
line 2: `on My Head is An Animal`

MDRP-Style:
line 1: `Title: King And Lionheart`
line 2: `Artist: Of Monsters and Men`

The tooltip on the large image will always have the album name, and the tooltip on the small icon will have one of `Listening to <Music Player>` or `paused` (when appropriate).

### Automatic Album Art Settings
Automatic Album Art or Remote Artowork as it is sometimes referred to, is the process by which MDRP will look on the internet for the album art that corresponds to your currently playing media. **If MDRP gets the album art wrong, please comment on [this discussion](https://github.com/jojo2357/Music-Discord-Rich-Presence/discussions/59)

| Setting | When It applies | Behavior when true | Behavior when false | Recommendation |
|---------|-----------------|--------------------|---------------------|----------|
| `get remote artwork` | The current album is unkeyed for the current Media Player |MDRP will attempt to get the artowork for the currently playing media online and display it on the default application for the current Media Player | MDRP will simply show the default background on the default application | Set this to true if you haven't keyed you albums and would like to have arts in your rich Presence |
| `remote needs exact match` | `get remote artwork` is true, The current album is unkeyed, and MDRP could not find an identical match either because there were too many results or the artist name was slightly off | MDRP will search again with more restrictive terms, and choose an exact match, or the only match, or the best non perfect artist match | ditto ^ | Set to false if your arts are frequently incorrect, true if your arts are frequently not found |
| `create cache file` | `get remote artwork` is true, and MDRP had to search for an artwork | after a successful search, MDRP will key this album to a file in your clientdata folder. This key file will apply to all players, and will use the default app for those players | The cache will not be saved, and lost on restart | Not a lot of reasons to set this to false, MDRP is basically keying all your music for you, and if MDRP gets it wrong, you can correct it yourself |
| `translate from japanese` | `get remote artwork` is true | Searches to ITunes will be done with the language of the search set to Japanese | Searches assume english, and will only directly latinize searches | Set this to true if you have a lot of Japanese music, especially if the searches don't find it often. Searching in JP will provide subtle corrections like reordering first and last names |


## Languages
To change the language, move the default `english-us.lang` back to the `languages` folder and then move the `.lang` file you need from the languages folder to the same directory as the readme and launcher.
  
Feel free to contribute a new language, it is recommended to join [my discord](https://discord.gg/qVbY2ygeGy) so we can make sure we get the translations right.
  
### Supported Langs
- English
- Spanish
- German
- Dutch
  
Icon is thanks to [Ghoelian](https://github.com/Ghoelian) so thanks for that!

### Licence
Since I have added more features that allow the user to do more and more, I must add that while licensed under an MIT license, I am not responsible for any damages caused by the use or abuse of the tools that I have provided. Use for good, not evil.
