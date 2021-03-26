using System;
using DiscordRPC;
using DiscordRPC.Message;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Media.Control;
using IWshRuntimeLibrary;
using Microsoft.Toolkit.Uwp.Notifications;
using File = System.IO.File;

namespace GroovyRP
{
	class Program
	{
		private const string Version = "1.5.0";
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
		private static string playerName = string.Empty, lastPlayer = String.Empty;
		private static bool justcleared, justUnknowned, ScreamAtUser, presenceIsRich, WrongArtistFlag;
		private static DiscordRpcClient activeClient;
		private static Album currentAlbum = new Album("");

		private static void Main(string[] args)
		{
			Console.Title = "Discord Rich Presence for Groove";

			Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			GenerateShortcuts();

			if (args.Length > 0)
				return;

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

			GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null, lastTrack;

			try
			{
				currentTrack = GetStuff();
				lastTrack = currentTrack;
			}
			catch (Exception)
			{
			}

			bool isPlaying = IsUsingAudio(), wasPlaying;

			while (IsInitialized())
			{
				try
				{
					//limit performace impact
					Thread.Sleep(2000);
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
							lastTrack = currentTrack;
							currentTrack = GetStuff();
							if (!currentAlbum.Equals(new Album(currentTrack.AlbumTitle, currentTrack.Artist,
								    currentTrack.AlbumArtist))
							    || playerName != lastPlayer || currentTrack.Title != lastTrack.Title ||
							    wasPlaying ^ isPlaying)
							{
								currentAlbum = new Album(currentTrack.AlbumTitle, currentTrack.Artist,
									currentTrack.AlbumArtist);
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

								if (activeClient.CurrentPresence == null ||
								    activeClient.CurrentPresence.Details != ("Title: " + currentTrack.Title) ||
								    wasPlaying != isPlaying)
								{
#if DEBUG
                                Console.WriteLine("Using " + activeClient.ApplicationID + " (" +
                                                  (ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum)
                                            && GetAlbum(AlbumKeyMapping, currentAlbum)
                                                .ContainsKey(activeClient.ApplicationID) &&
                                            GetAlbum(AlbumKeyMapping, currentAlbum)[activeClient.ApplicationID].Length <= 32
                                                ? GetAlbum(AlbumKeyMapping, currentAlbum)[activeClient.ApplicationID]
                                                : BigAssets[playerName]) + ")");
#endif
									var details = $"Title: {currentTrack.Title}";
									var state =
										$"Artist: {(currentTrack.Artist == "" ? "Unkown Artist" : currentTrack.Artist)}";
									presenceIsRich = ContainsAlbum(AlbumKeyMapping.Keys.ToArray(), currentAlbum) &&
									                 GetAlbum(AlbumKeyMapping, currentAlbum)
										                 .ContainsKey(activeClient.ApplicationID);
									if (ScreamAtUser && !presenceIsRich && !NotifiedAlbums.Contains(currentAlbum))
									{
										NotifiedAlbums.Add(currentAlbum);
										SendNotification("Album not keyed",
											currentAlbum.Name +
											" is not keyed. To disable these notifications, set verbose to false in DiscordPresenceConfig.ini");
									}

									WrongArtistFlag = HasNameNotQuite(new Album(currentTrack.AlbumTitle));

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
													: currentTrack.AlbumTitle
												: "Unknown Album",
											SmallImageKey = isPlaying
												? (LittleAssets.ContainsKey(playerName)
													? LittleAssets[playerName]
													: defaultPlayer)
												: "paused",
											SmallImageText = isPlaying ? ("Using " + Aliases[playerName]) : "paused"
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
										catch (Exception)
										{
										}
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
							if (client != null && client.CurrentPresence != null)
							{
								client.ClearPresence();
								//client.Invoke();
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
					client.Initialize();
				//return false;
			}

			return true;
		}

		private static void SetConsole(string title, string artist, string albumName, Album album)
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
			Console.WriteLine(artist == "" ? "Unknown Artist" : artist);

			if (!albumName.Equals(string.Empty))
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write("  Album: ");

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(albumName);

				if ((AlbumKeyMapping.ContainsKey(album) &&
				     GetAlbum(AlbumKeyMapping, album).ContainsKey(activeClient.ApplicationID)
						? GetAlbum(AlbumKeyMapping, album)[activeClient.ApplicationID]
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
				Console.Write("Nothing Playing (probably paused)\r");
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
					}
					else if (InverseWhatpeoplecallthisplayer.ContainsKey(line.Split('=')[0].Trim().ToLower()) &&
					         ValidPlayers.Contains(InverseWhatpeoplecallthisplayer[line.Split('=')[0].Trim().ToLower()])
					)
					{
						EnabledClients.Add(line.Split('=')[0], line.Split('=')[1].Trim().ToLower() == "true");
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
									SendNotification("Deprecation Notice", $"{file.Name} uses a deprecated keying format. Albums sould go in form Name==key==Artist");
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

							if (!AlbumKeyMapping.ContainsKey(album))
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
			catch (Exception)
			{
				Console.WriteLine("Something bad happened");
			}
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
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\discordapp.ico";
			shortcut.TargetPath = Directory.GetCurrentDirectory() + "\\RunHidden.bat";
			shortcut.Save();

			shortcut = (IWshShortcut) shell.CreateShortcut(rootFolder + "\\Shortcuts\\Run MDRP Background.lnk");
			shortcut.Description = "Run MDRP";
			shortcut.IconLocation = Directory.GetCurrentDirectory() + "\\discordapp.ico";
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

		public override bool Equals(object obj)
		{
			if (obj != null && typeof(Album).IsInstanceOfType(obj) && ((Album) obj).Name == Name)
			{
				if (((Album) obj).Artists.Length == 0 || ((Album) obj).Artists[0] == "*" || Artists.Length == 0 || Artists[0] == "*")
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