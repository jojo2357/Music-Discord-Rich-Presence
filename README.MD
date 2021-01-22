# KNOWN ERRORS
The setup files dont have very good error handling yet. If you have any questions, open an issue. I'm working on improving it, but if you follow [the instructions](https://github.com/jojo2357/Music-Discord-Presence#instructions) it should all work.

# Discord Rich Presence For Groove (windows only)
To run, simply run `RunHidden.bat` (this just makes it minimized in the taskbar)
To end, kill the terminal thru clicking the x or `KillHidden.bat`

## Features
* For 7 select albums, displays the cover art in the DRP for any songs being played by Groovy (built in windows music player).
* Shows song title, artist, and album on any song.
* Includes batch scripts to make rich presence launch when groove is opened

## About linking Groove and DRP
In order to have rich presence every time you open groovy, run the shortcut `Link Rich Presence With Groove`. If at any time you wish to disable this feature, simply run `Unlink Rich Presence With Groove`.

## Instructions
Uh, download and unzip (of course), then run `RunHidden.bat` and ggwp.
Don't run the exe standalone, for future settings wont work with this
Don't move the exe or the bat file. Instead move the provided shortcut to the desktop, or any other location

If you want to link Groove and the DRP: 
close Groove
run `Locate Groove`
run `Link Rich Presence With Groove`
restart groove (it should have been opened by `Locate groove`)
if the DRP doesnt show up within 5 seconds, run `Unlink Rich Presence With Groove` and try steps 1-4 again.
If it still doesn't work, open up an issue and let me know whats going on.

## Future fatures
* Make settings to allow showing music from a plethora of apps.

### [Credits](https://github.com/dsdude123/GroovyRP)

I really like the ingenious solution [@dsdude123](https://github.com/dsdude123) came up with, but it was so very expensive. It took about 20% of my CPU to run idly. With my tweaks and improvements, it takes less than 2% and half (15 MB less) as much memory. I'm not knocking their way, as it is too big brain for me, I just took what they did and built on it to make something better.