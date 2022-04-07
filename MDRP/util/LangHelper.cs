using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MDRP
{
	public class LangHelper
	{
		public Dictionary<LocalizableStrings, string> langMapping;
		public string language;

		private static readonly Dictionary<LocalizableStrings, string> defaultMapping = new Dictionary<LocalizableStrings, string>();

		static LangHelper()
		{
			defaultMapping = new Dictionary<LocalizableStrings, string>()
			{
				{ LocalizableStrings.ARTIST, "Artist" },
				{ LocalizableStrings.ALBUM, "Album" },
				{ LocalizableStrings.TITLE, "Title" },
				{ LocalizableStrings.PLAYER, "Player" },
				{ LocalizableStrings.DETAILS, "Music details" },
				{ LocalizableStrings.MDRP_FULL, "Music Discord Rich Presence" },
				{ LocalizableStrings.MDRP_SHORT, "MDRP" },
				{ LocalizableStrings.DRP_FULL, "Discord Rich Presence" },
				{ LocalizableStrings.DRP_SHORT, "DRP" },
				{ LocalizableStrings.VERSION, "Version" },
				{ LocalizableStrings.GOOD_ONE, "This is a good one, check your DRP ;)" },
				{ LocalizableStrings.KEYED_WRONG, "Album keyed for the wrong artist :/" },
				{ LocalizableStrings.UNKEYED, "Album not keyed :(" },
				{ LocalizableStrings.FOUND_REMOTELY, "Image found remotely!" },
				{ LocalizableStrings.NOT_FOUND_REMOTELY, "Image could not be found remotely" },
				{ LocalizableStrings.UNKNOWN_ARTIST, "Unknown Artist" },
				{ LocalizableStrings.CONSOLE_NAME, "Discord Rich Presence for Groove" },
				{ LocalizableStrings.NO_VALID_MEDIA, "Detected volume in something but not showing as it is not currently supported or is disabled" },
				{ LocalizableStrings.REQUIRE_PIPELINE, "Detected volume in {0} but no data has been recieved from it. You may need to update the player, install a plugin, or just pause and resume the music. See more at"},
				{ LocalizableStrings.AND, "and"},
				{ LocalizableStrings.FAILED_TO_GET_INFO, "Failed to get track info"},
				{ LocalizableStrings.UNKNOWN_ALBUM, "Unknown Album"},
				{ LocalizableStrings.ERROR_OCCURRED, "An error has occured. If the issue is severe, or you would like to know more, run the debug tool and use the printouts there. If this is severely hindering your MDRP experience, please open an issue on GitHub."},
				{ LocalizableStrings.REQUEST_DEBUG_TOOL, "An error has occured. Please run the debug tool to get a better readout and then open an issue on github with that printout."},
				{ LocalizableStrings.SPECIAL_INTEGRATION, "using special integration!"},
				{ LocalizableStrings.NOTHING_PLAYING, "Nothing Playing (probably paused)"},
				{ LocalizableStrings.NEW_UPDATE, "NEW UPDATE {0} (goto github to download)"},
				{ LocalizableStrings.NO_SETTINGS, "DiscordPresenceConfig.ini not found! this is the settings file to enable or disable certain features"},
				{ LocalizableStrings.NOTIF_KEYED_WRONG_HEADER, "Album keyed wrong"},
				{ LocalizableStrings.NOTIF_KEYED_WRONG_BODY, "is keyed for a different artist (check caps). To disable these notifications, set verbose to false in DiscordPresenceConfig.ini"},
				{ LocalizableStrings.NOTIF_UNKEYED_HEADER, "Album not keyed"},
				{ LocalizableStrings.NOTIF_UNKEYED_BODY, "is not keyed. To disable these notifications, set verbose to false in DiscordPresenceConfig.ini"},
				{ LocalizableStrings.USING, "Using"},
				{ LocalizableStrings.PAUSED, "paused"},
				{ LocalizableStrings.KEY_TOO_LONG, "The key for this album is too long. It must be {0} characters or less"},
				{ LocalizableStrings.NOTIF_KEY_TOO_LONG_HEADER, "Album key in discord too long"},
				{ LocalizableStrings.NOTIF_KEY_TOO_LONG_BODY, "The key for {0} ({1}) is longer than the {2} character maximum"},
				{ LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER, "\"MDRP settings issue\""},
				{ LocalizableStrings.NOTIF_SETERR_NO_ID_BODY, "\"Error in file {0} no id found on the second line\""},
				{ LocalizableStrings.NOTIF_SETERR_DEPREC_HEADER, "Deprecation Notice"},
				{ LocalizableStrings.NOTIF_SETERR_DEPREC_BODY, "{0} uses a deprecated keying format. Albums sould go in form Name==key==Artist"},
				{ LocalizableStrings.NOTIF_UPDATE_HEADER, "Update Available"},
				{ LocalizableStrings.NOTIF_UPDATE_BODY, "{0} is published on github. Go there for the latest version"},
				{ LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_BODY, "{0} could not be found on the ITunes API, please key this album manually."},
				{ LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_HEADER, "Album Art Not Found Remotely"},
			};
		}

		public LangHelper()
		{
			string[] files = Directory.GetFiles("../../../").Where(file => file.EndsWith(".lang")).ToArray();
			langMapping = defaultMapping;
			if (files.Length > 0)
			{
				loadMapping(files[0]);
			}
			else
			{
				langMapping = defaultMapping;
			}
		}

		private void loadMapping(string fyle)
		{
			language = Path.GetFileNameWithoutExtension(fyle);
			string[] lines = File.ReadAllLines(fyle);
			langMapping = new Dictionary<LocalizableStrings, string>();
			foreach (string line in lines)
			{
				string nameSearch = Regex.Split(line, @"==")[0];
				if (Enum.GetNames(typeof(LocalizableStrings)).Contains(nameSearch))
				{
					langMapping.Add((LocalizableStrings)Enum.Parse(typeof(LocalizableStrings), nameSearch), Regex.Split(line, @"==")[1]);
				}
			}
		}
		
		public string this[LocalizableStrings strin] => get(strin);

		public string get(LocalizableStrings strin)
		{
			if (langMapping.ContainsKey(strin))
				return langMapping[strin];
			return "|" +  Enum.GetName(typeof(LocalizableStrings), strin) + " UNKEYED|";
		}
	}

	public enum LocalizableStrings
	{
		MDRP_FULL,
		MDRP_SHORT,
		DRP_FULL,
		DRP_SHORT,
		ARTIST,
		ALBUM,
		TITLE,
		PLAYER,
		VERSION,
		DETAILS,
		GOOD_ONE,
		KEYED_WRONG,
		UNKEYED,
		FOUND_REMOTELY,
		NOT_FOUND_REMOTELY,
		UNKNOWN_ARTIST,
		CONSOLE_NAME,
		NO_VALID_MEDIA,
		REQUIRE_PIPELINE,
		AND,
		FAILED_TO_GET_INFO,
		UNKNOWN_ALBUM,
		ERROR_OCCURRED,
		REQUEST_DEBUG_TOOL,
		SPECIAL_INTEGRATION,
		NOTHING_PLAYING,
		NEW_UPDATE,
		NO_SETTINGS,
		NOTIF_KEYED_WRONG_HEADER,
		NOTIF_KEYED_WRONG_BODY,
		NOTIF_UNKEYED_HEADER,
		NOTIF_UNKEYED_BODY,
		NOTIF_KEY_TOO_LONG_HEADER,
		NOTIF_KEY_TOO_LONG_BODY,
		NOTIF_SETERR_NO_ID_HEADER,
		NOTIF_SETERR_NO_ID_BODY,
		NOTIF_SETERR_DEPREC_HEADER,
		NOTIF_SETERR_DEPREC_BODY,
		NOTIF_UPDATE_HEADER,
		NOTIF_UPDATE_BODY,
		NOTIF_NOT_FOUND_REMOTELY_HEADER,
		NOTIF_NOT_FOUND_REMOTELY_BODY,
		USING,
		PAUSED,
		KEY_TOO_LONG
	}
}