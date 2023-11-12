using System.Linq;
using Newtonsoft.Json.Linq;

namespace MDRP
{
	internal partial class Program
	{
		public class JsonResponse
		{
			public JsonResponse(JObject jObject)
			{
				Artist = GetJasonField(jObject, "artist");
				AlbumArtist = GetJasonField(jObject, "album_artist");
				if (jObject["album"] != null && jObject["album"].ToString() != string.Empty)
					Album = new Album(jObject["album"].ToString(), jObject["artist"].ToString());
				else
					Album = new Album("Unknown Album");
				Title = GetJasonField(jObject, "title");
				TimeStamp = GetJasonField(jObject, "timestamp");
				ActionString = GetJasonField(jObject, "action");
				Action = ParseAction(ActionString);
				Player = GetJasonField(jObject, "player");
			}

			private static string GetJasonField(JObject jObject, string fieldName)
			{
				if (jObject[fieldName] != null && jObject[fieldName].ToString() != string.Empty)
					return jObject[fieldName].ToString();
				else
					return "";
			}

			public string Artist { get; }
			public string AlbumArtist { get; }
			public Album Album { get; }
			public string Title { get; }
			public string TimeStamp { get; }
			public RemoteAction Action { get; }
			private string ActionString;
			public string Player { get; }

			public bool isValid()
			{
				return (Action != RemoteAction.NumActions) &&
				       ValidPlayers.Contains(Player) && EnabledClients.ContainsKey(Player) &&
				       EnabledClients[Player];
			}

			public string getReasonInvalid()
			{
				if (ActionString == "")
				{
					return "provide an action field";
				}
				else if (Action == RemoteAction.NumActions)
				{
					return "invalid action. expected one of \"play\" or \"pause\" but got \"" + Action + "\" instead";
				}
				else
				{
					if (!ValidPlayers.Contains(Player))
						return "invalid player name. expected one of \"" + string.Join("\", \"", ValidPlayers) +
						       "\" got " + Player + " instead";
					else if (!EnabledClients[Player])
						return "user has disabled this player";
					else
						return "valid";
				}
			}

			public override string ToString()
			{
				return Action + " " + Title + " by " + Artist + " (or " + AlbumArtist + ") on " + Album.Name + " ending " + TimeStamp +
				       " from " + Player;
			}

			private RemoteAction ParseAction(string actionIn)
			{
				switch (actionIn.ToLower().Trim())
				{
					case "play":
						return RemoteAction.Play;
					case "pause":
						return RemoteAction.Pause;
					case "shutdown":
						return RemoteAction.Shutdown;
					default:
						return RemoteAction.NumActions;
				}
			}
		}

		public enum RemoteAction
		{
			Play,
			Pause,
			Shutdown,

			//You can use this to get the length of the enums, and also as a default, non null return
			NumActions
		}
	}
}