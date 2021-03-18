using System;
using DiscordRPC;
using DiscordRPC.Message;
using Windows.Media.Control;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace GroovyRP
{
	class Program
	{
		private const string Version = "1.4.1";
		private const string Github = "https://github.com/jojo2357/Music-Discord-Rich-Presence";
		private const string Title = "Discord Rich Presence For Groove";

		//Player Name, client
		private static readonly Dictionary<string, DiscordRpcClient> DefaultClients =
			new Dictionary<string, DiscordRpcClient>
			{
				{"music.ui", new DiscordRpcClient("807774172574253056", autoEvents: false)},
				{"musicbee", new DiscordRpcClient("820837854385012766", autoEvents: false)},
				{"spotify", new DiscordRpcClient("802222525110812725", autoEvents: false)},
				{"chrome", new DiscordRpcClient("802213652974272513", autoEvents: false)},
				{"", new DiscordRpcClient("821398156905283585", autoEvents: false)},
			};

		//ID, client
		private static readonly Dictionary<string, DiscordRpcClient> AllClients =
			new Dictionary<string, DiscordRpcClient>();

		//Playername, client
		private static readonly Dictionary<string, DiscordRpcClient[]> PlayersClients =
			new Dictionary<string, DiscordRpcClient[]>();

		//Album, (id, key)
		private static readonly Dictionary<string, Dictionary<string, string>> AlbumKeyMapping =
			new Dictionary<string, Dictionary<string, string>>();

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
			{"spotify", ConsoleColor.DarkGreen},
			{"musicbee", ConsoleColor.Yellow}
		};

		private static string _presenceDetails = string.Empty;

		private static readonly string[] ValidPlayers = new[]
			{"music.ui", "chrome", "spotify", /*"brave", */"new_chrome", "musicbee" /*, "firefox" */};

		//For use in settings
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
		{
			{"musicbee", "Music Bee"},
			{"chrome", "Something in Google Chrome"},
			{"spotify", "Spotify Music"},
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
		};

		private static readonly Dictionary<string, string> Whatpeoplecallthisplayer = new Dictionary<string, string>
		{
			{"musicbee", "Music Bee"},
			{"music.ui", "Groove Music"},
			{"chrome", "Google Chrome"},
			{"new_chrome", "Brave"},
			{"brave", "Brave"},
			{"spotify", "Spotify"},
		};

		private static readonly Dictionary<string, string> InverseWhatpeoplecallthisplayer =
			new Dictionary<string, string>
			{
				{"musicbee", "musicbee"},
				{"groove", "music.ui"},
				{"chrome", "chrome"},
				{"brave", "new_chrome"},
				{"spotify", "spotify"},
			};

		private static readonly string defaultPlayer = "groove";
		private static readonly int timeout_seconds = 60;
		private static readonly Stopwatch Timer = new Stopwatch();
		private static readonly Stopwatch MetaTimer = new Stopwatch();
		private static string playerName = string.Empty;
		private static bool justcleared, justUnknowned, presenceIsRich = false;
		private static DiscordRpcClient activeClient;

		private static void Main()
		{
			Console.Title = "Discord Rich Presence for Groove";

			Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			foreach (DiscordRpcClient client in DefaultClients.Values)
			{
				AllClients.Add(client.ApplicationID, client);
			}

			LoadSettings();

			MetaTimer.Start();
			Timer.Start();

			foreach (DiscordRpcClient client in AllClients.Values)
			{
				client.Initialize();
				client.OnError += _client_OnError;
				client.OnPresenceUpdate += _client_OnPresenceUpdate;
			}

			GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack;

			try
			{
				currentTrack = GetStuff();
				GetStuff();
			}
			catch (Exception)
			{
			}

			bool isPlaying = IsUsingAudio(), wasPlaying;

			while (IsInitialized())
			{
				//limit performace impact
				Thread.Sleep(1000);
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
				if (EnabledClients.ContainsKey(playerName) && EnabledClients[playerName] &&
				    (isPlaying || Timer.ElapsedMilliseconds < timeout_seconds * 1000))
				{
					activeClient = null;
					try
					{
						currentTrack = GetStuff();
						var album = currentTrack.AlbumTitle;
						if (AlbumKeyMapping.ContainsKey(album))
						{
							activeClient = GetBestClient(AlbumKeyMapping[album]);
						}
						else if (AlbumKeyMapping.ContainsKey(Regex.Replace(album.ToLower(), @"[^0-9a-z\-_]+", "")))
						{
							activeClient =
								GetBestClient(AlbumKeyMapping[Regex.Replace(album.ToLower(), @"[^0-9a-z\-_]+", "")]);
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

#if DEBUG
						Console.WriteLine("Using " + activeClient.ApplicationID + " (" +
						                  (AlbumKeyMapping.ContainsKey(album) && AlbumKeyMapping[album]
							                   .ContainsKey(activeClient.ApplicationID) &&
						                   AlbumKeyMapping[album][activeClient.ApplicationID].Length <= 32
							                  ? AlbumKeyMapping[album][activeClient.ApplicationID]
							                  : BigAssets[playerName]) + ")");
#endif

						if (activeClient.CurrentPresence == null ||
						    activeClient.CurrentPresence.Details != ("Title: " + currentTrack.Title) ||
						    wasPlaying != isPlaying)
						{
							var details = $"Title: {currentTrack.Title}";
							var state = $"Artist: {currentTrack.Artist}";
							presenceIsRich = AlbumKeyMapping.ContainsKey(album) &&
							                 AlbumKeyMapping[album].ContainsKey(activeClient.ApplicationID);
							activeClient.SetPresence(new RichPresence
							{
								Details = details,
								State = state,
								Assets = new Assets
								{
									LargeImageKey =
										AlbumKeyMapping.ContainsKey(album) && AlbumKeyMapping[album]
											.ContainsKey(activeClient.ApplicationID) &&
										AlbumKeyMapping[album][activeClient.ApplicationID].Length <= 32
											? AlbumKeyMapping[album][activeClient.ApplicationID]
											: BigAssets[playerName],
									LargeImageText = currentTrack.AlbumTitle.Length > 0
										? currentTrack.AlbumTitle
										: "Unknown Album",
									SmallImageKey = isPlaying
										? (LittleAssets.ContainsKey(playerName)
											? LittleAssets[playerName]
											: defaultPlayer)
										: "paused",
									SmallImageText = isPlaying ? ("Using " + Aliases[playerName]) : "paused"
								}
							});
							SetConsole(currentTrack.Title, currentTrack.Artist, currentTrack.AlbumTitle, album);
							activeClient.Invoke();

							foreach (DiscordRpcClient client in AllClients.Values)
								if (client.CurrentPresence != null &&
								    client.ApplicationID != activeClient.ApplicationID)
								{
#if DEBUG
									Console.WriteLine("Cleared " + client.ApplicationID);
#endif
									client.ClearPresence();
									client.Invoke();
								}
						}
#if DEBUG
						Console.Write("" + (MetaTimer.ElapsedMilliseconds) + "(" +
						              (Timer.ElapsedMilliseconds /* < timeout_seconds * 1000*/) + ") in " + playerName +
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
						if (client.CurrentPresence != null)
						{
							client.ClearPresence();
							client.Invoke();
						}
				}
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

		private static bool IsInitialized()
		{
			foreach (DiscordRpcClient client in AllClients.Values)
			{
				if (!client.IsInitialized)
					return false;
			}

			return true;
		}

		private static void SetConsole(string title, string artist, string albumName, string album)
		{
			Console.Clear();

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Version: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(Version);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Github: ");

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(Github);

			Console.WriteLine();

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Music details:");

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("  Title: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" Artist: ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(artist);

			if (!albumName.Equals(string.Empty))
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write("  Album: ");

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(albumName);
				
				if ((AlbumKeyMapping.ContainsKey(album) && AlbumKeyMapping[album].ContainsKey(activeClient.ApplicationID)
						? AlbumKeyMapping[album][activeClient.ApplicationID]
						: BigAssets[playerName])
					.Length > 32)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("The key for this album is too long. It must be 32 characters or less");
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" Player: ");

			Console.ForegroundColor =
				PlayerColors.ContainsKey(playerName) ? PlayerColors[playerName] : ConsoleColor.White;
			Console.WriteLine(Whatpeoplecallthisplayer[playerName]);

			if (presenceIsRich)
			{
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine("\nThis is a good one, check ur DRP ;)");
				Console.ForegroundColor = ConsoleColor.White;
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
				Console.Write("Nothing Playing\r");
			}
		}

		private static void SetUnknown()
		{
			if (!justUnknowned)
			{
				justUnknowned = true;
				Console.Clear();
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
					}
					else if (InverseWhatpeoplecallthisplayer.ContainsKey(line.Split('=')[0].Trim().ToLower()) &&
					         ValidPlayers.Contains(InverseWhatpeoplecallthisplayer[line.Split('=')[0].Trim().ToLower()])
					)
					{
						EnabledClients.Add(line.Split('=')[0], line.Split('=')[1].Trim().ToLower() == "true");
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
				foreach (var file in new DirectoryInfo("../../../clientdata").GetFiles())
				{
					if (file.Name == "demo.dat")
						continue;
					try
					{
						string[] lines = File.ReadAllLines(file.FullName);
						if (!ValidPlayers.Contains(lines[0].Split('=')[0]))
						{
							Console.Error.WriteLine("Error in file " + file.Name + " not a valid player name");
							Thread.Sleep(5000);
							continue;
						}

						if (!lines[1].ToLower().Contains("id="))
						{
							Console.Error.WriteLine("Error in file " + file.Name + " no id found on the second line");
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

						for (int i = 2; i < lines.Length; i++)
						{
							if (lines[i].Contains("=="))
							{
								if (!AlbumKeyMapping.ContainsKey(Regex.Split(lines[i], @"==")[0]))
									AlbumKeyMapping.Add(Regex.Split(lines[i], @"==")[0],
										new Dictionary<string, string>());
								AlbumKeyMapping[Regex.Split(lines[i], @"==")[0]]
									.Add(id, Regex.Split(lines[i], @"==")[1]);
							}
							else if (lines[i].Contains('='))
							{
								if (!AlbumKeyMapping.ContainsKey(Regex.Split(lines[i], "=")[0]))
									AlbumKeyMapping.Add(Regex.Split(lines[i], "=")[0],
										new Dictionary<string, string>());
								AlbumKeyMapping[Regex.Split(lines[i], "=")[0]].Add(id, Regex.Split(lines[i], "=")[1]);
							}
							else
							{
								if (!AlbumKeyMapping.ContainsKey(lines[i]))
									AlbumKeyMapping.Add(lines[i], new Dictionary<string, string>());
								AlbumKeyMapping[lines[i]].Add(id, lines[i]);
							}
						}
					}
					catch (Exception e)
					{
						Console.Error.WriteLine(e);
						Thread.Sleep(1000);
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}