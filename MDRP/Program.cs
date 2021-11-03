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
		private const string Version = "1.6.1";
		private const string Github = "https://github.com/jojo2357/Music-Discord-Rich-Presence";
		private const string Title = "Music Discord Rich Presence";
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
				{ "musicbee", new DiscordRpcClient("820837854385012766", autoEvents: false) },
				{ "apple music", new DiscordRpcClient("870047192889577544", autoEvents: false) },
				{ "spotify", new DiscordRpcClient("802222525110812725", autoEvents: false) },
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

		//ID, process name
		//process name, enabled y/n
		private static readonly Dictionary<string, bool> EnabledClients = new Dictionary<string, bool>
		{
			{ "music.ui", true }
		};

		private static readonly Dictionary<string, ConsoleColor> PlayerColors = new Dictionary<string, ConsoleColor>
		{
			{ "music.ui", ConsoleColor.Blue },
			{ "apple music", ConsoleColor.DarkRed },
			{ "spotify", ConsoleColor.DarkGreen },
			{ "musicbee", ConsoleColor.Yellow }
		};

		private static string _presenceDetails = string.Empty;

		private static readonly string[] ValidPlayers =
			{ "apple music", "music.ui", "spotify", "musicbee" };

		private static readonly string[] RequiresPipeline = { "musicbee" };

		//For use in settings
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "spotify", "Spotify Music" },
			{ "apple music", "Apple Music" },
			{ "groove", "Groove Music Player" },
			{ "music.ui", "Groove Music Player" }
		};

		private static readonly Dictionary<string, string> BigAssets = new Dictionary<string, string>
		{
			{ "musicbee", "musicbee" },
			{ "music.ui", "groove" },
			{ "spotify", "spotify" },
			{ "apple music", "applemusic" }
		};

		//might just combine these later
		private static readonly Dictionary<string, string> LittleAssets = new Dictionary<string, string>
		{
			{ "musicbee", "musicbee_small" },
			{ "music.ui", "groove_small" },
			{ "spotify", "spotify_small" },
			{ "apple music", "applemusic_small" }
		};

		private static readonly Dictionary<string, string> Whatpeoplecallthisplayer = new Dictionary<string, string>
		{
			{ "musicbee", "Music Bee" },
			{ "music.ui", "Groove Music" },
			{ "spotify", "Spotify" },
			{ "apple music", "Apple Music" }
		};

		private static readonly Dictionary<string, string> InverseWhatpeoplecallthisplayer =
			new Dictionary<string, string>
			{
				{ "musicbee", "musicbee" },
				{ "groove", "music.ui" },
				{ "spotify", "spotify" },
				{ "Apple Music", "apple music" }
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

						WrongArtistFlag = HasNameNotQuite(new Album(parsedJason.Album.Name));
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
			listener.Start();
			//Console.WriteLine("Listening for connections on {0}", url);

			// Handle requests
			Task listenTask = HandleIncomingConnections();
			listenTask.GetAwaiter().GetResult();

			// Close the listener
			listener.Close();
		}


		private static void Main(string[] args)
		{
			Thread t = new Thread(doServer);
			t.Start();
			Console.OutputEncoding = Encoding.UTF8;
			Console.Title = "Discord Rich Presence for Groove";

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
				client.Initialize();
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
					Console.WriteLine("Detected volume in " + _playerName +
					                  " but no data has been recieved from it. You may need to update the player, install a plugin, or just pause and resume the music. See more at " +
					                  Github);
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
						Console.WriteLine(currentAlbum + " and " + new Album(_currentTrack.AlbumTitle,
							_currentTrack.Artist,
							_currentTrack.AlbumArtist));
						activeClient.UpdateSmallAsset("paused", "paused");
						activeClient.Invoke();
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

						string newDetailsWithTitle = CapLength(lineData.Split('\n')[0].Replace("${artist}", (_currentTrack.Artist == "" ? "Unkown Artist" : _currentTrack.Artist)).Replace("${title}", _currentTrack.Title).Replace("${album}", _currentTrack.AlbumTitle), titleLength);
						string newStateWithArtist = CapLength(lineData.Split('\n')[1].Replace("${artist}", (_currentTrack.Artist == "" ? "Unkown Artist" : _currentTrack.Artist)).Replace("${title}", _currentTrack.Title).Replace("${album}", _currentTrack.AlbumTitle), artistLength);
						/*string newDetailsWithTitle = CapLength($"{TitleLabel}{(TitleLabel.Length > 0 ? ": " : "")}{_currentTrack.Title}", titleLength),
							newStateWithArtist =
								CapLength($"{ArtistLabel}{(ArtistLabel.Length > 0 ? ": " : "")}{(_currentTrack.Artist == "" ? "Unkown Artist" : _currentTrack.Artist)}", artistLength);*/
						if (activeClient.CurrentPresence == null || activeClient.CurrentPresence.Details != newDetailsWithTitle ||
						    activeClient.CurrentPresence.State != newStateWithArtist || _wasPlaying != _isPlaying)
						{
							presenceIsRich = AlbumKeyMapping.ContainsKey(currentAlbum) &&
							                 AlbumKeyMapping[currentAlbum]
								                 .ContainsKey(activeClient.ApplicationID);

							WrongArtistFlag = HasNameNotQuite(new Album(_currentTrack.AlbumTitle));

							if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
							    currentAlbum.Name != "")
							{
								NotifiedAlbums.Add(currentAlbum);
								if (WrongArtistFlag)
									SendNotification("Album keyed wrong",
										currentAlbum.Name +
										" is keyed for a different artist (check caps). To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
								else
									SendNotification("Album not keyed",
										currentAlbum.Name +
										" is not keyed. To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
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
							activeClient.Invoke();
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
							Details = "Failed to get track info"
						});
					Console.Write("Failed to get track info \r");
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

				WrongArtistFlag = HasNameNotQuite(new Album(lastMessage.Album.Name));

				Console.WriteLine(CapLength($"Title: {lastMessage.Title}", titleLength));
				Console.WriteLine(CapLength($"Artist: {(lastMessage.Artist == "" ? "Unkown Artist" : lastMessage.Artist)}", artistLength));
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
				Console.WriteLine(GetSmallImageText());


				activeClient.SetPresence(new RichPresence
				{
					Details = CapLength(lineData.Split('\n')[0].Replace("${artist}", (lastMessage.Artist == "" ? "Unkown Artist" : lastMessage.Artist)).Replace("${title}", lastMessage.Title).Replace("${album}", lastMessage.Album.Name), titleLength),
					State = CapLength(lineData.Split('\n')[1].Replace("${artist}", (lastMessage.Artist == "" ? "Unkown Artist" : lastMessage.Artist)).Replace("${title}", lastMessage.Title).Replace("${album}", lastMessage.Album.Name), artistLength),

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
				activeClient.Invoke();

				if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum) &&
				    currentAlbum.Name != "")
				{
					NotifiedAlbums.Add(currentAlbum);
					if (WrongArtistFlag)
						SendNotification("Album keyed wrong",
							currentAlbum.Name +
							" is keyed for a different artist (check caps). To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
					else
						SendNotification("Album not keyed",
							currentAlbum.Name +
							" is not keyed. To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
				}

				SetConsole(lastMessage.Title, lastMessage.Artist, lastMessage.Album.Name,
					lastMessage.Album);
				if (!_isPlaying) Timer.Restart();
			}
		}

		private static string GetSmallImageText()
		{
			if (_isPlaying)
				return "Using " + Aliases[_playerName];
			else
				return "paused";
		}

		private static string GetSmallImageKey()
		{
			if (_isPlaying)
				if (LittleAssets.ContainsKey(_playerName))
					return LittleAssets[_playerName];
				else
					return defaultPlayer;
			else
				return "paused";
		}

		private static string GetLargeImageKey()
		{
			if (AlbumKeyMapping.ContainsKey(currentAlbum) &&
			    AlbumKeyMapping[currentAlbum].ContainsKey(activeClient.ApplicationID) &&
			    //make sure it is not too long, this will be warned about
			    AlbumKeyMapping[currentAlbum][activeClient.ApplicationID].Length <= keyLength)
				return AlbumKeyMapping[currentAlbum][activeClient.ApplicationID];
			else
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
				return "Unknown Album";
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
				Console.WriteLine(
					"An error has occured. If the issue is severe, or you would like to know more, run the debug tool and use the printouts there. If this is severely hindering your MDRP experience, please open an issue on GitHub.");
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
				Console.WriteLine(
					"An error has occured. Please run the debug tool to get a better readout and then open an issue on github with that printout.");
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
			try
			{
				if (PlayersClients.ContainsKey(_playerName))
					foreach (DiscordRpcClient klient in PlayersClients[_playerName])
						if (album.ContainsKey(klient.ApplicationID))
							return klient;
			}
			catch (Exception e)
			{
				SendToDebugServer(e);
			}

			return DefaultClients[_playerName];
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
#if DEBUG
#else
			Console.Clear();
#endif

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

				string albumKey = AlbumKeyMapping.ContainsKey(album) &&
				                  AlbumKeyMapping[album].ContainsKey(activeClient.ApplicationID)
					? AlbumKeyMapping[album][activeClient.ApplicationID]
					: BigAssets[_playerName];
				if (albumKey.Length > keyLength)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("         The key for this album is too long. It must be " + keyLength + " characters or less");
					if (ScreamAtUser && !NotifiedAlbums.ToArray().Contains(currentAlbum))
					{
						NotifiedAlbums.Add(currentAlbum);
						SendNotification("Album key in discord too long",
							$"The key for {currentAlbum.Name} ({albumKey}) is longer than the " + keyLength + " character maximum");
					}
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" Player: ");

			Console.ForegroundColor =
				PlayerColors.ContainsKey(_playerName) ? PlayerColors[_playerName] : ConsoleColor.White;
			Console.Write(Whatpeoplecallthisplayer[_playerName]);

			if (remoteControl) Console.Write(" using special integration!");

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
			if (!_justUnknowned)
			{
				_justUnknowned = true;
#if DEBUG
#else
				Console.Clear();
#endif
				DrawPersistentHeader();
				Console.Write(
					"Detected volume in something but not showing as it is not currently supported or is disabled");
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
				SendToDebugServer(
					"DiscordPresenceConfig.ini not found! this is the settings file to enable or disable certain features");
			}

			if (!foundFirst && !foundSecond)
			{
				lineData = "Title: ${title}\nArtist: ${artist}";
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
							if (lines[i].Trim() != "" && !warnedFile)
							{
								warnedFile = true;
								SendNotification("Deprecation Notice",
									$"{file.Name} uses a deprecated keying format. Albums sould go in form Name==key==Artist");
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

						if (!AlbumKeyMapping.ContainsKey(album)) Console.WriteLine("Uh oh");

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
								SendNotification("Update Available",
									UpdateVersion + " is published on github. Go there for the latest version");
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
		private static bool HasNameNotQuite(Album query)
		{
			foreach (Album alboom in AlbumKeyMapping.Keys)
				if (alboom.Name == query.Name)
					return true;
			return false;
		}
	}
}