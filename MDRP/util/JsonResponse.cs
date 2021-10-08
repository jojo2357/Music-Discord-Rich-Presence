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
				if (jObject["album"] != null && jObject["album"].ToString() != string.Empty)
					Album = new Album(jObject["album"].ToString(), jObject["artist"].ToString());
				else
					Album = new Album("Unknown Album");
				Title = GetJasonField(jObject, "title");
				TimeStamp = GetJasonField(jObject, "timestamp");
				Action = GetJasonField(jObject, "action");
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
			public Album Album { get; }
			public string Title { get; }
			public string TimeStamp { get; }
			public string Action { get; }
			public string Player { get; }

			public bool isValid()
			{
				/*Console.WriteLine("Validity check: " + (Action != "play") + " " + (Action != "pause") + " " +
				                  (ValidPlayers.Contains(Player)) + " " + (EnabledClients[Player]));
				Console.WriteLine(this);*/
				return (Action == "play" || Action == "pause" || Action == "shutdown") &&
				       ValidPlayers.Contains(Player) && EnabledClients.ContainsKey(Player) &&
				       EnabledClients[Player];
			}

			public string getReasonInvalid()
			{
				switch (Action)
				{
					case "":
						return "provide an action field";
					case "play":
					case "pause":
					case "shutdown":
						if (!ValidPlayers.Contains(Player))
							return "invalid player name. expected one of \"" + string.Join("\", \"", ValidPlayers) +
							       "\" got " + Player + " instead";
						else if (!EnabledClients[Player])
							return "user has disabled this player";
						else
							return "valid";
					default:
						return "invalid action. expected one of \"play\" or \"pause\" but got \"" + Action + "\" instead";
				}
				/*return Action == string.Empty
					? "provide an action field"
					: Action != "play" && Action != "pause" && Action != "shutdown"
						? "invalid action. expected one of \"play\" or \"pause\" but got \"" + Action + "\" instead"
						: !ValidPlayers.Contains(Player)
							? "invalid player name. expected one of \"" + string.Join("\", \"", ValidPlayers) +
							  "\" got " + Player + " instead"
							: !EnabledClients[Player]
								? "user has disabled this player"
								: "valid";*/
			}

			public override string ToString()
			{
				return Action + " " + Title + " by " + Artist + " on " + Album.Name + " ending " + TimeStamp +
				       " from " + Player;
			}
		}
	}
}