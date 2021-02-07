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

namespace GroovyRP
{
    class Program
    {
        private const string version = "1.2.0";
        private const string github = "https://github.com/jojo2357/Music-Discord-Presence";
        private const string title = "Discord Rich Presence For Groove";
        //ID, client
        private static Dictionary<string, DiscordRpcClient> defaultClients = new Dictionary<string, DiscordRpcClient>{
            /*{"music.ui", new DiscordRpcClient("801209905020272681", autoEvents: false) },
            {"chrome", new DiscordRpcClient("802213652974272513", autoEvents: false) },
            {"spotify", new DiscordRpcClient("802222525110812725", autoEvents: false) }*/
        };
        //ID, client
        private static Dictionary<string, DiscordRpcClient> allClients = new Dictionary<string, DiscordRpcClient>();
        //ID, process name
        private static Dictionary<string, string> clientIDs = new Dictionary<string, string>{
            /*{"801209905020272681", "music.ui" },
            {"802213652974272513", "chrome" },
            {"802222525110812725", "spotify" }*/
        };
        //process name, enabled y/n
        private static Dictionary<string, bool> enabled_clients = new Dictionary<string, bool>
        {
            {"music.ui", true },
        };
        private static readonly Dictionary<string, ConsoleColor> PlayerColors = new Dictionary<string, ConsoleColor>
        {
            {"music.ui", ConsoleColor.Blue },
            {"chrome", ConsoleColor.Yellow },
            {"spotify", ConsoleColor.DarkGreen }
        };
        //private static readonly DiscordRpcClient chrome_client = new DiscordRpcClient("802213652974272513", autoEvents: false);
        //My head is an animal, Fever Dream (of monsters and men)
        //Sigh no more, wilder mind, babel, delta (Mumford + sons)
        //The Lumineers (the lumineers)
        //normalized album name, ID
        private static Dictionary<string, string> albums = new Dictionary<string, string>() /*{ "myheadisananimal", "feverdream", "babel", "thelumineers", "delta", "sighnomore", "wildermind" }*/;
        private static string pressenceDetails = string.Empty;
        private static readonly string[] validPlayers = new[] { "music.ui", "chrome", "spotify", /*"brave", */"new_chrome"/*, "firefox" */};
        //For use in settings
        private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>
        {
            {"chrome", "Something in Google Chrome" },
            {"spotify", "Spotify Music" },
            {"groove", "Groove Music Player" },
            {"new_chrome", "Something in Brave" },
            {"music.ui", "Groove Music Player" },
            {"brave", "Something in Brave" },
        };
        private static readonly Dictionary<string, string> big_assets = new Dictionary<string, string>
        {
            {"music.ui", "groove" },
            {"chrome", "chrome" },
            {"new_chrome", "brave_small" },
            {"brave", "brave_small" },
            {"spotify", "spotify" },
        };
        //might just combine these later
        private static readonly Dictionary<string, string> little_assets = new Dictionary<string, string>
        {
            {"music.ui", "groove_small" },
            {"chrome", "chrome_small" },
            {"new_chrome", "brave_small" },
            {"brave", "brave" },
            {"spotify", "spotify_small" },
        };
        private static readonly Dictionary<string, string> whatpeoplecallthisplayer = new Dictionary<string, string>
        {
            {"music.ui", "Groove Music" },
            {"chrome", "Google Chrome" },
            {"new_chrome", "Brave" },
            {"brave", "Brave" },
            {"spotify", "Spotify" },
        };
        private static readonly Dictionary<string, string> inverseWhatpeoplecallthisplayer = new Dictionary<string, string>
        {
            {"groove", "music.ui" },
            {"chrome", "chrome" },
            {"brave", "new_chroome" },
            {"spotify", "spotify" },
        };
        private static readonly string defaultPlayer = "groove";
        private static readonly int timeout_seconds = 60;
        private static readonly Stopwatch timer = new Stopwatch();
        private static readonly Stopwatch metaTimer = new Stopwatch();
        private static string playerName = string.Empty;
        private static bool justcleared = false;
        private static bool justUnknowned = false;

        private static void Main()
        {
            Console.Title = "Discord Rich Presence for Groove";

            LoadSettings();

            metaTimer.Start();
            timer.Start();

            foreach (DiscordRpcClient client in allClients.Values)
            {
                client.Initialize();
                client.OnError += _client_OnError;
                client.OnPresenceUpdate += _client_OnPresenceUpdate;
            }

            GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null;
            GlobalSystemMediaTransportControlsSessionMediaProperties oldTrack = null;

            try
            {
                currentTrack = GetStuff();
                oldTrack = GetStuff();
            }
            catch (Exception)
            {

            }

            bool isPlaying = IsUsingAudio();
            bool wasPlaying = false;

            while (IsInitialized())
            {
                //limit performace impact
                System.Threading.Thread.Sleep(1000);
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
                    timer.Restart();
                if (enabled_clients.ContainsKey(playerName) && enabled_clients[playerName] && (isPlaying || timer.ElapsedMilliseconds < timeout_seconds * 1000))
                {
                    DiscordRpcClient activeClient = null;
                    try
                    {
                        oldTrack = currentTrack;
                        currentTrack = GetStuff();
                        var album = currentTrack.AlbumTitle;
                        album = album.ToLower();
                        album = Regex.Replace(album, @"[^0-9a-z\-_]+", "");
                        if (albums.ContainsKey(album))
                            activeClient = allClients[albums[album]];
                        else if (defaultClients.ContainsKey(playerName))
                            activeClient = defaultClients[playerName];
                        else
                            activeClient = defaultClients["music.ui"];
                        if (activeClient.CurrentPresence == null || activeClient.CurrentPresence.Details != ("Title: " + currentTrack.Title) || wasPlaying != isPlaying)
                        {
                            var details = $"Title: {currentTrack.Title}";
                            var state = $"Artist: {currentTrack.Artist}";


                            bool hasAlbum = false;
                            foreach (string str in albums.Keys)
                                hasAlbum |= str.Equals(album);

                            activeClient.SetPresence(new RichPresence
                            {
                                Details = details,
                                State = state,
                                Assets = new Assets
                                {
                                    LargeImageKey = (albums.ContainsKey(album) ? album : (big_assets.ContainsKey(playerName) ? big_assets[playerName] : defaultPlayer)),
                                    LargeImageText = currentTrack.AlbumTitle.Length > 0 ? currentTrack.AlbumTitle : "Unknown Album",
                                    SmallImageKey = isPlaying ? (little_assets.ContainsKey(playerName) ? little_assets[playerName] : defaultPlayer) : "paused",
                                    SmallImageText = isPlaying ? ("using " + aliases[playerName]) : "paused"
                                }
                            });
                            SetConsole(currentTrack.Title, currentTrack.Artist, currentTrack.AlbumTitle, album);
                            activeClient.Invoke();

                            foreach (DiscordRpcClient client in allClients.Values)
                                if (client.CurrentPresence != null && client.ApplicationID != activeClient.ApplicationID)
                                {
#if DEBUG
                                    Console.WriteLine("Cleared " + client.ApplicationID);
#endif
                                    client.ClearPresence();
                                    client.Invoke();
                                }
                        }
#if DEBUG
                        Console.Write("" + (metaTimer.ElapsedMilliseconds) + "(" + (timer.ElapsedMilliseconds/* < timeout_seconds * 1000*/) + ") in " + playerName + '\r');
#endif
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                        if (activeClient != null) 
                            activeClient.SetPresence(new RichPresence()
                            {
                                Details = "Failed to get track info"
                            });
                        Console.Write("Failed to get track info\r");
                    }
                }
                else if (!enabled_clients.ContainsKey(playerName))
                {
                    SetUnknown();
                    foreach (DiscordRpcClient client in allClients.Values)
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
                    Console.Write("Cleared " + (metaTimer.ElapsedMilliseconds) + "\r");
#endif
                    foreach (DiscordRpcClient client in allClients.Values)
                        if (client.CurrentPresence != null)
                        {
                            client.ClearPresence();
                            client.Invoke();
                        }
                }
            }
        }

        private static bool IsInitialized()
        {
            foreach (DiscordRpcClient client in allClients.Values)
            {
                if (!client.IsInitialized)
                    return false;
            }
            return true;
        }

        private static void SetConsole(string Title, string Artist, string Album, string album)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(title);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Version: ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(version);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Github: ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(github);

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Music details:");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Title: ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Title);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" Artist: ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Artist);

            if (!Album.Equals(String.Empty))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  Album: ");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(Album);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" Player: ");

            Console.ForegroundColor = PlayerColors.ContainsKey(playerName) ? PlayerColors[playerName] : ConsoleColor.White;
            Console.WriteLine(whatpeoplecallthisplayer[playerName]);

            if (albums.ContainsKey(album))
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
                Console.Write("Detected volume in " + playerName + " but not showing as it is not currently supported");
            }
        }

        private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
        {
            if (args.Presence != null)
            {
                if (pressenceDetails != args.Presence.Details)
                {
                    pressenceDetails = allClients[args.ApplicationID].CurrentPresence?.Details;
                }
            }
            else
            {
                pressenceDetails = string.Empty;
            }
        }

        private static void _client_OnError(object sender, ErrorMessage args)
        {
            Console.WriteLine(args.Message);
        }

        //Get palying details
        private static GlobalSystemMediaTransportControlsSessionMediaProperties GetStuff()
        {
            var gsmtcsm = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult().GetCurrentSession();
            return gsmtcsm.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
        }

        private static bool IsUsingAudio()
        {
            //Music.UI is Groove. Additional options include chrome, spotify, etc
            List<Process> candidates = new List<Process>();
            foreach (string program in validPlayers)
                if (enabled_clients.ContainsKey(program) && enabled_clients[program])
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
                            if (validPlayers.Contains(process.ProcessName.ToLower()) && session.QueryInterface<AudioMeterInformation>().GetPeakValue() > 0)
                            {
                                playerName = process.ProcessName.ToLower();
                                return true;
                            }
                        }
                        catch (Exception) {
#if DEBUG
Console.WriteLine("Caught isUsingAudioException");
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
                    if (validPlayers.Contains(line.Split('=')[0].Trim().ToLower()))
                    //if (enabled_clients.Keys.Contains(line.Split('=')[0].Trim().ToLower()))
                    {
                        enabled_clients[line.Split('=')[0]] = line.Split('=')[1].Trim().ToLower() == "true";
                    }else if ((inverseWhatpeoplecallthisplayer.ContainsKey(line.Split('=')[0].Trim().ToLower()) && validPlayers.Contains(inverseWhatpeoplecallthisplayer[line.Split('=')[0].Trim().ToLower()]))){
                        enabled_clients.Add(line.Split('=')[0], line.Split('=')[1].Trim().ToLower() == "true");
                    }

                }
            }
            catch (Exception)
            {
                Console.Error.WriteLine("DiscordPresenceConfig.ini not found! this is the settings file to enable or disable certain features");
                System.Threading.Thread.Sleep(5000);
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
                        string id = "";
                        if (!validPlayers.Contains(lines[0].Split('=')[0]))
                        {
                            Console.Error.WriteLine("Error in file " + file.Name + " not a valid player name");
                            System.Threading.Thread.Sleep(5000);
                            continue;
                        }
                        if (!lines[1].ToLower().Contains("id="))
                        {
                            Console.Error.WriteLine("Error in file " + file.Name + " no id found on the second line");
                            System.Threading.Thread.Sleep(5000);
                            continue;
                        }
                        id = lines[1].Split('=')[1].Trim();
                        allClients.Add(id, new DiscordRpcClient(id, autoEvents: false));
                        if (!defaultClients.ContainsKey(lines[0].Split('=')[0]))
                            defaultClients.Add(lines[0].Split('=')[0], allClients[id]);
                        clientIDs.Add(id, lines[0].Split('=')[0]);
                        for (int i = 2; i < lines.Length; i++)
                            albums.Add(lines[i], id);
                    }
                    catch (Exception) { }
                }
               
            }
            catch (Exception) { }
            
        }
    }
}
