using System;
using DiscordRPC;
using DiscordRPC.Message;
using Windows.Media.Control;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using System.Linq;
using System.Collections.Generic;

namespace GroovyRP
{
    class Program
    {
        private const string appDetails = "Discord Rich Presence For Groovy\nGithub: https://github.com/jojo2357/Music-Discord-Presence";
        private static readonly DiscordRpcClient _client = new DiscordRpcClient("801209905020272681", autoEvents: false);
        //My head is an animal, Fever Dream (of monsters and men)
        //Sigh no more, wilder mind, babel, delta (Mumford + sons)
        //The Lumineers (the lumineers)
        private static readonly string[] albums = new[] { "myheadisananimal", "feverdream", "babel", "thelumineers", "delta", "sighnomore", "wildermind" };
        private static string pressenceDetails = string.Empty;
        private static readonly string[] validPlayers = new[] { "music.ui", "chrome", "spotify", "brave", "new_chrome", "firefox" };
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
        private static readonly string defaultPlayer = "groove";
        private static readonly int timeout_seconds = 60;
        private static readonly Stopwatch timer = new Stopwatch();
        private static readonly Stopwatch metaTimer = new Stopwatch();
        private static string playerName = string.Empty;
        private static bool justcleared = false;

        private static void Main()
        {
            metaTimer.Start();
            timer.Start();
            _client.Initialize();
            _client.OnError += _client_OnError;
            _client.OnPresenceUpdate += _client_OnPresenceUpdate;

            GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null;
            GlobalSystemMediaTransportControlsSessionMediaProperties oldTrack = null;

            try
            {
                currentTrack = GetStuff();
                oldTrack = GetStuff();
            }catch (Exception)
            {
            
            }

            bool isPlaying = IsUsingAudio();
            bool wasPlaying = false;

            while (_client.IsInitialized)
            {
                //limit performace impact
                System.Threading.Thread.Sleep(500);
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
                if (isPlaying || timer.ElapsedMilliseconds < timeout_seconds * 1000)
                {
                    try
                    {
                        oldTrack = currentTrack;
                        currentTrack = GetStuff();
                        if (_client.CurrentPresence == null || _client.CurrentPresence.Details != ("Title: " + currentTrack.Title) || wasPlaying != isPlaying)
                        {
                            var details = $"Title: {currentTrack.Title}";
                            var state = $"Artist: {currentTrack.Artist}";
                            var album = currentTrack.AlbumTitle;
                            album = album.Replace(" ", "");
                            album = album.ToLower();

                            bool hasAlbum = false;
                            foreach (string str in albums)
                                hasAlbum |= str.Equals(album);

                            _client.SetPresence(new RichPresence
                            {
                                Details = details,
                                State = state,
                                Assets = new Assets
                                {
                                    LargeImageKey = (albums.Contains(album) ? album : (big_assets.ContainsKey(playerName) ? big_assets[playerName] : defaultPlayer)),
                                    LargeImageText = currentTrack.AlbumTitle.Length > 0 ? currentTrack.AlbumTitle : "Unknown Album",
                                    SmallImageKey = isPlaying ? (little_assets.ContainsKey(playerName) ? little_assets[playerName] : defaultPlayer) : "paused",
                                    SmallImageText = isPlaying ? ("using " + aliases[playerName]) : "paused"
                                }
                            });

                            _client.Invoke();

                            SetConsole(currentTrack.Title, currentTrack.Artist, currentTrack.AlbumTitle, album);
                        }
                        #if DEBUG
                        Console.Write("" + (metaTimer.ElapsedMilliseconds) + "(" + (timer.ElapsedMilliseconds/* < timeout_seconds * 1000*/) + ") in " + playerName + '\r');
                        #endif
                    }
                    catch (Exception)
                    {
                        _client.SetPresence(new RichPresence()
                        {
                            Details = "Failed to get track info"
                        });
                        Console.Write("Failed to get track info\r");
                    }
                }
                else
                {
                    SetClear();
                    #if DEBUG
                    Console.Write("Cleared " + (metaTimer.ElapsedMilliseconds) + "\r");
                    #endif
                    if (_client.CurrentPresence != null)
                    {
                        _client.ClearPresence();
                        _client.Invoke();
                    }
                }
            }
        }

        private static void SetConsole(string Title, string Artist, string Album, string album)
        {
            Console.Clear();
            Console.WriteLine(appDetails);
            Console.WriteLine($"Title: {Title}, Artist: {Artist}, Album: {Album} {(albums.Contains(album) ? "\nThis is a good one, check ur DRP ;)" : "")}");
            justcleared = false;
        }

        private static void SetClear()
        {
            if (!justcleared) {
                justcleared = true;
                Console.Clear();
                Console.Write("Nothing Playing\r");
            }
        }

        private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
        {
            if (args.Presence != null)
            {
                if (pressenceDetails != args.Presence.Details)
                {
                    pressenceDetails = _client.CurrentPresence?.Details;
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
                        if (validPlayers.Contains(process.ProcessName.ToLower()) && session.QueryInterface<AudioMeterInformation>().GetPeakValue() > 0)
                        {
                            playerName = process.ProcessName.ToLower();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void LoadSettings()
        {

        }
    }
}
