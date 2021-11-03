# Discord Rich Presence For Groove (windows only)
To run, use the included launcher. After you run it once, you can rerun it from a shortcut in the generated shortcuts folder.
Works with Groove, Spotify, and MusicBee

## Features
* Allows uploading album art to custom applications to display and exceed the ~~150~~ 300 (got changed recently) asset maximum (see [uploading custom album art](https://github.com/jojo2357/Music-Discord-Rich-Presence#custom-album-art) for instructions) 
* Shows song title, artist, and album on any song in a customizable way.
* Includes batch scripts to enable rich presence to launch when groove is opened
* Ability to toggle media players showing up in rich presence.
* Can run in the background or with a pretty terminal

## Custom presentation
In the `.ini` file, you can edit the two allotted lines. There are spotify-style and MDRP-style templates, and can be customized by adding text, or the title (`${title}`), artist (`${artist}`), and album (`${album}) as you see fit.

## Music Library Repo
[@Smaltin](https://github.com/Smaltin) and I are maintaining a [library repo](https://github.com/jojo2357/Music-Discord-Rich-Presence-Library) that allows you to find and download album art packs and contribute your own album arts for others to use. You can read more [here.](https://github.com/jojo2357/Music-Discord-Rich-Presence-Library)

## Data File Validator Tool
If you get stuck, feel free to use the dat [file validator](https://jojo2357.github.io/MDRP-Validation-Tool/) and if you need more help, open an issue there. DO NOT OPEN ALBUM ART RELATED ISSUES IN THIS REPO.

## About linking Groove and DRP
In order to have rich presence every time you open groovy, run the shortcut `Link Rich Presence With Groove`. If at any time you wish to disable this feature, simply run `Unlink Rich Presence With Groove`. See [Linking Groove](https://github.com/jojo2357/Music-Discord-Rich-Presence#linking-groove) for more 

## Instructions
Uh, download and unzip (of course), then run the `Launch DRP` shortcut from the shortcuts folder
Do **NOT** move the exe. Instead, move the provided shortcut(s)
To prevent a player from showing up in the rich presence, open `DiscordPresenceConfig.ini` and change true to false or false to true as you desire.

## Linking Groove 
close Groove
run `Locate Groove`
run `Link Rich Presence With Groove`
restart groove (it should have been opened by `Locate groove`)
if the DRP doesnt show up within 5 seconds, run `Unlink Rich Presence With Groove` and try steps 1-4 again.
If it still doesn't work, open up an issue and let me know whats going on.

## Custom Album Art
* Tutorial for 1.5 and groove and album art: https://www.youtube.com/watch?v=_qkxjf3T8Pw
* Tutorial for 1.6 and musicbee: https://youtu.be/GucN2WiteOM
* If you get stuck, feel free to use the dat [file validator](https://jojo2357.github.io/MDRP-Validation-Tool/) and if you need more help, open an issue there. DO NOT OPEN ALBUM ART RELATED ISSUES HERE.
* In the `clientdata` folder you will find 1 text files by default for demoing. Feel free to take a look.
* [Create a discord application](https://discord.com/developers/applications) and set the name to be what you want displayed under your avatar (as in `playing <name>`)
* Grab the client ID. We will use this later.
* Navigate to the rich presence tab and select art assets.
* Add an image (note that the MINIMUM size is 512x512 so you may need to resize it)
* Set the name to be the album name with no spaces, all lower case, and remove any special characters the regex rejects (i dont know what will pass and what wont so if it doesnt work later, lmk what special characters you had and I'll add them to my filter)
* Once you save, it may take up to 10 minutes for discord to show your pictures. Don't worry though, if you did everything right it will show.
* Create a file in the `clientdata` directory and follow the model in `demo.dat`
* The file is in the general format (REMOVE < and > AND FILL IN THE BLANK):
```
<executeable playing music name>=<player name>
id=<client ID from earlier>
Album Name 1==albumname1==Artist Name 1
Album Name 2==albumname2==Artist Name 2==Artist Name 3
Album Name 3==albumname3
```
* For non latin albums, latinize it however you want and put a line in the format `<Album In Japanese/Emoji/Cyrillic/Chinese/non latin alphabet>==albumkey`
* For now, the executeable playing music must be a [supported player](https://github.com/jojo2357/Music-Discord-Rich-Presence#supported-players) and the part after the = is ignored. In the future I do think it will have a use tho.

## Changing default background
If you really want to, you can go into the ini config and swap the application id's with your own app to get your own defaults. The three images that will be pulled from this default app are `<playername>` `<playername>_small` and `paused` (replace <playername> with groove, spotify, etc.)

### Supported Players
- Groove Music (music.ui)
- Spotify
- MusicBee
(More can be added, I just dunno if it's needed)

## Music Bee
- In order to be compatible with music bee, you have to add [my new plugin](https://github.com/jojo2357/MDRP-MusicBee-Bridge). The WMC plugin required before is no longer needed.

The new icon is thanks to [Ghoelian](https://github.com/Ghoelian) so thanks for that!

### Licence
Since I have added more features that allow the user to do more and more, I must add that while licenced under an MIT licence, I am not responsible for any damages caused by the use or abuse of the tools that I have provided. Use for good, not evil.
