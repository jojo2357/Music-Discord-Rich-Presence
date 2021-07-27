using System;
using System.Runtime.InteropServices;
using DiscordRPC;
using DiscordRPC.Message;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Web.Http;
using IWshRuntimeLibrary;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace GroovyRP
{
	class Program
	{
		private const string Version = "1.6.0";
		private const string Github = "https://github.com/jojo2357/Music-Discord-Rich-Presence";
		private const string Title = "Discord Rich Presence For Groove";

		private static readonly Uri uri =
			new Uri("https://api.github.com/repos/jojo2357/Music-Discord-Rich-Presence/releases/latest");

		//Player Name, client
		private static readonly Dictionary<string, DiscordRpcClient> DefaultClients =
			new Dictionary<string, DiscordRpcClient>
			{
				{"music.ui", new DiscordRpcClient("807774172574253056", autoEvents: false)},
				{"musicbee", new DiscordRpcClient("820837854385012766", autoEvents: false)},
				{"Apple Music", new DiscordRpcClient("820837854385012766", autoEvents: false)},
				{"spotify", new DiscordRpcClient("802222525110812725", autoEvents: false)},
				{"chrome", new DiscordRpcClient("802213652974272513", autoEvents: false)},
				{"", new DiscordRpcClient("821398156905283585", autoEvents: false)},
			};

		private static readonly List<Album> NotifiedAlbums = new List<Album>();

		//ID, client
		private static readonly Dictionary<string, DiscordRpcClient> AllClients =
			new Dictionary<string, DiscordRpcClient>();

		//Playername, client
		private static readonly Dictionary<string, DiscordRpcClient[]> PlayersClients =
			new Dictionary<string, DiscordRpcClient[]>();

		//Album, (id, key)
		private static readonly Dictionary<Album, Dictionary<string, string>> AlbumKeyMapping =
			new Dictionary<Album, Dictionary<string, string>>();

		//ID, process name
		//process name, enabled y/n
		private static readonly Dictionary<string, bool> EnabledClients = new Dictionary<string, bool>
		{
			{"music.ui", true},
		};

		private static readonly Dictionary<string, ConsoleColor> PlayerColors = new Dictionary<string, ConsoleColor>
		{
			{"music.ui", ConsoleColor.Blue},
			{"chrome", ConsoleColor.Yellow},
			{"Apple Music", ConsoleColor.DarkRed},
			{"spotify", ConsoleColor.DarkGreen},
			{"musicbee", ConsoleColor.Yellow}
		};

		private static string _presenceDetails = string.Empty;

		private static readonly string[] ValidPlayers = new[]
			{"apple music", "music.ui", "chrome", "spotify", /*"brave", */"new_chrome", "musicbee" /*, "firefox" */};

		private static readonly string[] RequiresPipeline = new[] {"musicbee"};

		//For use in settings
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
		{
			{"musicbee", "Music Bee"},
			{"chrome", "Something in Google Chrome"},
			{"spotify", "Spotify Music"},
			{"apple music", "Apple Music"},
			{"groove", "Groove Music Player"},
			{"new_chrome", "Something in Brave"},
			{"music.ui", "Groove Music Player"},
			{"brave", "Something in Brave"},
		};

		private static readonly Dictionary<string, string> BigAssets = new Dictionary<string, string>
		{
			{"musicbee", "musicbee"},
			{"music.ui", "groove"},
			{"chrome", "chrome"},
			{"new_chrome", "brave_small"},
			{"brave", "brave_small"},
			{"spotify", "spotify"},
			{"apple music", "applemusic"},
		};

		//might just combine these later
		private static readonly Dictionary<string, string> LittleAssets = new Dictionary<string, string>
		{
			{"musicbee", "musicbee_small"},
			{"music.ui", "groove_small"},
			{"chrome", "chrome_small"},
			{"new_chrome", "brave_small"},
			{"brave", "brave"},
			{"spotify", "spotify_small"},
			{"apple music", "applemusic_small"},
		};

		private static readonly Dictionary<string, string> Whatpeoplecallthisplayer = new Dictionary<string, string>
		{
			{"musicbee", "Music Bee"},
			{"music.ui", "Groove Music"},
			{"chrome", "Google Chrome"},
			{"new_chrome", "Brave"},
			{"brave", "Brave"},
			{"spotify", "Spotify"},
			{"apple music", "Apple Music"},
		};

		private static readonly Dictionary<string, string> InverseWhatpeoplecallthisplayer =
			new Dictionary<string, string>
			{
				{"musicbee", "musicbee"},
				{"groove", "music.ui"},
				{"chrome", "chrome"},
				{"brave", "new_chrome"},
				{"spotify", "spotify"},
				{"apple music", "apple music"},
			};

		private static readonly string defaultPlayer = "groove";
		private static readonly int timeout_seconds = 60;

		private static readonly Stopwatch Timer = new Stopwatch(),
			MetaTimer = new Stopwatch(),
			UpdateTimer = new Stopwatch();

		private static string playerName = string.Empty, lastPlayer = String.Empty;

		private static bool justcleared,
			justUnknowned,
			ScreamAtUser,
			presenceIsRich,
			WrongArtistFlag,
			UpdateAvailibleFlag,
			NotifiedRequiredPipeline;

		private static DiscordRpcClient activeClient;
		private static Album currentAlbum = new Album("");
		private static readonly HttpClient Client = new HttpClient();
		private static int updateCheckInterval = 36000000;
		private static string UpdateVersion;

		public static HttpListener listener;
		public static string url = "http://localhost:2357/";
		public static int pageViews = 0;
		public static int requestCount = 0;
		public static string pageData = "{response:\"Verified\"}";

		public static bool remoteControl = false;
		public static long resignRemoteControlAt = 0;

		private static Queue<JsonResponse> messages = new Queue<JsonResponse>();

		public static async Task HandleIncomingConnections()
		{
			// While a user hasn't visited the `shutdown` url, keep on handling requests
			while (true)
			{
				// Will wait here until we hear from a connection
				HttpListenerContext ctx = await listener.GetContextAsync();

				// Peel out the requests and response objects
				HttpListenerRequest req = ctx.Request;
				HttpListenerResponse resp = ctx.Response;

				// Print out some info about the request
#if DEBUG
				Console.WriteLine("Request #: {0}", ++requestCount);
				Console.WriteLine(req.Url.ToString());
				Console.WriteLine(req.HttpMethod);
				Console.WriteLine("Time: " +
				                  (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds);
#endif
				string text;
				using (var reader = new StreamReader(req.InputStream,
					req.ContentEncoding))
				{
					text = reader.ReadToEnd();
				}

				string decodedText = Uri.UnescapeDataString(text);

				string response;
				try
				{
					var jason = JObject.Parse(decodedText);
					Console.WriteLine("Tmes: " + jason["timestamp"]);
					JsonResponse parsedJason = new JsonResponse(jason);
					if (parsedJason.isValid())
					{
						Console.WriteLine("Enqueuing");
						messages.Enqueue(parsedJason);
						response = "{response:\"success\"}";
					}
					else
					{
						Console.WriteLine("Invalid JSON ");
						response = "{response:\"invalid json " + parsedJason.getReasonInvalid() + "\"}";
					}
				}
				catch (Exception e)
				{
					response = "{response:\"failure to parse json\"}";
					Console.WriteLine(response);
				}

				Console.WriteLine(decodedText);
				Console.WriteLine();

				byte[] data = Encoding.UTF8.GetBytes(response);
				resp.ContentType = "text/json";
				resp.ContentEncoding = Encoding.UTF8;
				resp.ContentLength64 = data.LongLength;

				// Write out to the response stream (asynchronously), then close it
				await resp.OutputStream.WriteAsync(data, 0, data.Length);
				resp.Close();
			}
		}

		private class JsonResponse
		{
			public string Artist { get; private set; }
			public Album Album { get; private set; }
			public string Title { get; private set; }
			public string TimeStamp { get; private set; }
			public string Action { get; private set; }
			public string Player { get; private set; }

			public JsonResponse(JObject jObject)
			{
				//if (jObject["artist"].ToString() != String.Empty)
				Artist = jObject["artist"].ToString();
				//if (jObject["album"].ToString() != String.Empty)
				Album = new Album(jObject["album"].ToString(), jObject["artist"].ToString());
				//if (jObject["title"].ToString() != String.Empty)
				Title = jObject["title"].ToString();
				//if (jObject["timestamp"].ToString() != String.Empty)
				TimeStamp = jObject["timestamp"].ToString();
				//if (jObject["action"].ToString() != String.Empty)
				Action = jObject["action"].ToString();

				Player = jObject["player"].ToString();
			}

			public bool isValid()
			{
				/*Console.WriteLine("Validity check: " + (Action != "play") + " " + (Action != "pause") + " " +
				                  (ValidPlayers.Contains(Player)) + " " + (EnabledClients[Player]));
				Console.WriteLine(this);*/
				return (Action == "play" || Action == "pause") && ValidPlayers.Contains(Player) &&
				       EnabledClients[Player];
			}

			public string getReasonInvalid()
			{
				return Action == String.Empty
					? "provide an action field"
					: Action != "play" && Action != "pause"
						? "invalid action. expected one of \"play\" or \"pause\" but got \"" + Action + "\" instead"
						: !ValidPlayers.Contains(Player)
							? "invalid player name. expected one of \"" + String.Join("\", \"", ValidPlayers) +
							  "\" got " + Player + " instead"
							: !EnabledClients[Player]
								? "user has disabled this player"
								: "valid";
			}

			public override string ToString()
			{
				return Action + " " + Title + " by " + Artist + " on " + Album.Name + " ending " + TimeStamp + " from " + Player;
			}
		}

		private static void doServer()
		{
			listener = new HttpListener();
			listener.Prefixes.Add(url);
			listener.Start();
			Console.WriteLine("Listening for connections on {0}", url);

			// Handle requests
			Task listenTask = HandleIncomingConnections();
			listenTask.GetAwaiter().GetResult();

			// Close the listener
			listener.Close();
		}


		private static void Main(string[] args)
		{
			ThreadStart ts = doServer;
			Thread t = new Thread(ts);
			t.Start();
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.Title = "Discord Rich Presence for Groove";

			Client.DefaultRequestHeaders["User-Agent"] = "c#";
			Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			GenerateShortcuts();

			if (args.Length > 0)
				return;

			LoadSettings();

			foreach (DiscordRpcClient client in DefaultClients.Values)
			{
				if (!AllClients.ContainsKey(client.ApplicationID))
					AllClients.Add(client.ApplicationID, client);
				else
					AllClients[client.ApplicationID] = client;
			}

			MetaTimer.Start();
			Timer.Start();
			UpdateTimer.Start();

			CheckForUpdate();

			foreach (DiscordRpcClient client in AllClients.Values)
			{
				client.Initialize();
				client.OnError += _client_OnError;
				client.OnPresenceUpdate += _client_OnPresenceUpdate;
			}

			GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null, lastTrack = null;

			try
			{
				currentTrack = GetStuff();
				lastTrack = currentTrack;
			}
			catch (Exception e)
			{
#if DEBUG
				Console.WriteLine(e);
#endif
			}

			bool isPlaying = IsUsingAudio(), wasPlaying;

			while (IsInitialized())
			{
				try
				{
					//limit performace impact
					if (UpdateTimer.ElapsedMilliseconds > updateCheckInterval)
						CheckForUpdate();
					Thread.Sleep(2000);
					if (messages.Count > 0)
					{
						JsonResponse lastMessage = messages.Last();
						messages.Clear();
						Console.WriteLine("Recieved on main thread {0}", lastMessage);
						wasPlaying = isPlaying;
						remoteControl = isPlaying = lastMessage.Action == "play";
						currentAlbum = lastMessage.Album;
						playerName = lastMessage.Player;
						GetClient();

						resignRemoteControlAt = long.Parse(lastMessage.TimeStamp) + 1000;

						presenceIsRich = ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum) &&
						                 GetAlbum(AlbumKeyMapping, currentAlbum)
							                 .ContainsKey(activeClient.ApplicationID);

						WrongArtistFlag = HasNameNotQuite(new Album(lastMessage.Album.Name));

						activeClient.SetPresence(new RichPresence()
						{
							Details = CapLength($"Title: {lastMessage.Title}", 32),
							State = CapLength(
								$"Artist: {(lastMessage.Artist == "" ? "Unkown Artist" : lastMessage.Artist)}", 32),
							Timestamps = isPlaying ? new Timestamps()
							{
								End = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(lastMessage.TimeStamp))
									.DateTime
							} : null,
							Assets = new Assets
							{
								LargeImageKey =
									ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum)
									&& GetAlbum(AlbumKeyMapping, currentAlbum)
										.ContainsKey(activeClient.ApplicationID) &&
									GetAlbum(AlbumKeyMapping, currentAlbum)[activeClient.ApplicationID]
										.Length <= 32
										? GetAlbum(AlbumKeyMapping, currentAlbum)[
											activeClient.ApplicationID]
										: BigAssets[playerName],
								LargeImageText = lastMessage.Album.Name.Length > 0
									? lastMessage.Album.Name.Length <= 2
										? "_" + lastMessage.Album.Name + "_"
										: lastMessage.Album.Name.Length > 128
											? lastMessage.Album.Name.Substring(0, 128)
											: lastMessage.Album.Name
									: "Unknown Album",
								SmallImageKey = isPlaying
									? (LittleAssets.ContainsKey(playerName)
										? LittleAssets[playerName]
										: defaultPlayer)
									: "paused",
								SmallImageText = isPlaying
									? ("Using " + Aliases[playerName])
									: "paused"
							}
						});
						activeClient.Invoke();
						foreach (DiscordRpcClient client in AllClients.Values)
							if (client.CurrentPresence != null &&
							    client.ApplicationID != activeClient.ApplicationID)
							{
#if DEBUG
										Console.WriteLine("Cleared " + client.ApplicationID);
#endif
								client.ClearPresence();
								try
								{
									client.Invoke();
								}
								catch (Exception e)
								{
#if DEBUG
											Console.WriteLine(e);
#endif
								}
							}
						SetConsole(lastMessage.Title, lastMessage.Artist, lastMessage.Album.Name, lastMessage.Album);
						if (!isPlaying)
						{
							Timer.Restart();
						}
					}
					remoteControl = remoteControl
						? (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds <
						  resignRemoteControlAt
						: false;
					if (!remoteControl)
					{
						wasPlaying = isPlaying;
						try
						{
							isPlaying = IsUsingAudio();
						}
						catch (Exception)
						{
							isPlaying = false;
						}

						if (wasPlaying && !isPlaying)
							Timer.Restart();
						if (RequiresPipeline.Contains(playerName))
						{
							if (!NotifiedRequiredPipeline)
							{
								Console.Clear();
								DrawPersistentHeader();
								Console.ForegroundColor = ConsoleColor.DarkRed;
								Console.WriteLine("Detected volume in " + playerName +
								                  " but no data has been recieved from it. You may need to update the player, install a plugin, or just pause and resume the music. See more at " +
								                  Github);
								NotifiedRequiredPipeline = true;
								Console.ForegroundColor = ConsoleColor.White;
							}
						}
						else if (EnabledClients.ContainsKey(playerName) && EnabledClients[playerName] &&
						         (isPlaying || Timer.ElapsedMilliseconds < timeout_seconds * 1000))
						{
							try
							{
								if (isPlaying)
									lastTrack = currentTrack;
								currentTrack = GetStuff();
								if (wasPlaying && !isPlaying)
								{
									Console.WriteLine(currentAlbum + " and " + new Album(currentTrack.AlbumTitle,
										currentTrack.Artist,
										currentTrack.AlbumArtist));
									activeClient.UpdateSmallAsset("paused", "paused");
									activeClient.Invoke();
									SetConsole(lastTrack.Title, lastTrack.Artist, lastTrack.AlbumTitle,
										currentAlbum);
								}
								else if
								( /*(!currentAlbum.Equals(new Album(currentTrack.AlbumTitle, currentTrack.Artist,
								          currentTrack.AlbumArtist))
							          || playerName != lastPlayer || currentTrack.Title != lastTrack.Title) &&*/
									isPlaying)
								{
									currentAlbum = new Album(currentTrack.AlbumTitle, currentTrack.Artist,
										currentTrack.AlbumArtist);
									GetClient();

									string details = $"Title: {currentTrack.Title}",
										state =
											$"Artist: {(currentTrack.Artist == "" ? "Unkown Artist" : currentTrack.Artist)}";
									if (activeClient.CurrentPresence == null ||
									    activeClient.CurrentPresence.Details !=
									    details.Substring(0, Math.Min(32, details.Length)) ||
									    activeClient.CurrentPresence.State !=
									    state.Substring(0, Math.Min(32, state.Length)) ||
									    wasPlaying != isPlaying)
									{
#if DEBUG
									Console.WriteLine("Using " + activeClient.ApplicationID + " (" +
									                  (ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum)
									                   && GetAlbum(AlbumKeyMapping, currentAlbum)
										                   .ContainsKey(activeClient.ApplicationID) &&
									                   GetAlbum(AlbumKeyMapping, currentAlbum)[
										                   activeClient.ApplicationID].Length <= 32
										                  ? GetAlbum(AlbumKeyMapping, currentAlbum)[
											                  activeClient.ApplicationID]
										                  : BigAssets[playerName]) + ")");
#endif
										presenceIsRich = ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum) &&
										                 GetAlbum(AlbumKeyMapping, currentAlbum)
											                 .ContainsKey(activeClient.ApplicationID);

										WrongArtistFlag = HasNameNotQuite(new Album(currentTrack.AlbumTitle));

										if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
										    currentAlbum.Name != "")
										{
											NotifiedAlbums.Add(currentAlbum);
											if (WrongArtistFlag)
											{
												SendNotification("Album keyed wrong",
													currentAlbum.Name +
													" is keyed for a different artist (check caps). To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
											}
											else
											{
												SendNotification("Album not keyed",
													currentAlbum.Name +
													" is not keyed. To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
											}
										}

										activeClient.SetPresence(new RichPresence
										{
											Details = details.Length > 32 ? details.Substring(0, 32) : details,
											State = state.Length > 32 ? state.Substring(0, 32) : state,
											Assets = new Assets
											{
												LargeImageKey =
													ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum)
													&& GetAlbum(AlbumKeyMapping, currentAlbum)
														.ContainsKey(activeClient.ApplicationID) &&
													GetAlbum(AlbumKeyMapping, currentAlbum)[activeClient.ApplicationID]
														.Length <= 32
														? GetAlbum(AlbumKeyMapping, currentAlbum)[
															activeClient.ApplicationID]
														: BigAssets[playerName],
												LargeImageText = currentTrack.AlbumTitle.Length > 0
													? currentTrack.AlbumTitle.Length <= 2
														? "_" + currentTrack.AlbumTitle + "_"
														: currentTrack.AlbumTitle.Length > 128
															? currentTrack.AlbumTitle.Substring(0, 128)
															: currentTrack.AlbumTitle
													: "Unknown Album",
												SmallImageKey = isPlaying
													? (LittleAssets.ContainsKey(playerName)
														? LittleAssets[playerName]
														: defaultPlayer)
													: "paused",
												SmallImageText = isPlaying
													? ("Using " + Aliases[playerName])
													: "paused"
											}
										});
										SetConsole(currentTrack.Title, currentTrack.Artist, currentTrack.AlbumTitle,
											currentAlbum);
										activeClient.Invoke();
									}

									foreach (DiscordRpcClient client in AllClients.Values)
										if (client.CurrentPresence != null &&
										    client.ApplicationID != activeClient.ApplicationID)
										{
#if DEBUG
										Console.WriteLine("Cleared " + client.ApplicationID);
#endif
											client.ClearPresence();
											try
											{
												client.Invoke();
											}
											catch (Exception e)
											{
#if DEBUG
											Console.WriteLine(e);
#endif
											}
										}
								}

#if DEBUG
							Console.Write("" + (MetaTimer.ElapsedMilliseconds) + "(" +
							              (Timer.ElapsedMilliseconds /* < timeout_seconds * 1000*/) + ") in " +
							              playerName +
							              '\r');
#endif
							}
							catch (Exception e)
							{
#if DEBUG
							Console.WriteLine(e.StackTrace);
#else
								Console.WriteLine(e.Message);
#endif
								if (activeClient != null)
									activeClient.SetPresence(new RichPresence()
									{
										Details = "Failed to get track info"
									});
								Console.Write("Failed to get track info \r");
							}
						}
						else if (!EnabledClients.ContainsKey(playerName))
						{
							SetUnknown();
							foreach (DiscordRpcClient client in AllClients.Values)
								if (client.CurrentPresence != null)
								{
									client.ClearPresence();
									client.Invoke();
								}
						}
						else
						{
							SetClear();
#if DEBUG
						Console.Write("Cleared " + (MetaTimer.ElapsedMilliseconds) + "\r");
#endif
							foreach (DiscordRpcClient client in AllClients.Values)
								if (client != null && client.CurrentPresence != null)
								{
									client.ClearPresence();
									//client.Invoke();
								}
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					Console.WriteLine("Something unexpected has occured");
				}
			}
		}

		private static void GetClient()
		{
			if (ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum))
			{
				activeClient = GetBestClient(GetAlbum(AlbumKeyMapping, currentAlbum));
			}
			else if (DefaultClients.ContainsKey(playerName))
				activeClient = DefaultClients[playerName];
			else
				activeClient = DefaultClients[""];

			if (activeClient == null)
			{
				activeClient = DefaultClients[playerName];
				Console.WriteLine("Uh oh!!!");
			}
		}

		private static DiscordRpcClient GetBestClient(Dictionary<string, string> album)
		{
			try
			{
				if (PlayersClients.ContainsKey(playerName))
					foreach (DiscordRpcClient klient in PlayersClients[playerName])
					{
						if (album.ContainsKey(klient.ApplicationID))
							return klient;
					}
			}
			catch (Exception e)
			{
#if DEBUG
				Console.WriteLine(e);
#endif
			}

			return DefaultClients[playerName];
		}

		public static string CapLength(string instring, int capLength)
		{
			return instring.Substring(0, Math.Min(capLength, instring.Length));
		}

		private static bool IsInitialized()
		{
			foreach (DiscordRpcClient client in AllClients.Values)
			{
				if (!client.IsInitialized)
					client.Initialize();
				//return false;
			}

			return true;
		}

		private static void SetConsole(string title, string artist, string albumName, Album album)
		{
			Console.Clear();

			DrawPersistentHeader();

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Music details:");

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("  Title: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" Artist: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(artist == "" ? "Unknown Artist" : artist);

			if (!albumName.Equals(string.Empty))
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write("  Album: ");

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(albumName);

				string albumKey = ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), album) &&
				                  GetAlbum(AlbumKeyMapping, album).ContainsKey(activeClient.ApplicationID)
					? GetAlbum(AlbumKeyMapping, album)[activeClient.ApplicationID]
					: BigAssets[playerName];
				if (albumKey.Length > 32)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("         The key for this album is too long. It must be 32 characters or less");
					if (ScreamAtUser && !ContainsAlbum(NotifiedAlbums.ToArray(), currentAlbum))
					{
						NotifiedAlbums.Add(currentAlbum);
						SendNotification("Album key in discord too long",
							$"The key for {currentAlbum.Name} ({albumKey}) is longer than the 32 character maximum");
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" Player: ");

			Console.ForegroundColor =
				PlayerColors.ContainsKey(playerName) ? PlayerColors[playerName] : ConsoleColor.White;
			Console.Write(Whatpeoplecallthisplayer[playerName]);

			if (remoteControl)
			{
				Console.Write(" using special integration!");
			}

			Console.WriteLine();

			if (presenceIsRich)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("\nThis is a good one, check ur DRP ;)");
			}
			else if (WrongArtistFlag)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("\nAlbum keyed for the wrong artist :/");
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("\nAlbum not keyed :(");
			}

			Console.ForegroundColor = ConsoleColor.White;
			justcleared = false;
			justUnknowned = false;
		}

		private static void SetClear()
		{
			if (!justcleared)
			{
				justcleared = true;
				Console.Clear();
				DrawPersistentHeader();
				Console.Write("Nothing Playing (probably paused)\r");
			}
		}

		private static void DrawPersistentHeader()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Version: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(Version);

			if (UpdateAvailibleFlag)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(" NEW UPDATE " + UpdateVersion + " (goto github to download)");
			}

			Console.WriteLine();

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Github: ");

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(Github);

			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.White;
		}

		private static void SetUnknown()
		{
			if (!justUnknowned)
			{
				justUnknowned = true;
				Console.Clear();
				DrawPersistentHeader();
				Console.Write(
					"Detected volume in something but not showing as it is not currently supported or is disabled");
			}
		}

		private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
		{
			if (args.Presence != null)
			{
				if (_presenceDetails != args.Presence.Details)
				{
					_presenceDetails = AllClients[args.ApplicationID].CurrentPresence?.Details;
				}
			}
			else
			{
				_presenceDetails = string.Empty;
			}
		}

		private static void _client_OnError(object sender, ErrorMessage args)
		{
			Console.WriteLine(args.Message);
		}

		//Get palying details
		private static GlobalSystemMediaTransportControlsSessionMediaProperties GetStuff()
		{
			var gsmtcsm = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult()
				.GetCurrentSession();
			return gsmtcsm.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
		}

		private static bool IsUsingAudio()
		{
			//Music.UI is Groove. Additional options include chrome, spotify, etc
			List<Process> candidates = new List<Process>();
			foreach (string program in ValidPlayers)
				if (EnabledClients.ContainsKey(program) && EnabledClients[program])
					foreach (Process process in Process.GetProcessesByName(program))
						candidates.Add(process);
			if (candidates.Any())
			{
				AudioSessionManager2 sessionManager;
				using (var enumerator = new MMDeviceEnumerator())
				{
					using (var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
					{
						sessionManager = AudioSessionManager2.FromMMDevice(device);
					}
				}

				using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
				{
					foreach (var session in sessionEnumerator)
					{
						var process = session.QueryInterface<AudioSessionControl2>().Process;
						try
						{
							if (ValidPlayers.Contains(process.ProcessName.ToLower()) &&
							    EnabledClients.ContainsKey(process.ProcessName.ToLower()) &&
							    EnabledClients[process.ProcessName.ToLower()] &&
							    session.QueryInterface<AudioMeterInformation>().GetPeakValue() > 0)
							{
								lastPlayer = playerName;
								playerName = process.ProcessName.ToLower();
								return true;
							}
						}
						catch (Exception e)
						{
#if DEBUG
							Console.WriteLine(e.StackTrace);
#else
							Console.WriteLine(e.Message);
#endif
						}
					}
				}
			}

			return false;
		}

		private static void SendNotification(string message)
		{
			SendNotification("MDRP message", message);
		}

		private static void SendNotification(string messageTitle, string message)
		{
			new ToastContentBuilder()
				.AddText(messageTitle)
				.AddText(message)
				.Show();
			/*ProcessStartInfo errornotif =
				new ProcessStartInfo("sendNotification.bat", "\"" + messageTitle + "\" \"" + message + "\"");
			errornotif.WindowStyle = ProcessWindowStyle.Hidden;
			Process.Start(errornotif);*/
		}

		private static void LoadSettings()
		{
			try
			{
				string[] lines = File.ReadAllLines("../../../DiscordPresenceConfig.ini");
				foreach (string line in lines)
				{
					if (ValidPlayers.Contains(line.Split('=')[0].Trim().ToLower()))
					{
						EnabledClients[line.Split('=')[0]] = line.Split('=')[1].Trim().ToLower() == "true";
						if (line.Split('=').Length > 2)
						{
							DefaultClients[line.Split('=')[0]] =
								new DiscordRpcClient(line.Split('=')[2], autoEvents: false);
						}
					}
					else if (InverseWhatpeoplecallthisplayer.ContainsKey(line.Split('=')[0].Trim().ToLower()) &&
					         ValidPlayers.Contains(InverseWhatpeoplecallthisplayer[line.Split('=')[0].Trim().ToLower()])
					)
					{
						EnabledClients.Add(line.Split('=')[0], line.Split('=')[1].Trim().ToLower() == "true");
						if (line.Split('=').Length > 2)
						{
							Console.WriteLine(InverseWhatpeoplecallthisplayer[line.Split('=')[0]]);
							DefaultClients[InverseWhatpeoplecallthisplayer[line.Split('=')[0]]] =
								new DiscordRpcClient(line.Split('=')[2], autoEvents: false);
						}
					}
					else if (line.Split('=')[0].Trim().ToLower() == "verbose" && line.Split('=').Length > 1)
					{
						ScreamAtUser = line.Split('=')[1].Trim().ToLower() == "true";
					}
				}
			}
			catch (Exception)
			{
				Console.Error.WriteLine(
					"DiscordPresenceConfig.ini not found! this is the settings file to enable or disable certain features");
				Thread.Sleep(5000);
			}

			try
			{
				ReadKeyingFromFile(new DirectoryInfo("../../../clientdata"));
			}
			catch (Exception)
			{
				Console.WriteLine("Something bad happened");
			}
		}

		private static void ReadKeyingFromFile(DirectoryInfo files)
		{
			foreach (var dir in files.GetDirectories())
				ReadKeyingFromFile(dir);
			foreach (var file in files.GetFiles())
			{
				if (file.Name == "demo.dat")
					continue;
				try
				{
					string[] lines = File.ReadAllLines(file.FullName);
					if (!ValidPlayers.Contains(lines[0].Split('=')[0]))
					{
						Console.Error.WriteLine("Error in file " + file.Name + " not a valid player name");
						SendNotification("Error in clientdata",
							"Error in file " + file.Name + ": " + lines[0].Split('=')[0] +
							" is not a valid player name");
						Thread.Sleep(5000);
						continue;
					}

					if (!lines[1].ToLower().Contains("id="))
					{
						Console.Error.WriteLine("Error in file " + file.Name + " no id found on the second line");
						SendNotification("\"MDRP settings issue\"",
							"\"Error in file " + file.Name + " no id found on the second line\"");
						Thread.Sleep(5000);
						continue;
					}

					string id = lines[1].Split('=')[1].Trim();
					if (!AllClients.ContainsKey(id))
					{
						AllClients.Add(id, new DiscordRpcClient(id, autoEvents: false));
						if (!PlayersClients.ContainsKey(lines[0].Split('=')[0]))
							PlayersClients.Add(lines[0].Split('=')[0], new DiscordRpcClient[0]);
						PlayersClients[lines[0].Split('=')[0]] =
							PlayersClients[lines[0].Split('=')[0]].Append(AllClients[id]).ToArray();
						if (!DefaultClients.ContainsKey(lines[0].Split('=')[0]))
							DefaultClients.Add(lines[0].Split('=')[0], AllClients[id]);
					}

					bool warnedFile = false;
					for (int i = 2; i < lines.Length; i++)
					{
						bool foundDupe = false;
						Album album;
						string[] parsedLine;
						if (lines[i].Contains("=="))
						{
							parsedLine = Regex.Split(lines[i], @"==");
						}
						else if (lines[i].Contains('='))
						{
							parsedLine = Regex.Split(lines[i], @"=");
						}
						else
						{
							if (!warnedFile)
							{
								warnedFile = true;
								SendNotification("Deprecation Notice",
									$"{file.Name} uses a deprecated keying format. Albums sould go in form Name==key==Artist");
							}

							continue;
						}

						if (parsedLine.Length == 2)
						{
							album = new Album(parsedLine[0]);
						}
						else
						{
							album = new Album(parsedLine[0],
								parsedLine.Skip(2).Take(parsedLine.Length - 2).ToArray());
						}

						if (!ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), album))
							AlbumKeyMapping.Add(album, new Dictionary<string, string>());
						else
						{
							foreach (DiscordRpcClient otherKlient in PlayersClients[lines[0].Split('=')[0]])
							{
								if (otherKlient.ApplicationID != id)
									foundDupe |= GetAlbum(AlbumKeyMapping, album)
										.ContainsKey(otherKlient.ApplicationID);
							}

							if (foundDupe)
								continue;
						}

						if (!GetAlbum(AlbumKeyMapping, album).ContainsKey(id))
							GetAlbum(AlbumKeyMapping, album).Add(id, parsedLine[1]);
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					Thread.Sleep(1000);
				}
			}
		}

		private static void CheckForUpdate()
		{
			UpdateTimer.Restart();
			Client.GetAsync(uri).AsTask().ContinueWith((Task<HttpResponseMessage> response) =>
			{
				IHttpContent responseContent = response.Result.Content;
				foreach (string str in Regex.Split(responseContent.ToString(), ","))
				{
					if (str.Contains("\"tag_name\":"))
					{
						UpdateVersion = str.Replace("\"tag_name\":\"", "").Replace("\"", "");
						if (ScreamAtUser && !UpdateAvailibleFlag)
							if (str.Replace("\"tag_name\":\"", "").Replace("\"", "") != Version)
								SendNotification("Update Available",
									UpdateVersion + " is published on github. Go there for the latest version");
						UpdateAvailibleFlag = UpdateVersion != Version;
					}
				}
			});
		}

		private static void GenerateShortcuts()
		{
			WshShell shell;
			IWshShortcut shortcut;
			string rootFolder;
			rootFolder = Directory.GetParent(
				Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName;
			shell = new WshShell();

			Directory.CreateDirectory(rootFolder + "\\Shortcuts");

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Run MDRP Windowed.lnk");
			shortcut.Description = "Run MDRP";
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\GroovyRP.exe";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\RunHidden.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Run MDRP Background.lnk");
			shortcut.Description = "Run MDRP";
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\GroovyRP.exe";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\RunHidden.vbs";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With MusicBee.lnk");
			shortcut.Description = "Link With MusicBee";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithMusicBee.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With Groove.lnk");
			shortcut.Description = "Link With Groove";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithGroove.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With Spotify.lnk");
			shortcut.Description = "Link With Spotify";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithSpotify.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink MusicBee.lnk");
			shortcut.Description = "Unlink With MusicBee";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromMusicBee.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink Spotify.lnk");
			shortcut.Description = "Unlink With Spotify";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromSpotify.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink Groove.lnk");
			shortcut.Description = "Unlink With Groove";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromGroove.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Kill Hidden.lnk");
			shortcut.Description = "Kills MDRP";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\KillHidden.vbs";
			shortcut.Save();
		}

		private static bool ContainsAlbum(Album[] albumArray, Album album)
		{
			foreach (Album alboom in albumArray)
				if (alboom.Equals(album))
					return true;
			return false;
		}

		private static Dictionary<string, string> GetAlbum(
			Dictionary<Album, Dictionary<string, string>> idkwhattocallthis, Album query)
		{
			foreach (Album alboom in idkwhattocallthis.Keys)
				if (alboom.Equals(query))
					return idkwhattocallthis[alboom];
			throw new KeyNotFoundException();
		}

		/**
		 * Returns true if the loaded albums contain the title of the passed album, disregarding the artists
		 */
		private static bool HasNameNotQuite(Album query)
		{
			foreach (Album alboom in AlbumKeyMapping.Keys)
				if (alboom.Name == query.Name)
					return true;
			return false;
		}
	}

	class Album
	{
		public string Name;
		private string[] Artists;

		public Album(string name) : this(name, "*")
		{
		}

		public Album(string name, params string[] artists)
		{
			Name = name;
			Artists = artists.Where(artist => artist != "").ToArray();
		}

		public override string ToString()
		{
			return Name + " by " + String.Join(",", Artists);
		}

		/**
		 * Returns true if and only if the provided album has the same name and accepts the same artists
		 */
		public override bool Equals(object obj)
		{
			if (obj != null && typeof(Album).IsInstanceOfType(obj) && ((Album) obj).Name == Name)
			{
				if (((Album) obj).Artists.Length == 0 || ((Album) obj).Artists[0] == "*" || Artists.Length == 0 ||
				    Artists[0] == "*")
					return true;
				foreach (string arteest in Artists)
					if (arteest != "")
						foreach (string whyamithisdeep in ((Album) obj).Artists)
							if (whyamithisdeep != "" &&
							    (whyamithisdeep.Contains(arteest) || arteest.Contains(whyamithisdeep)))
								return true;
			}

			return false;
		}
	}
}