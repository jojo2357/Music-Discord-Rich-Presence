# Discord Rich Presence For Groove (windows only)
To run, simply run `Launch DRP` from the shortcuts folder or, if you want it to run in the background, run `Launch DRP Hidden`
Works with Groove, Spotify, and Chrome

## Features
* Allows uploading album art to custom applications to display and exceed the 150 asset maximum (see [uploading custom album art](https://github.com/jojo2357/Music-Discord-Presence#custom-album-art) for instructions) 
* Shows song title, artist, and album on any song.
* Includes batch scripts to enable rich presence to launch when groove is opened
* Ability to toggle media players showing up in rich presence.
* Can run in the background or with a pretty terminal

## About linking Groove and DRP
In order to have rich presence every time you open groovy, run the shortcut `Link Rich Presence With Groove`. If at any time you wish to disable this feature, simply run `Unlink Rich Presence With Groove`. See [Linking Groove](https://github.com/jojo2357/Music-Discord-Presence#linking-groove) for more 

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
* In the `clientdata` folder you will find 6 text files by default. `chrome.dat` `groove.dat` and `spotify.dat` are required only to provide a default if there is no provided album art.
* Since the provided files correlate to my own applications, you may create your own instead in order to upload art.
* [Create a discord application](https://discord.com/developers/applications) and set the name to be what you want displayed under your avatar (as in `playing <name>`)
* Grab the client ID. We will use this later.
* Navigate to the rich presence tab and select art assets.
* Add an image (note that the MINIMUM size is 512x512 so you may need to open the image in mspaint to resize)
* Set the name to be the album name with no spaces, all lower case, and remove any special characters the regex rejects (i dont know what will pass and what wont so if it doesnt work later, lmk what special characters you had and I'll add them to my filter)
* Once you save, it may take up to 10 minutes for discord to show your pictures. Don't worry though, if you did everything right it will show.
* Create a file in the `clientdata` directory and follow the model in `demo.dat`
* The first line is in the format:
```
<executeable playing music name>=<player name>
id=<client ID from earlier>
album1
album2
Album Name $3=albumname3
```
* For non latin albums, latinize it however you want and put a line in the format `<Album In Japanese/Emoji/Cyrillic/Chinese/non latin alphabet>=albumkey`
* For now, the executeable playing music must be a [supported player](https://github.com/jojo2357/Music-Discord-Presence#supported-players) and the part after the = is ignored. In the future I do think it will have a use tho.
* Nonlatin example owned and maintained by Smaltin

### Supported Players
- Groove Music (music.ui)
- Spotify
- Chrome
(More can be added, I just dunno if it's needed)

## Future fatures
* Maybe an image resizer but that sounds hard so only if asked for.

### [Credits](https://github.com/dsdude123/GroovyRP)
I really like the ingenious solution [@dsdude123](https://github.com/dsdude123) came up with, but it was so very expensive. It took about 20% of my CPU to run idly. With my tweaks and improvements, it takes less than 2% and half (15 MB less) as much memory. I'm not knocking their way, as it is too big brain for me, I just took what they did and built on it to make something better.

The new icon is thanks to [Ghoelian](https://github.com/Ghoelian) so thanks for that!

### Licence
Since I have added more features that allow the user to do more and more, I must add that while licenced under an MIT licence, I am not responsible for any damages caused by the use or abuse of the tools that I have provided. Use for good, not evil.