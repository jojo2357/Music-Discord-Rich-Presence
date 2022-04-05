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
using IWshRuntimeLibrary;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace MDRP
{
	internal partial class Program
	{
		public static LangHelper langHelper = new LangHelper();
		private const string Version = "1.7.0";
		private const string Github = "https://github.com/jojo2357/Music-Discord-Rich-Presence";
		private static readonly string Title = langHelper.get(LocalizableStrings.MDRP_FULL);
		private const int titleLength = 64;
		private const int artistLength = 64;
		private const int keyLength = 32;

		private static readonly Uri _GithubUrl =
			new Uri("https://api.github.com/repos/jojo2357/Music-Discord-Rich-Presence/releases/latest");

		//Player Name, client
		private static readonly Dictionary<string, DiscordRpcClient> DefaultClients =
			new Dictionary<string, DiscordRpcClient>
			{
				{ "music.ui", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "microsoft.media.player", new DiscordRpcClient("807774172574253056", autoEvents: false) },
				{ "musicbee", new DiscordRpcClient("820837854385012766", autoEvents: false) },
				{ "apple music", new DiscordRpcClient("870047192889577544", autoEvents: false) },
				{ "spotify", new DiscordRpcClient("802222525110812725", autoEvents: false) },
				{ "tidalplayer", new DiscordRpcClient("922625678271197215", autoEvents: false) },
				{ "wavelink", new DiscordRpcClient("927328178618376212", autoEvents: false) },
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
			{ "apple music", ConsoleColor.DarkRed },
			{ "spotify", ConsoleColor.DarkGreen },
			{ "musicbee", ConsoleColor.Yellow },
			{ "tidalplayer", ConsoleColor.Gray },
			{ "wavelink", ConsoleColor.DarkBlue },
		};

		private static string _presenceDetails = string.Empty;

		private static readonly string[] ValidPlayers =
			{ "apple music", "music.ui", "spotify", "musicbee", "microsoft.media.player", "tidalplayer", "wavelink" };

		private static readonly string[] RequiresPipeline = { "musicbee" };

		//For use in settings
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "spotify", "Spotify Music" },
			{ "apple music", "Apple Music" },
			{ "groove", "Groove Music Player" },
			{ "microsoft.media.player", "Windows Media Player" },
			{ "music.ui", "Groove Music Player" },
			{ "tidalplayer", "Tidal Music" },
			{ "wavelink", "Wave Link" }
		};

		private static readonly Dictionary<string, string> BigAssets = new Dictionary<string, string>
		{
			{ "musicbee", "musicbee" },
			{ "music.ui", "groove" },
			{ "microsoft.media.player", "groove" },
			{ "spotify", "spotify" },
			{ "apple music", "applemusic" },
			{ "tidalplayer", "tidalplayer" },
			{ "wavelink", "wavelink" },
		};

		//might just combine these later
		private static readonly Dictionary<string, string> LittleAssets = new Dictionary<string, string>
		{
			{ "musicbee", "musicbee_small" },
			{ "music.ui", "groove_small" },
			{ "microsoft.media.player", "groove_small" },
			{ "spotify", "spotify_small" },
			{ "apple music", "applemusic_small" },
			{ "tidalplayer", "tidal_small" },
			{ "wavelink", "wavelink_small" },
		};

		private static readonly Dictionary<string, string> Whatpeoplecallthisplayer = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "music.ui", "Groove Music" },
			{ "microsoft.media.player", "Windows Media Player" },
			{ "spotify", "Spotify" },
			{ "apple music", "Apple Music" },
			{ "tidalplayer", "Tidal Music" },
			{ "wavelink", "Wave Link" }
		};

		private static readonly Dictionary<string, string> InverseWhatpeoplecallthisplayer =
			new Dictionary<string, string>
			{
				{ "musicbee", "musicbee" },
				{ "groove", "music.ui" },
				{ "Microsoft.Media.Player", "microsoft.media.player" },
				{ "spotify", "spotify" },
				{ "Apple Music", "apple music" },
				{ "Tidal Music", "tidalplayer" },
				{ "Wave Link", "wavelink" }
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
			WrongArtistFlag,
			UpdateAvailibleFlag,
			NotifiedRequiredPipeline;

		private static DiscordRpcClient activeClient;
		private static Album currentAlbum = new Album("");
		private static readonly HttpClient Client = new HttpClient();
		private const long updateCheckInterval = 36000000;
		private static string UpdateVersion;

		public static HttpListener listener;
		public static string url = "http://localhost:2357/";

		private static bool useRemoteArt = false;
		public static bool remoteControl;
		public static long resignRemoteControlAt;

		private static readonly Queue<JsonResponse> _PendingMessages = new Queue<JsonResponse>();
		private static bool spawnedFromApplication;
		private static bool _isPlaying;
		private static bool _wasPlaying;
		private static GlobalSystemMediaTransportControlsSessionMediaProperties _currentTrack = null;
		private static GlobalSystemMediaTransportControlsSessionMediaProperties _lastTrack = null;

		private static string lineData = "";
		private static bool foundFirst = false, foundSecond = false;
		private static bool foundImageRemotely = false;

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
								response = "{response:\"no key\"}";
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
					SendToDebugServer(e);
					SendToDebugServer("failure to parse incoming json: \n" + text + " (" + decodedText + ")");
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

			return false;
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

			GenerateShortcuts();

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
				_currentTrack = GetStuff();
				_lastTrack = _currentTrack;
			}
			catch (Exception e)
			{
				SendToDebugServer(e);
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
					SendToDebugServer(e);
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
				SendToDebugServer(e);
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
					DrawPersistentHeader();
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
					_currentTrack = GetStuff();
					if (_currentTrack == null)
						return;
					if (_wasPlaying && !_isPlaying)
					{
						Console.WriteLine(currentAlbum + " " + langHelper[LocalizableStrings.AND] + " " + new Album(_currentTrack.AlbumTitle,
							_currentTrack.Artist,
							_currentTrack.AlbumArtist));
						activeClient.UpdateSmallAsset("paused", "paused");
						InvokeActiveClient();
						SetConsole(_lastTrack.Title, _lastTrack.Artist, _lastTrack.AlbumTitle,
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
						GetClient();

						foreach (DiscordRpcClient client in AllClients.Values)
							if (client.CurrentPresence != null &&
							    client.ApplicationID != activeClient.ApplicationID)
								try
								{
									ClearAPresence(client);
								}
								catch (Exception e)
								{
									SendToDebugServer(e);
								}

						string newDetailsWithTitle = CapLength(lineData.Split('\n')[0].Replace("${artist}", (_currentTrack.Artist == "" ? langHelper[LocalizableStrings.UNKNOWN_ARTIST] : _currentTrack.Artist)).Replace("${title}", _currentTrack.Title).Replace("${album}", _currentTrack.AlbumTitle), titleLength);
						string newStateWithArtist = CapLength(lineData.Split('\n')[1].Replace("${artist}", (_currentTrack.Artist == "" ? langHelper[LocalizableStrings.UNKNOWN_ARTIST] : _currentTrack.Artist)).Replace("${title}", _currentTrack.Title).Replace("${album}", _currentTrack.AlbumTitle), artistLength);
						if (activeClient.CurrentPresence == null || activeClient.CurrentPresence.Details != newDetailsWithTitle ||
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
								if (WrongArtistFlag)
									SendNotification(langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_HEADER],
										currentAlbum.Name +
										" " + langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_BODY]);
								else
									SendNotification(langHelper[LocalizableStrings.NOTIF_UNKEYED_HEADER],
										currentAlbum.Name + " " + langHelper[LocalizableStrings.NOTIF_UNKEYED_BODY]);
							}

							activeClient.SetPresence(new RichPresence
							{
								Details = newDetailsWithTitle,
								State = newStateWithArtist,
								Assets = new Assets
								{
									LargeImageKey = GetLargeImageKey(),
									LargeImageText = GetLargeImageText(_currentTrack.AlbumTitle),
									SmallImageKey = GetSmallImageKey(),
									SmallImageText = GetSmallImageText()
								}
							});
							SetConsole(_currentTrack.Title, _currentTrack.Artist, _currentTrack.AlbumTitle,
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
					SendToDebugServer(e);
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
							ClearAPresence(client);
							/*client.ClearPresence();
							client.Invoke();*/
						}
						catch (Exception e)
						{
							SendToDebugServer(e);
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
							SendToDebugServer(e);
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
					resignRemoteControlAt = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds + 60000;
				else
					resignRemoteControlAt = 0;

				presenceIsRich = AlbumKeyMapping.ContainsKey(currentAlbum) &&
				                 AlbumKeyMapping[currentAlbum].ContainsKey(activeClient.ApplicationID);

				WrongArtistFlag = HasNameNotQuite(new Album(lastMessage.Album.Name), _playerName);

				/*Console.WriteLine(CapLength($"{langHelper[LocalizableStrings.TITLE]}: {lastMessage.Title}", titleLength));
				Console.WriteLine(CapLength($"{langHelper[LocalizableStrings.ARTIST]}: {(lastMessage.Artist == "" ? langHelper[LocalizableStrings.UNKNOWN_ARTIST] : lastMessage.Artist)}", artistLength));
				Console.WriteLine(_isPlaying
					? new Timestamps
					{
						End = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(lastMessage.TimeStamp))
							.DateTime
					}
					: null);
				Console.WriteLine(GetLargeImageKey());
				Console.WriteLine(GetLargeImageText(lastMessage.Album.Name));
				Console.WriteLine(GetSmallImageKey());
				Console.WriteLine(GetSmallImageText());*/

				activeClient.SetPresence(new RichPresence
				{
					Details = CapLength(lineData.Split('\n')[0].Replace("${artist}", (lastMessage.Artist == "" ? langHelper[LocalizableStrings.UNKNOWN_ARTIST] : lastMessage.Artist)).Replace("${title}", lastMessage.Title).Replace("${album}", lastMessage.Album.Name), titleLength),
					State = CapLength(lineData.Split('\n')[1].Replace("${artist}", (lastMessage.Artist == "" ? langHelper[LocalizableStrings.UNKNOWN_ARTIST] : lastMessage.Artist)).Replace("${title}", lastMessage.Title).Replace("${album}", lastMessage.Album.Name), artistLength),

					//Details = CapLength($"{TitleLabel}{(TitleLabel.Length > 0 ? ": " : "")}{lastMessage.Title}", titleLength),
					//State = CapLength($"{ArtistLabel}{(ArtistLabel.Length > 0 ? ": " : "")}{(lastMessage.Artist == "" ? "Unkown Artist" : lastMessage.Artist)}", artistLength),
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
						LargeImageText = GetLargeImageText(lastMessage.Album.Name),
						SmallImageKey = GetSmallImageKey(),
						SmallImageText = GetSmallImageText()
					}
				});
				InvokeActiveClient();

				if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
				    currentAlbum.Name != "")
				{
					NotifiedAlbums.Add(currentAlbum);
					if (WrongArtistFlag)
						SendNotification(langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_HEADER],
							currentAlbum.Name + " " + langHelper[LocalizableStrings.NOTIF_KEYED_WRONG_BODY]);
					else
						SendNotification(langHelper[LocalizableStrings.NOTIF_UNKEYED_HEADER],
							currentAlbum.Name + " " + langHelper[LocalizableStrings.NOTIF_UNKEYED_BODY]);
				}

				SetConsole(lastMessage.Title, lastMessage.Artist, lastMessage.Album.Name,
					lastMessage.Album);
				if (!_isPlaying) Timer.Restart();
			}
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
				return langHelper[LocalizableStrings.PAUSED];
		}

		private static string GetLargeImageKey()
		{
			foundImageRemotely = false;
			if (AlbumKeyMapping.ContainsKey(currentAlbum) &&
			    AlbumKeyMapping[currentAlbum].ContainsKey(activeClient.ApplicationID))
			{
				//make sure it is not too long, this will be warned about
				if (Uri.IsWellFormedUriString(AlbumKeyMapping[currentAlbum][activeClient.ApplicationID], UriKind.Absolute))
				{
					return AlbumKeyMapping[currentAlbum][activeClient.ApplicationID];
				}

				if (AlbumKeyMapping[currentAlbum][activeClient.ApplicationID].Length <= keyLength)
				{
					return AlbumKeyMapping[currentAlbum][activeClient.ApplicationID];
				}
			}

			if (useRemoteArt)
			{
				string res = mngr.AlbumLookup(currentAlbum).Result;
				if (res != String.Empty)
				{
					foundImageRemotely = true;
					return res;
				}
			}

			return BigAssets[_playerName];
		}

		private static string GetLargeImageText(string albumName)
		{
			if (albumName.Length > 0)
				if (albumName.Length <= 2)
					return "_" + albumName + "_";
				else
					return CapLength(albumName, 128);
			else
				return langHelper[LocalizableStrings.UNKNOWN_ALBUM];
		}

		private static void UnsetAllPresences()
		{
			foreach (DiscordRpcClient client in AllClients.Values)
				if (client.CurrentPresence != null)
					ClearAPresence(client);
		}

		private static void SendToDebugServer(Exception exception)
		{
#if DEBUG
			Console.WriteLine(exception.Message);
			Console.WriteLine(exception.StackTrace);
			Console.WriteLine("Something unexpected has occured");
#else
			Uri debugUri = new Uri("http://localhost:7532/");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(debugUri);
			request.Method = "POST";
			request.ContentType = "text/json";
			string urlEncoded = Uri.EscapeUriString(exception.ToString());
			byte[] arr = Encoding.UTF8.GetBytes(urlEncoded);
			try
			{
				Stream rs = request.GetRequestStream();
				rs.Write(arr, 0, arr.Length);
				request.GetResponse().Close();
			}
			catch (Exception)
			{
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine(langHelper[LocalizableStrings.ERROR_OCCURRED]);
				Console.ForegroundColor = ConsoleColor.White;
			}
#endif
		}

		private static void SendToDebugServer(string message)
		{
#if DEBUG
			Console.WriteLine(message);
#else
			Uri url = new Uri("http://localhost:7532/");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "text/json";
			string urlEncoded = Uri.EscapeUriString(message);
			byte[] arr = Encoding.UTF8.GetBytes(urlEncoded);
			try
			{
				Stream rs = request.GetRequestStream();
				rs.Write(arr, 0, arr.Length);
				request.GetResponse().Close();
			}
			catch (Exception)
			{
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine(langHelper[LocalizableStrings.REQUEST_DEBUG_TOOL]);
				Console.ForegroundColor = ConsoleColor.White;
			}
#endif
		}

		private static void GetClient(Album album, string playerName)
		{
			if (AlbumKeyMapping.ContainsKey(album))
				activeClient = GetBestClient(AlbumKeyMapping[album]);
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

		private static DiscordRpcClient GetBestClient(Dictionary<string, string> album)
		{
			string playerName = _playerName;
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
				SendToDebugServer(e);
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

			DrawPersistentHeader();

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
							SendNotification(langHelper[LocalizableStrings.NOTIF_KEY_TOO_LONG_HEADER],
								string.Format(langHelper[LocalizableStrings.NOTIF_KEY_TOO_LONG_BODY], currentAlbum.Name,
									albumKey, keyLength));
						}
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(new string(' ', totalBufLen - langHelper.get(LocalizableStrings.PLAYER).Length + 1) + langHelper.get(LocalizableStrings.PLAYER) + ": ");

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
				Console.WriteLine("\n" + langHelper.get(LocalizableStrings.KEYED_WRONG) + " Got \"" + album.GetArtistString() + "\" but expected \"" + Program.GetNameNotQuite(album, _playerName).GetArtistString() + "\"");
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
					Console.WriteLine("\n" + "Image found remotely!");
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
				DrawPersistentHeader();
				Console.Write(langHelper[LocalizableStrings.NOTHING_PLAYING] + "\r");
			}
		}

		private static void DrawPersistentHeader()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(langHelper.get(LocalizableStrings.VERSION) + ": ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(Version);

			if (UpdateAvailibleFlag)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(string.Format(" " + langHelper[LocalizableStrings.NEW_UPDATE], UpdateVersion));
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
			if (!_justUnknowned)
			{
				_justUnknowned = true;
#if DEBUG
#else
				Console.Clear();
#endif
				DrawPersistentHeader();
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
			SendToDebugServer(args.ToString());
			Console.WriteLine(args.Message);
		}

		//Get palying details
		private static GlobalSystemMediaTransportControlsSessionMediaProperties GetStuff()
		{
			try
			{
				GlobalSystemMediaTransportControlsSession gsmtcsm = GlobalSystemMediaTransportControlsSessionManager
					.RequestAsync().GetAwaiter().GetResult()
					.GetCurrentSession();
				return gsmtcsm.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
			}
			catch (Exception)
			{
				//happens all the time but trying again works so dw ab it
				//SendToDebugServer("Something went wrong getting playing stuff.");
				return null;
			}
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
				using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
				{
					using (MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
					{
						sessionManager = AudioSessionManager2.FromMMDevice(device);
					}
				}

				using (AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator())
				{
					foreach (AudioSessionControl session in sessionEnumerator)
					{
						Process process = session.QueryInterface<AudioSessionControl2>().Process;
						try
						{
							if (ValidPlayers.Contains(process.ProcessName.ToLower()) &&
							    EnabledClients.ContainsKey(process.ProcessName.ToLower()) &&
							    EnabledClients[process.ProcessName.ToLower()] &&
							    session.QueryInterface<AudioMeterInformation>().GetPeakValue() > 0)
							{
								_lastPlayer = _playerName;
								_playerName = process.ProcessName.ToLower();
								return true;
							}
						}
						catch (Exception e)
						{
							SendToDebugServer(e);
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
			//todo make a notif system for mb
			if (!spawnedFromApplication)
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
					if (ValidPlayers.Contains(line.Split('=')[0].Trim().ToLower()))
					{
						EnabledClients[line.Split('=')[0]] = line.Split('=')[1].Trim().ToLower() == "true";
						if (line.Split('=').Length > 2)
							DefaultClients[line.Split('=')[0]] =
								new DiscordRpcClient(line.Split('=')[2], autoEvents: false);
					}
					else if (InverseWhatpeoplecallthisplayer.ContainsKey(line.Split('=')[0].Trim().ToLower()) &&
					         ValidPlayers.Contains(InverseWhatpeoplecallthisplayer[line.Split('=')[0].Trim().ToLower()])
					        )
					{
						EnabledClients.Add(line.Split('=')[0], line.Split('=')[1].Trim().ToLower() == "true");
						if (line.Split('=').Length > 2)
							DefaultClients[InverseWhatpeoplecallthisplayer[line.Split('=')[0]]] =
								new DiscordRpcClient(line.Split('=')[2], autoEvents: false);
					}
					else if (line.Split('=')[0].Trim().ToLower() == "verbose" && line.Split('=').Length > 1)
					{
						ScreamAtUser = line.Split('=')[1].Trim().ToLower() == "true";
					}
					else if (!foundFirst && line.Split('=')[0].Trim().ToLower() == "first line")
					{
						foundFirst = true;
						lineData = String.Join("=", line.Split('=').Skip(1)) + lineData;
					}
					else if (!foundSecond && line.Split('=')[0].Trim().ToLower() == "second line")
					{
						foundSecond = true;
						lineData = lineData + "\n" + String.Join("=", line.Split('=').Skip(1));
					} else if (line.Split('=')[0].Trim().ToLower() == "get remote artwork")
					{
						useRemoteArt = line.Split('=')[0].Trim().ToLower() == "true";
					}
				/*else if (line.Split('=')[0].Trim().ToLower() == "artist label")
				{
					ArtistLabel = line.Split('=')[1];
				}
				else if (line.Split('=')[0].Trim().ToLower() == "title label")
				{
					TitleLabel = line.Split('=')[1];
				}*/
			}
			catch (Exception e)
			{
				SendToDebugServer(e);
				SendToDebugServer(langHelper[LocalizableStrings.NO_SETTINGS]);
			}

			if (!foundFirst && !foundSecond)
			{
				lineData = langHelper.get(LocalizableStrings.TITLE) + ": ${title}\n" + langHelper.get(LocalizableStrings.ARTIST) + ": ${artist}";
			}

			try
			{
				ReadKeyingFromFile(new DirectoryInfo("../../../clientdata"));
			}
			catch (Exception e)
			{
				SendToDebugServer(e);
			}
		}

		private static void ReadKeyingFromFile(DirectoryInfo files)
		{
			foreach (DirectoryInfo dir in files.GetDirectories())
				ReadKeyingFromFile(dir);
			foreach (FileInfo file in files.GetFiles())
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
						Console.Error.WriteLine(string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER], file.Name));
						SendNotification(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER],
							string.Format(langHelper[LocalizableStrings.NOTIF_SETERR_NO_ID_HEADER], file.Name));
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
							if (lines[i].Trim() != "" && !warnedFile)
							{
								warnedFile = true;
								SendNotification(langHelper[LocalizableStrings.NOTIF_SETERR_DEPREC_HEADER],
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
					SendToDebugServer(e);
				}
			}
		}

		private static void ClearAPresence(DiscordRpcClient client)
		{
			try
			{
				client.ClearPresence();
			}
			catch (NullReferenceException e)
			{
				Console.WriteLine("Excepted " + e);
				client.SetPresence(null);
				client.Invoke();
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
								SendNotification(langHelper[LocalizableStrings.NOTIF_UPDATE_HEADER],
									string.Format(langHelper[LocalizableStrings.NOTIF_UPDATE_BODY], UpdateVersion));
						UpdateAvailibleFlag = UpdateVersion != Version;
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

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Run MDRP Windowed.lnk");
			shortcut.Description = "Run MDRP";
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\MDRP.exe";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\RunHidden.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Run MDRP Background.lnk");
			shortcut.Description = "Run MDRP";
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\MDRP.exe";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\RunHidden.vbs";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With MusicBee.lnk");
			shortcut.Description = "Link With MusicBee";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithMusicBee.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With Groove.lnk");
			shortcut.Description = "Link With Groove";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithGroove.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Link With Spotify.lnk");
			shortcut.Description = "Link With Spotify";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\LinkWithSpotify.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink MusicBee.lnk");
			shortcut.Description = "Unlink With MusicBee";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromMusicBee.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink Spotify.lnk");
			shortcut.Description = "Unlink With Spotify";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromSpotify.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Unlink Groove.lnk");
			shortcut.Description = "Unlink With Groove";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\UnlinkFromGroove.bat";
			shortcut.Save();

			shortcut = (IWshShortcut)shell.CreateShortcut(rootFolder + "\\Shortcuts\\Kill Hidden.lnk");
			shortcut.Description = "Kills MDRP";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\KillHidden.vbs";
			shortcut.Save();
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
			/*foreach (Album alboom in AlbumKeyMapping.Keys)
				if (alboom.Name.Equals(query.Name))
					return true;*/
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
			/*foreach (Album alboom in AlbumKeyMapping.Keys)
				if (alboom.Name.Equals(query.Name))
					return alboom;*/
			return null;
		}
	}
}