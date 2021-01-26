# Discord Rich Presence For Groove (windows only)
To run, simply run `Launch DRP` from the shortcuts folder.
Works with Groove, Spotify, and Chrome

## Features
* For 7 select albums, displays the cover art in the DRP for any songs being played by Groovy (built in windows music player).
* Shows song title, artist, and album on any song.
* Includes batch scripts to enable rich presence to launch when groove is opened
* Ability to toggle media players showing up in rich presence.

## About linking Groove and DRP
In order to have rich presence every time you open groovy, run the shortcut `Link Rich Presence With Groove`. If at any time you wish to disable this feature, simply run `Unlink Rich Presence With Groove`. See [Linking Groove](https://github.com/jojo2357/Music-Discord-Presence#linking-groove) for more 

## Instructions
Uh, download and unzip (of course), then run the `Launch DRP` shortcut from the shortcuts folder
Do **NOT** move the exe. Instead, move the provided shortcut
To prevent a player from showing up in the rich presence, open `DiscordPresenceConfig.ini` and change true to false or false to true as you desire.

## Adding different players
In `DiscordPresenceConfig.ini`, add <the name of the player (ie spotify, or chrome)>=<true (or false> (dont actually add the <> just use ur brain and look at the example pls)

## Linking Groove 
close Groove
run `Locate Groove`
run `Link Rich Presence With Groove`
restart groove (it should have been opened by `Locate groove`)
if the DRP doesnt show up within 5 seconds, run `Unlink Rich Presence With Groove` and try steps 1-4 again.
If it still doesn't work, open up an issue and let me know whats going on.

## Future fatures
* 1.2.0 will have better versatility with more players, and custom presences that allow you to upload your own album arts!

### [Credits](https://github.com/dsdude123/GroovyRP)
I really like the ingenious solution [@dsdude123](https://github.com/dsdude123) came up with, but it was so very expensive. It took about 20% of my CPU to run idly. With my tweaks and improvements, it takes less than 2% and half (15 MB less) as much memory. I'm not knocking their way, as it is too big brain for me, I just took what they did and built on it to make something better.