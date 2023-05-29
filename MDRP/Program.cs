using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Web.Http;
using CSCore.CoreAudioAPI;
using DiscordRPC;
using DiscordRPC.Message;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace MDRP
{
	internal partial class Program
	{
		public static LangHelper langHelper = new LangHelper();
		public const string Version = "1.7.3";
		public const string Github = "https://github.com/jojo2357/Music-Discord-Rich-Presence";
		public static readonly string Title = langHelper.get(LocalizableStrings.MDRP_FULL);
		private const int titleLength = 64;
		private const int artistLength = 64;
		private const int keyLength = 32;

		public static bool showPlayers = false;

		private static readonly Uri _GithubUrl =
			new Uri("https://api.github.com/repos/jojo2357/Music-Discord-Rich-Presence/releases/latest");

		//Player Name, client
		private static readonly Dictionary<string, DiscordRpcClient> DefaultClients =
			new Dictionary<string, DiscordRpcClient>
			{
				{ "music.ui", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "microsoft.media.player", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "wmplayer", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "musicbee", new DiscordRpcClient("820837854385012766", autoEvents: false) },
				{ "spotify", new DiscordRpcClient("802222525110812725", autoEvents: false) },
				{ "tidal", new DiscordRpcClient("922625678271197215", autoEvents: false) },
				{ "tidalplayer", new DiscordRpcClient("922625678271197215", autoEvents: false) },
				{ "wavelink", new DiscordRpcClient("927328178618376212", autoEvents: false) },
				{ "amazon music", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "foobar2000", new DiscordRpcClient("1009193842211299428", autoEvents: false) },
				{ "", new DiscordRpcClient("821398156905283585", autoEvents: false) }
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

		public static readonly ExternalArtManager mngr = new ExternalArtManager();

		//ID, process name
		//process name, enabled y/n
		private static readonly Dictionary<string, bool> EnabledClients = new Dictionary<string, bool>
		{
			{ "music.ui", true }
		};

		private static readonly Dictionary<string, ConsoleColor> PlayerColors = new Dictionary<string, ConsoleColor>
		{
			{ "music.ui", ConsoleColor.Blue },
			{ "microsoft.media.player", ConsoleColor.Blue },
			{ "wmplayer", ConsoleColor.Blue },
			{ "spotify", ConsoleColor.DarkGreen },
			{ "musicbee", ConsoleColor.Yellow },
			{ "tidal", ConsoleColor.Gray },
			{ "tidalplayer", ConsoleColor.Gray },
			{ "wavelink", ConsoleColor.DarkBlue },
			{ "amazon music", ConsoleColor.Gray },
			{ "foobar2000", ConsoleColor.DarkCyan},
		};

		private static string _presenceDetails = string.Empty;

		private static readonly string[] ValidPlayers =
		{
			"music.ui", "spotify", "musicbee", "microsoft.media.player", "wmplayer", "tidal", "tidalplayer", "wavelink", "amazon music", "foobar2000"
		};

		private static readonly string[] RequiresPipeline = { "musicbee" };

		//For use in settings
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "spotify", "Spotify Music" },
			{ "groove", "Groove Music Player" },
			{ "microsoft.media.player", "Windows Media Player" },
			{ "wmplayer", "Windows Media Player" },
			{ "music.ui", "Groove Music Player" },
			{ "tidal", "Tidal Music" },
			{ "tidalplayer", "Tidal Music" },
			{ "wavelink", "Wave Link" },
			{ "amazon music", "Amazon Music" },
			{ "foobar2000", "FooBar2000"},
		};

		private static readonly Dictionary<string, string> BigAssets = new Dictionary<string, string>
		{
			{ "musicbee", "https://cdn.discordapp.com/app-assets/820837854385012766/820838584617009222.png" },
			{ "music.ui", "https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png" },
			{ "amazon music", "https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png" },
			{
				"microsoft.media.player",
				"https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png"
			},
			{ "spotify", "https://cdn.discordapp.com/app-assets/802222525110812725/802222954821582869.png" },
			{ "tidal", "https://cdn.discordapp.com/app-assets/922625678271197215/978018192393920562.png" },
			{ "tidalplayer", "https://cdn.discordapp.com/app-assets/922625678271197215/978018192393920562.png" },
			{ "wavelink", "https://cdn.discordapp.com/app-assets/927328178618376212/927329727180574760.png" },
			{ "foobar2000", "https://cdn.discordapp.com/app-assets/1009193842211299428/1009196080853950464.png" },
		};

		//might just combine these later
		private static readonly Dictionary<string, string> LittleAssets = new Dictionary<string, string>
		{
			{ "musicbee", "https://cdn.discordapp.com/app-assets/820837854385012766/820838635703500810.png" },
			{ "music.ui", "https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png" },
			{ "amazon music", "https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png" },
			{
				"microsoft.media.player",
				"https://cdn.discordapp.com/app-assets/801209905020272681/801224923547959376.png"
			},
			{ "spotify", "https://cdn.discordapp.com/app-assets/802222525110812725/802222992683827200.png" },
			{ "tidal", "https://cdn.discordapp.com/app-assets/922625678271197215/978018192637198406.png" },
			{ "tidalplayer", "https://cdn.discordapp.com/app-assets/922625678271197215/978018192637198406.png" },
			{ "wavelink", "https://cdn.discordapp.com/app-assets/927328178618376212/927329727218319410.png" },
			{ "foobar2000", "https://cdn.discordapp.com/app-assets/1009193842211299428/1009196080950423563.png" },
		};

		private static readonly Dictionary<string, string> Whatpeoplecallthisplayer = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "music.ui", "Groove Music" },
			{ "microsoft.media.player", "Windows Media Player" },
			{ "wmplayer", "Windows Media Player" },
			{ "spotify", "Spotify" },
			{ "tidal", "Tidal Music" },
			{ "tidalplayer", "Tidal Music" },
			{ "wavelink", "Wave Link" },
			{ "amazon music", "Amazon Music" },
			{ "foobar2000", "FooBar2000"},
		};

		private static readonly Dictionary<string, string> InverseWhatpeoplecallthisplayer =
			new Dictionary<string, string>
			{
				{ "musicbee", "musicbee" },
				{ "groove", "music.ui" },
				{ "Microsoft.Media.Player", "microsoft.media.player" },
				{ "spotify", "spotify" },
				{ "Tidal Music", "tidal" },
				{ "Wave Link", "wavelink" },
				{ "Amazon Music", "anazon music" },
				{ "FooBar2000", "foobar2000"},
			};

		private static readonly string defaultPlayer = "groove";
		private static readonly int timeout_seconds = 60;

		private static readonly Stopwatch Timer = new Stopwatch(),
			MetaTimer = new Stopwatch(),
			UpdateTimer = new Stopwatch();

		private static string _playerName = string.Empty, _lastPlayer = string.Empty;

		private static bool _justcleared,
			_justUnknowned,
			ScreamAtUser,
			presenceIsRich,
			WrongArtistFlag;

		public static bool UpdateAvailibleFlag;

		private static bool NotifiedRequiredPipeline;

		private static DiscordRpcClient activeClient;
		private static Album currentAlbum = new Album("");
		private static readonly HttpClient Client = new HttpClient();
		private const long updateCheckInterval = 36000000;
		public static string UpdateVersion;

		public static HttpListener listener;
		public static string url = "http://localhost:2357/";

		public const string defaultPausedURL =
			"https://cdn.discordapp.com/app-assets/801209905020272681/801527319537516596.png";

		public static string pausedAsset;

		public static bool useRemoteArt = false, needsExactMatch = true, createCacheFile = true;
		public static bool remoteControl;
		public static bool translateFromJapanese = true;
		public static long resignRemoteControlAt;

		private static readonly Queue<JsonResponse> _PendingMessages = new Queue<JsonResponse>();
		public static bool spawnedFromApplication;
		private static bool _isPlaying;
		private static bool _wasPlaying;
		private static GlobalSystemMediaTransportControlsSessionMediaProperties _currentTrack = null;
		private static GlobalSystemMediaTransportControlsSessionMediaProperties _lastTrack = null;

		private static string lineData = "", currentTitle = "";
		private static bool foundFirst = false, foundSecond = false;
		private static string buttonText = "Click Me!", buttonURL = "https://archive.org/donate/";
		private static bool foundButtonText = false, foundButtonURL = false;
		private static bool foundImageRemotely = false;

		private static readonly Regex _smallAssetRex =
			new Regex("(?<=^small\\s?)\\b[\\w\\\\.]+\\b(?=\\s?asset)", RegexOptions.IgnoreCase);

		private static readonly Regex _largeAssetReg =
			new Regex("(?<=^large\\s?)\\b[\\w\\\\.]+\\b(?=\\s?asset)", RegexOptions.IgnoreCase);

		private static String GetArtist(JsonResponse lastMessage)
		{
			if (lastMessage != null)
			{
				if (lastMessage.Artist != "" || lastMessage.AlbumArtist != "")
				{
					if (lastMessage.Artist != "")
					{
						return lastMessage.Artist;
					}
					return lastMessage.AlbumArtist;
				}
			}
			else if (_currentTrack.Artist != "" || _currentTrack.AlbumArtist != "")
			{
				if (_currentTrack.Artist != "")
				{
					return _currentTrack.Artist;
				}

				return _currentTrack.AlbumArtist;
			}
			return langHelper[LocalizableStrings.UNKNOWN_ARTIST];
		}

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
				Console.WriteLine(req.Url.ToString());
				Console.WriteLine(req.HttpMethod);
				Console.WriteLine("Time: " +
				                  (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
#endif
				string text;
				using (StreamReader reader = new StreamReader(req.InputStream,
					       req.ContentEncoding))
				{
					text = reader.ReadToEnd();
				}

				string decodedText = Uri.UnescapeDataString(text);

				if (decodedText == "{message:\"please die\"}")
				{
					byte[] deathdata = Encoding.UTF8.GetBytes("{message:\"i die now\"");
					resp.OutputStream.Write(deathdata, 0, deathdata.Length);
					resp.Close();
					listener.Close();
					Environment.Exit(0);
				}

				string response;
				try
				{
					JObject jason = JObject.Parse(decodedText);
					//Console.WriteLine("Tmes: " + jason["timestamp"]);
					JsonResponse parsedJason = new JsonResponse(jason);
					if (parsedJason.isValid())
					{
#if DEBUG
						Console.WriteLine("Enqueuing");
#endif
						_PendingMessages.Enqueue(parsedJason);
						GetClient(parsedJason.Album, parsedJason.Player);
						presenceIsRich = AlbumKeyMapping.ContainsKey(parsedJason.Album) &&
						                 AlbumKeyMapping[parsedJason.Album]
							                 .ContainsKey(activeClient.ApplicationID);

						WrongArtistFlag = HasNameNotQuite(new Album(parsedJason.Album.Name), parsedJason.Player);
						if (!presenceIsRich)
						{
							if (WrongArtistFlag)
								response = "{response:\"keyed incorrectly\"}";
							else
							{
								GetLargeImageKey(parsedJason.Album, parsedJason.Title);
								if (foundImageRemotely)
									response = "{response:\"found remotely\"}";
								else
									response = "{response:\"no key\"}";
							}
						}
						else
						{
							response = "{response:\"keyed successfully\"}";
						}
					}
					else if (jason.ContainsKey("player") || decodedText == "")
					{
						response = "{application:\"MDRP\",version:\"" + Version + "\"}";
					}
					else
					{
#if DEBUG
						Console.WriteLine("Invalid JSON ");
#endif
						response = "{response:\"invalid json " + parsedJason.getReasonInvalid() + "\"}";
					}
				}
				catch (Exception e)
				{
					Functions.SendToDebugServer(e);
					Functions.SendToDebugServer("failure to parse incoming json: \n" + text + " (" + decodedText + ")");
					response = "{response:\"failure to parse json\"}";
#if DEBUG
					Console.WriteLine(response);
					Console.WriteLine(e);
#endif
				}
#if DEBUG
				Console.WriteLine(decodedText);
				Console.WriteLine();
#endif

				byte[] data = Encoding.UTF8.GetBytes(response);
				resp.ContentType = "text/json";
				resp.ContentEncoding = Encoding.UTF8;
				resp.ContentLength64 = data.LongLength;

				// Write out to the response stream (asynchronously), then close it
				await resp.OutputStream.WriteAsync(data, 0, data.Length);
				resp.Close();
			}
		}

		private static void doServer()
		{
			listener = new HttpListener();
			listener.Prefixes.Add(url);
			try
			{
				listener.Start();
			}
			catch (Exception)
			{
				if (!AttemptToCloseConflict())
					return;
				listener = new HttpListener();
				listener.Prefixes.Add(url);
				listener.Start();
			}
			//Console.WriteLine("Listening for connections on {0}", url);

			// Handle requests
			Task listenTask = HandleIncomingConnections();
			listenTask.GetAwaiter().GetResult();

			// Close the listener
			listener.Close();
		}

		private static bool AttemptToCloseConflict()
		{
			Uri conflictedURI = new Uri("http://localhost:2357/");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(conflictedURI);
			request.Method = "POST";
			request.ContentType = "text/json";
			string urlEncoded = Uri.EscapeUriString("{message:\"please die\"}");
			byte[] arr = Encoding.UTF8.GetBytes(urlEncoded);
			try
			{
				Stream rs = request.GetRequestStream();
				rs.Write(arr, 0, arr.Length);
				WebResponse res = request.GetResponse();
				string text = "";
				using (StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
				{
					text = reader.ReadToEnd();
				}

				res.Close();

				if (text.ToLower() == "{message:\"i die now\"")
					return true;
				return false;
			}
			catch (Exception)
			{
				return true;
			}
		}


		private static void Main(string[] args)
		{
			Thread t = new Thread(doServer);
			t.Start();
			Console.OutputEncoding = Encoding.UTF8;
			Console.Title = langHelper.get(LocalizableStrings.CONSOLE_NAME);

			Client.DefaultRequestHeaders["User-Agent"] = "c#";
			//Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			Functions.GenerateShortcuts();

			if (args.Length > 0)
				if (args[0] == "Shortcuts_Only")
					return;
				else
					spawnedFromApplication = true;

			LoadSettings();

			foreach (DiscordRpcClient client in DefaultClients.Values)
				if (!AllClients.ContainsKey(client.ApplicationID))
					AllClients.Add(client.ApplicationID, client);
				else
					AllClients[client.ApplicationID] = client;

			MetaTimer.Start();
			Timer.Start();
			UpdateTimer.Start();

			CheckForUpdate();

			//SendToDebugServer("Starting up");

			foreach (DiscordRpcClient client in AllClients.Values)
			{
				if (!client.Initialize()) Console.WriteLine("Could not init client with id: " + client.ApplicationID);
				client.OnError += _client_OnError;
			}

			try
			{
				_currentTrack = Functions.GetPlayingDetails();
				_lastTrack = _currentTrack;
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
			}

			_isPlaying = IsUsingAudio();

			while (IsInitialized())
				try
				{
					//limit performace impact
					if (UpdateTimer.ElapsedMilliseconds > updateCheckInterval)
						CheckForUpdate();
					Thread.Sleep(2000);
					if (_PendingMessages.Count > 0) HandleRemoteRequests();

					if (!remoteControl) HandleLocalRequests();

					if (remoteControl && (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds >
					    resignRemoteControlAt)
					{
						UnsetAllPresences();
						SetClear();
						remoteControl = false;
					}
				}
				catch (Exception e)
				{
					Functions.SendToDebugServer(e);
				}
		}

		private static void HandleLocalRequests()
		{
			_wasPlaying = _isPlaying;
			try
			{
				_isPlaying = IsUsingAudio();
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
				_isPlaying = false;
			}

			if (_wasPlaying && !_isPlaying)
				Timer.Restart();
			if (RequiresPipeline.Contains(_playerName))
			{
				if (!NotifiedRequiredPipeline)
				{
#if DEBUG
#else
					Console.Clear();
#endif
					Functions.DrawPersistentHeader();
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine(string.Format(langHelper[LocalizableStrings.REQUIRE_PIPELINE], _playerName) +
					                  " " + Github);
					NotifiedRequiredPipeline = true;
					Console.ForegroundColor = ConsoleColor.White;
				}
			}
			else if (EnabledClients.ContainsKey(_playerName) && EnabledClients[_playerName] &&
			         (_isPlaying || Timer.ElapsedMilliseconds < timeout_seconds * 1000))
			{
				try
				{
					if (_isPlaying)
						_lastTrack = _currentTrack;
					_currentTrack = Functions.GetPlayingDetails();
					if (_currentTrack == null)
					{
						Console.Error.WriteLine("Failed to find any WMC data");
						return;
					}

					//Console.WriteLine("Title: " + _currentTrack.Title + " Album: " + _currentTrack.AlbumTitle);
					if (_wasPlaying && !_isPlaying)
					{
						activeClient.SetPresence(new RichPresence
						{
							Details = activeClient.CurrentPresence.Details,
							State = activeClient.CurrentPresence.State,
							Assets = new Assets
							{
								LargeImageKey = GetLargeImageKey(),
								LargeImageText = Functions.GetLargeImageText(_currentTrack.AlbumTitle),
								SmallImageKey = GetSmallImageKey(),
								SmallImageText = GetSmallImageText()
							}
						});
						InvokeActiveClient();
						SetConsole(_lastTrack.Title, GetArtist(null), _lastTrack.AlbumTitle,
							currentAlbum);
					}
					else if
						( /*(!currentAlbum.Equals(new Album(currentTrack.AlbumTitle, currentTrack.Artist,
								          currentTrack.AlbumArtist))
							          || playerName != lastPlayer || currentTrack.Title != lastTrack.Title) &&*/
						 _isPlaying)
					{
						currentAlbum = new Album(_currentTrack.AlbumTitle, _currentTrack.Artist,
							_currentTrack.AlbumArtist);
						currentTitle = _currentTrack.Title;
						GetClient();

						foreach (DiscordRpcClient client in AllClients.Values)
							if (client.CurrentPresence != null &&
							    client.ApplicationID != activeClient.ApplicationID)
								try
								{
									Functions.ClearAPresence(client);
								}
								catch (Exception e)
								{
									Functions.SendToDebugServer(e);
								}

						string newDetailsWithTitle = Functions.CapLength(
							lineData.Split('\n')[0]
								.Replace("${artist}",
									(GetArtist(null))).Replace("${title}", _currentTrack.Title)
								.Replace("${album}", _currentTrack.AlbumTitle), titleLength);
						string newStateWithArtist = Functions.CapLength(
							lineData.Split('\n')[1]
								.Replace("${artist}",
									(GetArtist(null))).Replace("${title}", _currentTrack.Title)
								.Replace("${album}", _currentTrack.AlbumTitle), artistLength);
						if (activeClient.CurrentPresence == null ||
						    activeClient.CurrentPresence.Details != newDetailsWithTitle ||
						    activeClient.CurrentPresence.State != newStateWithArtist || _wasPlaying != _isPlaying)
						{
							presenceIsRich = AlbumKeyMapping.ContainsKey(currentAlbum) &&
							                 AlbumKeyMapping[currentAlbum]
								                 .ContainsKey(activeClient.ApplicationID);

							WrongArtistFlag = HasNameNotQuite(new Album(_currentTrack.AlbumTitle), _playerName);

							if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
							    currentAlbum.Name != "")
							{
								NotifiedAlbums.Add(currentAlbum);
								if (useRemoteArt)
								{
									if (mngr.AlbumLookup(currentAlbum, currentTitle) == "")
									{
										Functions.SendNotification(
											langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_HEADER],
											string.Format(langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_BODY],
												currentAlbum));
									}
								}
								else
								{
									if (WrongArtistFlag)
										Functions.SendNotification(
											langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_HEADER],
											currentAlbum.Name +
											" " + langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_BODY]);
									else
										Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_UNKEYED_HEADER],
											currentAlbum.Name + " " +
											langHelper[LocalizableStrings.NOTIF_UNKEYED_BODY]);
								}
							}

							activeClient.SetPresence(new RichPresence
							{
								Details = newDetailsWithTitle,
								State = newStateWithArtist,
								Assets = new Assets
								{
									LargeImageKey = GetLargeImageKey(),
									LargeImageText = Functions.GetLargeImageText(_currentTrack.AlbumTitle),
									SmallImageKey = GetSmallImageKey(),
									SmallImageText = GetSmallImageText()
								}
							});
							SetConsole(_currentTrack.Title, GetArtist(null), _currentTrack.AlbumTitle,
								currentAlbum);
							InvokeActiveClient();
						}
					}

#if DEBUG
					Console.Write("" + MetaTimer.ElapsedMilliseconds + "(" +
					              Timer.ElapsedMilliseconds + ") in " +
					              _playerName +
					              '\r');
#endif
				}
				catch (Exception e)
				{
					Functions.SendToDebugServer(e);
					if (activeClient != null)
						activeClient.SetPresence(new RichPresence
						{
							Details = langHelper[LocalizableStrings.FAILED_TO_GET_INFO]
						});
					Console.Write(langHelper[LocalizableStrings.FAILED_TO_GET_INFO] + " \r");
				}
			}
			else
			{
				if (showPlayers)
				{
					if (!_isPlaying)
					{
						Console.WriteLine(_playerName + " and not playing");
					}

					if (!(EnabledClients.ContainsKey(_playerName) && EnabledClients[_playerName]))
					{
						Console.WriteLine(_playerName + " is not enabled");
					}

					if (Timer.ElapsedMilliseconds >= timeout_seconds * 1000)
					{
						Console.WriteLine("TIMED OUT");
					}
				}

				if (!EnabledClients.ContainsKey(_playerName))
					SetUnknown();
				else
					SetClear();
				UnsetAllPresences();
			}
		}

		private static void InvokeActiveClient()
		{
			if (!activeClient.IsInitialized) activeClient.Initialize();
			activeClient.Invoke();
		}

		private static void HandleRemoteRequests()
		{
			JsonResponse lastMessage = _PendingMessages.Last();

			_PendingMessages.Clear();
#if DEBUG
			Console.WriteLine("Recieved on main thread {0}", lastMessage);
#endif
			_wasPlaying = _isPlaying;
			remoteControl = lastMessage.Action != RemoteAction.Shutdown;
			_isPlaying = lastMessage.Action == RemoteAction.Play;
			if (!remoteControl)
			{
				UnsetAllPresences();
				SetClear();
				resignRemoteControlAt = 0;
			}
			else
			{
				foreach (DiscordRpcClient client in AllClients.Values)
					if (client.CurrentPresence != null && client.ApplicationID != activeClient.ApplicationID)
					{
						try
						{
							Functions.ClearAPresence(client);
						}
						catch (Exception e)
						{
							Functions.SendToDebugServer(e);
						}
					}
					else
					{
						try
						{
							client.Invoke();
						}
						catch (Exception e)
						{
							Functions.SendToDebugServer(e);
						}
					}

				currentAlbum = lastMessage.Album;
				_playerName = lastMessage.Player;
				GetClient();
#if DEBUG
				Console.WriteLine("Using " + activeClient.ApplicationID);
#endif
				if (_isPlaying)
					resignRemoteControlAt = long.Parse(lastMessage.TimeStamp) + 1000;
				else if (remoteControl)
					resignRemoteControlAt = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds +
					                        60000;
				else
					resignRemoteControlAt = 0;

				presenceIsRich = AlbumKeyMapping.ContainsKey(currentAlbum) &&
				                 AlbumKeyMapping[currentAlbum].ContainsKey(activeClient.ApplicationID);

				currentTitle = lastMessage.Title;
				WrongArtistFlag = HasNameNotQuite(new Album(lastMessage.Album.Name), _playerName);

				RichPresence richPresence = new RichPresence
				{
					Details = PrepareFormatStringCapped(lastMessage, lineData.Split('\n')[0], titleLength),
					State = PrepareFormatStringCapped(lastMessage, lineData.Split('\n')[1], titleLength),

					Timestamps = _isPlaying
						? new Timestamps
						{
							End = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(lastMessage.TimeStamp))
								.DateTime
						}
						: null,
					Assets = new Assets
					{
						LargeImageKey = GetLargeImageKey(),
						LargeImageText = Functions.GetLargeImageText(lastMessage.Album.Name),
						SmallImageKey = GetSmallImageKey(),
						SmallImageText = GetSmallImageText()
					},
				};
				
				if (foundButtonText || foundButtonURL)
				{
					richPresence.Buttons = new []{new Button
					{
						Label = PrepareFormatStringCapped(lastMessage, buttonText, 32),
						Url = PrepareEscapedFormatString(lastMessage, buttonURL)
					}};
				}
				
				activeClient.SetPresence(richPresence);
				

				InvokeActiveClient();

				if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
				    currentAlbum.Name != "")
				{
					NotifiedAlbums.Add(currentAlbum);
					if (useRemoteArt)
					{
						if (mngr.AlbumLookup(currentAlbum, currentTitle) == "")
						{
							Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_HEADER],
								string.Format(langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_BODY],
									currentAlbum));
						}
					}
					else
					{
						if (WrongArtistFlag)
							Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_HEADER],
								currentAlbum.Name + " " + langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_BODY]);
						else
							Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_UNKEYED_HEADER],
								currentAlbum.Name + " " + langHelper[LocalizableStrings.NOTIF_UNKEYED_BODY]);
					}
				}

				SetConsole(lastMessage.Title, GetArtist(lastMessage), lastMessage.Album.Name,
					lastMessage.Album);
				if (!_isPlaying) Timer.Restart();
			}
		}

		private static string PrepareFormatStringCapped(JsonResponse lastMessage, string instring, int capLength)
		{
			return Functions.CapLength(
				PrepareFormatString(lastMessage, instring), capLength);
		}

		private static string PrepareFormatString(JsonResponse lastMessage, string instring)
		{
			return instring.Replace("${artist}",
					(GetArtist(lastMessage))).Replace("${title}", lastMessage.Title)
				.Replace("${album}", lastMessage.Album.Name);
		}

		private static string PrepareEscapedFormatString(JsonResponse lastMessage, string instring)
		{
			return instring.Replace("${artist}",
					Uri.EscapeUriString(GetArtist(lastMessage))).Replace("${title}", Uri.EscapeUriString(lastMessage.Title))
				.Replace("${album}", Uri.EscapeUriString(lastMessage.Album.Name));
		}

		private static string GetSmallImageText()
		{
			if (_isPlaying)
				return langHelper[LocalizableStrings.USING] + " " + Aliases[_playerName];
			else
				return langHelper[LocalizableStrings.PAUSED];
		}

		private static string GetSmallImageKey()
		{
			if (_isPlaying)
				if (LittleAssets.ContainsKey(_playerName))
					return LittleAssets[_playerName];
				else
					return defaultPlayer;
			else
				return pausedAsset;
		}

		private static string GetLargeImageKey()
		{
			return GetLargeImageKey(currentAlbum, currentTitle);
		}

		private static string GetLargeImageKey(Album activeAlbum, string activeTitle)
		{
			foundImageRemotely = false;
			if (AlbumKeyMapping.ContainsKey(activeAlbum) &&
			    AlbumKeyMapping[activeAlbum].ContainsKey(activeClient.ApplicationID))
			{
				//make sure it is not too long, this will be warned about
				if (Uri.IsWellFormedUriString(AlbumKeyMapping[activeAlbum][activeClient.ApplicationID],
					    UriKind.Absolute))
				{
					return AlbumKeyMapping[activeAlbum][activeClient.ApplicationID];
				}

				if (AlbumKeyMapping[activeAlbum][activeClient.ApplicationID].Length <= keyLength)
				{
					return AlbumKeyMapping[activeAlbum][activeClient.ApplicationID];
				}
			}

			if (useRemoteArt)
			{
				string res = mngr.AlbumLookup(activeAlbum, activeTitle);
				if (res != String.Empty)
				{
					foundImageRemotely = true;
					return res;
				}

				if (ScreamAtUser)
				{
					if (!NotifiedAlbums.Contains(activeAlbum))
					{
						Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_HEADER],
							string.Format(langHelper[LocalizableStrings.NOTIF_NOT_FOUND_REMOTELY_BODY], activeAlbum));
						NotifiedAlbums.Add(activeAlbum);
					}
				}
			}

			return BigAssets.ContainsKey(_playerName) ? BigAssets[_playerName] : BigAssets[defaultPlayer];
		}

		private static void UnsetAllPresences()
		{
			foreach (DiscordRpcClient client in AllClients.Values)
				if (client.CurrentPresence != null)
					Functions.ClearAPresence(client);
		}

		private static void GetClient(Album album, string playerName)
		{
			if (AlbumKeyMapping.ContainsKey(album))
				activeClient = GetBestClient(AlbumKeyMapping[album], playerName);
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

		private static void GetClient()
		{
			GetClient(currentAlbum, _playerName);
		}

		private static DiscordRpcClient GetBestClient(Dictionary<string, string> album, string playerName)
		{
			if (playerName.ToLower() == "microsoft.media.player")
				playerName = "music.ui";
			try
			{
				if (PlayersClients.ContainsKey(playerName))
					foreach (DiscordRpcClient klient in PlayersClients[playerName])
						if (album.ContainsKey(klient.ApplicationID))
							return klient;
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
			}

			return DefaultClients[playerName];
		}

		private static bool IsInitialized()
		{
			foreach (DiscordRpcClient client in AllClients.Values)
				if (!client.IsInitialized)
					client.Initialize();
			return true;
		}

		private static void SetConsole(string title, string artist, string albumName, Album album)
		{
			int totalBufLen = Math.Max(langHelper.get(LocalizableStrings.ALBUM).Length,
				Math.Max(langHelper.get(LocalizableStrings.PLAYER).Length,
					Math.Max(langHelper[LocalizableStrings.TITLE].Length,
						langHelper[LocalizableStrings.ARTIST].Length)));
#if DEBUG
#else
			Console.Clear();
#endif

			Functions.DrawPersistentHeader();

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(langHelper.get(LocalizableStrings.DETAILS) + ":");

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(new string(' ', totalBufLen - langHelper.get(LocalizableStrings.TITLE).Length + 1) +
			              langHelper.get(LocalizableStrings.TITLE) + ": ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(new string(' ', totalBufLen - langHelper.get(LocalizableStrings.ARTIST).Length + 1) +
			              langHelper.get(LocalizableStrings.ARTIST) + ": ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(artist == "" ? langHelper.get(LocalizableStrings.UNKNOWN_ARTIST) : artist);

			if (!albumName.Equals(string.Empty))
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(new string(' ', totalBufLen - langHelper.get(LocalizableStrings.ALBUM).Length + 1) +
				              langHelper.get(LocalizableStrings.ALBUM) + ": ");

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(albumName);

				string albumKey = AlbumKeyMapping.ContainsKey(album) &&
				                  AlbumKeyMapping[album].ContainsKey(activeClient.ApplicationID)
					? AlbumKeyMapping[album][activeClient.ApplicationID]
					: BigAssets[_playerName];
				if (albumKey.Length > keyLength)
				{
					if (!Uri.IsWellFormedUriString(albumKey, UriKind.Absolute))
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(string.Format("         " + langHelper[LocalizableStrings.KEY_TOO_LONG],
							keyLength));
						if (ScreamAtUser && !NotifiedAlbums.ToArray().Contains(currentAlbum))
						{
							NotifiedAlbums.Add(currentAlbum);
							Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_KEY_TOO_LONG_HEADER],
								string.Format(langHelper[LocalizableStrings.NOTIF_KEY_TOO_LONG_BODY], currentAlbum.Name,
									albumKey, keyLength));
						}
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(new string(' ', totalBufLen - langHelper.get(LocalizableStrings.PLAYER).Length + 1) +
			              langHelper.get(LocalizableStrings.PLAYER) + ": ");

			Console.ForegroundColor =
				PlayerColors.ContainsKey(_playerName) ? PlayerColors[_playerName] : ConsoleColor.White;
			Console.Write(Whatpeoplecallthisplayer[_playerName]);

			if (remoteControl) Console.Write(" " + langHelper[LocalizableStrings.SPECIAL_INTEGRATION]);

			Console.WriteLine();

			if (presenceIsRich)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("\n" + langHelper.get(LocalizableStrings.GOOD_ONE));
			}
			else if (WrongArtistFlag)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("\n" + langHelper.get(LocalizableStrings.KEYED_WRONG) + " Got \"" +
				                  album.GetArtistString() + "\" but expected \"" +
				                  Program.GetNameNotQuite(album, _playerName).GetArtistString() + "\"");
				/*foreach (string str in Program.GetNameNotQuite(album, _playerName).Artists)
				{
				    Console.Write(album.GetArtistString() + ", " + str + "\t");
				    Console.Write(album.GetArtistString().Zip(str, (c1, c2) => c1 == c2).TakeWhile(b => b).Count() + " != " + album.GetArtistString().Zip(str, (c1, c2) => c1 == c2).TakeWhile(b => b).Count());
				    Console.WriteLine(" " + album.GetArtistString().ToCharArray()[album.GetArtistString().Zip(str, (c1, c2) => c1 == c2).TakeWhile(b => b).Count()] + " != " + str.ToCharArray()[album.GetArtistString().Zip(str, (c1, c2) => c1 == c2).TakeWhile(b => b).Count()]);
				}*/
			}
			else
			{
				if (foundImageRemotely)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("\n" + langHelper[LocalizableStrings.FOUND_REMOTELY]);
				}
				else if (!foundImageRemotely && useRemoteArt)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("\n" + langHelper[LocalizableStrings.NOT_FOUND_REMOTELY]);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("\n" + langHelper.get(LocalizableStrings.UNKEYED));
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			_justcleared = false;
			_justUnknowned = false;
		}

		private static void SetClear()
		{
			if (!_justcleared)
			{
				_justcleared = true;
#if DEBUG
#else
				Console.Clear();
#endif
				Functions.DrawPersistentHeader();
				Console.Write(langHelper[LocalizableStrings.NOTHING_PLAYING] + "\r");
			}
		}

		private static void SetUnknown()
		{
			if (!_justUnknowned)
			{
				_justUnknowned = true;
#if DEBUG
#else
				Console.Clear();
#endif
				Functions.DrawPersistentHeader();
				Console.Write(langHelper[LocalizableStrings.NO_VALID_MEDIA]);
			}
		}

		/*private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
		{
		    Console.WriteLine("Update presence: " + args.Name + " | " + args.Presence.State + " | " + args.Type);
		    if (args.Presence != null)
		    {
		        if (_presenceDetails != args.Presence.Details)
		            _presenceDetails = AllClients[args.ApplicationID].CurrentPresence?.Details;
		    }
		    else
		    {
		        _presenceDetails = string.Empty;
		    }
		}*/

		private static void _client_OnError(object sender, ErrorMessage args)
		{
			Functions.SendToDebugServer(args.ToString());
			Console.WriteLine(args.Message);
		}

		//Get palying details

		private static bool IsUsingAudio()
		{
			//Music.UI is Groove. Additional options include chrome, spotify, etc
			List<Process> candidates = new List<Process>();
			foreach (string program in ValidPlayers)
				if (EnabledClients.ContainsKey(program) && EnabledClients[program])
					foreach (Process process in Process.GetProcessesByName(program))
						candidates.Add(process);
			if (candidates.Any() || showPlayers)
			{
				using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
				{
					foreach (MMDevice audioOutputDevice in enumerator.EnumAudioEndpoints(DataFlow.Render,
						         DeviceState.Active))
					{
						try
						{
							using (AudioSessionEnumerator sessionEnumerator = AudioSessionManager2
								       .FromMMDevice(audioOutputDevice).GetSessionEnumerator())
							{
								foreach (AudioSessionControl session in sessionEnumerator)
								{
									Process process = session.QueryInterface<AudioSessionControl2>().Process;
									bool theresSomething = session.QueryInterface<AudioSessionControl2>()
										.QueryInterface<AudioMeterInformation>().PeakValue > 0;
									try
									{
										if (ValidPlayers.Contains(process.ProcessName.ToLower()) &&
										    EnabledClients.ContainsKey(process.ProcessName.ToLower()) &&
										    EnabledClients[process.ProcessName.ToLower()] &&
										    session.QueryInterface<AudioMeterInformation>().GetPeakValue() > 0)
										{
											if (showPlayers)
											{
												Console.ForegroundColor = ConsoleColor.White;
												Console.Write("Detected and accepting ");
												Console.ForegroundColor = ConsoleColor.Green;
												Console.WriteLine(process.ProcessName);
												Console.ForegroundColor = ConsoleColor.White;
											}

											_lastPlayer = _playerName;
											_playerName = process.ProcessName.ToLower();
											return true;
										}
										else if (showPlayers)
										{
											var reason = "";
											if (!ValidPlayers.Contains(process.ProcessName.ToLower()))
												reason = "INVALID_PLAYER";
											else if (!(EnabledClients.ContainsKey(process.ProcessName.ToLower()) &&
											           EnabledClients[process.ProcessName.ToLower()]))
												reason = "PLAYER_DISABLED";
											else if (session.QueryInterface<AudioMeterInformation>().GetPeakValue() <=
											         0)
												reason = "NO_AUDIO_DETECTED" +
												         (theresSomething ? " but detected SOMETHING" : "");
											else
												reason = "i jsut dont feel like it";
											Console.ForegroundColor = ConsoleColor.White;
											Console.Write("Detected and ignoring " + process.ProcessName + " reason: ");
											Console.ForegroundColor = ConsoleColor.Red;
											Console.WriteLine(reason);
											Console.ForegroundColor = ConsoleColor.White;
										}
									}
									catch (Exception e)
									{
										Functions.SendToDebugServer(e);
									}
								}
							}
						}
						catch (Exception e)
						{
							// i am going to ignore this because it likely has to do with the device being 
							// unsuitable and I dont know how to check
						}
					}
				}
			}

			return false;
		}

		private static void LoadSettings()
		{
			try
			{
				string[] lines = File.ReadAllLines("../../../DiscordPresenceConfig.ini");
				foreach (string line in lines)
				{
					string[] explodedLine = line.Split('=');
					string firstPortionRaw = explodedLine.Length > 0 ? explodedLine[0] : "";
					string firstPortion = firstPortionRaw.Trim().ToLower();
					string secondPortionRaw = explodedLine.Length > 1 ? explodedLine[1] : "";
					string secondPortion = secondPortionRaw.Trim().ToLower();
					if (ValidPlayers.Contains(firstPortion))
					{
						EnabledClients[firstPortionRaw] = secondPortion == "true";
						if (explodedLine.Length > 2)
							DefaultClients[firstPortionRaw] =
								new DiscordRpcClient(explodedLine[2], autoEvents: false);
					}
					else if (InverseWhatpeoplecallthisplayer.ContainsKey(firstPortion) &&
					         ValidPlayers.Contains(InverseWhatpeoplecallthisplayer[firstPortion])
					        )
					{
						EnabledClients.Add(firstPortionRaw, secondPortion == "true");
						if (explodedLine.Length > 2)
							DefaultClients[InverseWhatpeoplecallthisplayer[firstPortionRaw]] =
								new DiscordRpcClient(explodedLine[2], autoEvents: false);
					}
					else if (firstPortion == "verbose" && explodedLine.Length > 1)
					{
						ScreamAtUser = secondPortion == "true";
					}
					else if (!foundFirst && firstPortion == "first line")
					{
						foundFirst = true;
						lineData = String.Join("=", explodedLine.Skip(1).ToArray()) + lineData;
					}
					else if (!foundSecond && firstPortion == "second line")
					{
						foundSecond = true;
						lineData = lineData + "\n" + String.Join("=", explodedLine.Skip(1).ToArray());
					}
					else if (!foundButtonText && firstPortion == "button text")
					{
						foundButtonText = true;
						buttonText = String.Join("=", explodedLine.Skip(1).ToArray());
					}
					else if (!foundButtonURL && firstPortion == "button url")
					{
						foundButtonURL = true;
						buttonURL = String.Join("=", explodedLine.Skip(1).ToArray());
					}
					else if (firstPortion == "get remote artwork")
					{
						useRemoteArt = secondPortion == "true";
					}
					else if (firstPortion == "remote needs exact match")
					{
						needsExactMatch = secondPortion == "true";
					}
					else if (firstPortion == "create cache file")
					{
						createCacheFile = secondPortion == "true";
					}
					else if (firstPortion == "translate from japanese")
					{
						translateFromJapanese = secondPortion == "true";
					}
					else if (firstPortion == "paused asset")
					{
						pausedAsset = secondPortionRaw.Trim();
					}
					else if (BigAssets.ContainsKey(_largeAssetReg.Match(firstPortion).Value))
					{
						BigAssets[_largeAssetReg.Match(firstPortion).Value] = secondPortionRaw.Trim();
					}
					else if (BigAssets.ContainsKey(_smallAssetRex.Match(firstPortion).Value))
					{
						LittleAssets[_smallAssetRex.Match(firstPortion).Value] = secondPortionRaw.Trim();
					}
					else if (firstPortion == "debug missing player")
					{
						showPlayers = secondPortion == "true";
					}
				}
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
				Functions.SendToDebugServer(langHelper[LocalizableStrings.NO_SETTINGS]);
			}

			if (!foundFirst && !foundSecond)
			{
				lineData = langHelper.get(LocalizableStrings.TITLE) + ": ${title}\n" +
				           langHelper.get(LocalizableStrings.ARTIST) + ": ${artist}";
			}

			if (pausedAsset == null)
				pausedAsset = defaultPausedURL;

			try
			{
				ReadKeyingFromFile(new DirectoryInfo("../../../clientdata"));
				if (File.Exists(ExternalArtManager.cacheFileLocation))
					ReadKeyingFromFile(new FileInfo(ExternalArtManager.cacheFileLocation));
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
			}
		}

		private static void ReadKeyingFromFile(DirectoryInfo files)
		{
			foreach (DirectoryInfo dir in files.GetDirectories())
				ReadKeyingFromFile(dir);
			foreach (FileInfo file in files.GetFiles())
			{
				if (file.Name == "demo.dat" || file.Name == "cachedImages.dat")
					continue;
				ReadKeyingFromFile(file);
			}
		}

		private static void ReadKeyingFromFile(FileInfo file)
		{
			try
			{
				string[] lines = File.ReadAllLines(file.FullName);
				if (!ValidPlayers.Contains(lines[0].Split('=')[0]))
				{
					if (lines[0].Split('=')[0] == "*")
					{
						parseWildcardKeying(lines);
					}
					else
					{
						Console.Error.WriteLine("Error in file " + file.Name + " not a valid player name");
						Functions.SendNotification("Error in clientdata",
							"Error in file " + file.Name + ": " + lines[0].Split('=')[0] +
							" is not a valid player name");
						Thread.Sleep(5000);
						return;
					}
				}

				if (!lines[1].ToLower().Contains("id="))
				{
					Console.Error.WriteLine(string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER],
						file.Name));
					Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER],
						string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER], file.Name));
					Thread.Sleep(5000);
					return;
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
						if (lines[i].Trim() != "" && !warnedFile)
						{
							warnedFile = true;
							Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_SETERR_DEPREC_HEADER],
								string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_DEPREC_BODY], file.Name));
						}

						continue;
					}

					if (parsedLine.Length == 2)
						album = new Album(parsedLine[0]);
					else
						album = new Album(parsedLine[0],
							parsedLine.Skip(2).Take(parsedLine.Length - 2).ToArray());

					if (!AlbumKeyMapping.ContainsKey(album))
					{
						AlbumKeyMapping.Add(album, new Dictionary<string, string>());
					}
					else
					{
						foreach (DiscordRpcClient otherKlient in PlayersClients[lines[0].Split('=')[0]])
							if (otherKlient.ApplicationID != id && AlbumKeyMapping.ContainsKey(album))
								foundDupe |= AlbumKeyMapping[album].ContainsKey(otherKlient.ApplicationID);

						if (foundDupe)
							continue;
					}

					//if (!AlbumKeyMapping.ContainsKey(album)) ;//Console.WriteLine("Uh oh");

					if (!AlbumKeyMapping[album].ContainsKey(id))
						AlbumKeyMapping[album].Add(id, parsedLine[1]);
				}
			}
			catch (Exception e)
			{
				Functions.SendToDebugServer(e);
			}
		}

		private static void parseWildcardKeying(string[] lines)
		{
			bool useDefaults = lines[1] == "id=default";
			Dictionary<string, string> idPlayerDict = new Dictionary<string, string>();
			string id = lines[1].Split('=')[1];

			if (!useDefaults)
			{
				if (!AllClients.ContainsKey(id))
				{
					AllClients.Add(id, new DiscordRpcClient(id, autoEvents: false));
				}
			}

			foreach (string playerCandidate in lines[0].Split('=')[1] == "*"
				         ? ValidPlayers
				         : lines[0].Split('=')[1].ToLower().Split(','))
			{
				if (useDefaults)
				{
					idPlayerDict[playerCandidate] = DefaultClients[playerCandidate].ApplicationID;
					if (!PlayersClients.ContainsKey(playerCandidate))
					{
						PlayersClients[playerCandidate] = new[]
						{
							DefaultClients[playerCandidate]
						};
					}
				}
				else
				{
					PlayersClients[playerCandidate] = PlayersClients[playerCandidate].Append(AllClients[id]).ToArray();
					idPlayerDict[playerCandidate] = id;
				}
			}

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
					/*if (lines[i].Trim() != "" && !warnedFile)
					{
					    warnedFile = true;
					    SendNotification(langHelper[LocalizableStrings.NOTIF_SETERR_DEPREC_HEADER],
					        string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_DEPREC_BODY], file.Name));
					}*/

					continue;
				}

				if (parsedLine.Length == 2)
					album = new Album(parsedLine[0]);
				else
					album = new Album(parsedLine[0],
						parsedLine.Skip(2).Take(parsedLine.Length - 2).ToArray());

				if (!AlbumKeyMapping.ContainsKey(album))
				{
					AlbumKeyMapping.Add(album, new Dictionary<string, string>());
				}

				//if (!AlbumKeyMapping.ContainsKey(album)) ;//Console.WriteLine("Uh oh");

				foreach (string player in idPlayerDict.Keys)
				{
					foreach (DiscordRpcClient otherKlient in PlayersClients[player])
						if (otherKlient.ApplicationID != id && AlbumKeyMapping.ContainsKey(album))
						{
							if (AlbumKeyMapping[album].ContainsKey(otherKlient.ApplicationID))
							{
								foundDupe = true;
								break;
							}
						}

					if (foundDupe)
						continue;

					if (!AlbumKeyMapping[album].ContainsKey(idPlayerDict[player]))
						AlbumKeyMapping[album].Add(idPlayerDict[player], parsedLine[1]);
				}
			}
		}

		private static void CheckForUpdate()
		{
			UpdateTimer.Restart();
			Client.GetAsync(_GithubUrl).AsTask().ContinueWith(response =>
			{
				IHttpContent responseContent = response.Result.Content;
				foreach (string str in Regex.Split(responseContent.ToString(), ","))
					if (str.Contains("\"tag_name\":"))
					{
						UpdateVersion = str.Replace("\"tag_name\":\"", "").Replace("\"", "");
						if (ScreamAtUser && !UpdateAvailibleFlag)
							if (str.Replace("\"tag_name\":\"", "").Replace("\"", "") != Version)
								Functions.SendNotification(langHelper[LocalizableStrings.NOTIF_UPDATE_HEADER],
									string.Format(langHelper[LocalizableStrings.NOTIF_UPDATE_BODY], UpdateVersion));
						UpdateAvailibleFlag = UpdateVersion != Version;
					}
			});
		}

		/**
		 * Returns true if the loaded albums contain the title of the passed album, disregarding the artists
		 */
		private static bool HasNameNotQuite(Album query, string player)
		{
			if (PlayersClients.ContainsKey(player))
				foreach (Album alboom in AlbumKeyMapping.Keys)
					if (alboom.Name.Equals(query.Name))
						foreach (DiscordRpcClient klient in PlayersClients[player])
							if (AlbumKeyMapping[alboom].ContainsKey(klient.ApplicationID))
								return true;
			return false;
		}

		private static Album GetNameNotQuite(Album query, string player)
		{
			if (PlayersClients.ContainsKey(player))
				foreach (Album alboom in AlbumKeyMapping.Keys)
					if (alboom.Name.Equals(query.Name))
						foreach (DiscordRpcClient klient in PlayersClients[player])
							if (AlbumKeyMapping[alboom].ContainsKey(klient.ApplicationID))
								return alboom;
			return null;
		}
	}
}