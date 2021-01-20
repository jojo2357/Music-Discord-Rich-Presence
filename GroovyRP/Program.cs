using System;
using DiscordRPC;
using DiscordRPC.Message;
using Windows.Media.Control;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using System.Linq;

namespace GroovyRP
{
    class Program
    {
        private const string appDetails = "GroovyRP\nOrignal Creator: https://github.com/dsdude123/GroovyRP \nModified: https://github.com/jojo2357/GroovyRP";
        private static readonly DiscordRpcClient _client = new DiscordRpcClient("801209905020272681", autoEvents: false);
        //My head is an animal, Fever Dream (of monsters and men)
        //Sigh no more, wilder mind, babel, delta (Mumford + sons)
        //The Lumineers (the lumineers)
        private static readonly string[] albums = new[] { "myheadisananimal", "feverdream", "babel", "thelumineers", "delta", "sighnomore", "wildermind" };
        private static string pressenceDetails = string.Empty;
        private static bool wasPlaying = false;

        private static void Main()
        {
            _client.Initialize();
            _client.OnError += _client_OnError;
            _client.OnPresenceUpdate += _client_OnPresenceUpdate;

            GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null;
            GlobalSystemMediaTransportControlsSessionMediaProperties oldTrack = null;

            while (_client.IsInitialized)
            {
                //limit performace impact
                System.Threading.Thread.Sleep(500);
                if (IsUsingAudio())
                {
                    try
                    {
                        wasPlaying = true;
                        currentTrack = GetStuff();
                        if (oldTrack == null || oldTrack.Title != currentTrack.Title)
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
                                    LargeImageKey = (hasAlbum ? album :"groove"),
                                    LargeImageText = currentTrack.AlbumTitle.Length > 0 ? currentTrack.AlbumTitle : "Unknown Album",
                                    SmallImageKey = "groove_small",
                                    SmallImageText = "using trash"
                                }
                            });

                            _client.Invoke();
                        }
                    }
                    catch (Exception)
                    {
                        _client.SetPresence(new RichPresence()
                        {
                            Details = "Failed to get track info"
                        });
                        Console.WriteLine("Failed to get track info");
                    }
                }
                else
                {
                    if (wasPlaying)
                    {
                        _client.ClearPresence();
                        oldTrack = null;
                        _client.Invoke();
                    }
                    wasPlaying = false;
                }
            }
        }

        private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
        {
            if (args.Presence != null)
            {
                if (pressenceDetails != args.Presence.Details)
                {
                    pressenceDetails = _client.CurrentPresence?.Details;
                    Console.Clear();
                    Console.WriteLine(appDetails);
                    Console.WriteLine($"{args.Presence.Details}, {args.Presence.State}");
                }
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Nothing Playing");
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
            var grooveMusics = Process.GetProcessesByName("Music.UI");
            if (grooveMusics.Any())
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
                        bool targetProcess = false;
                        using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                        {
                            var process = sessionControl.Process;
                            if (process.ProcessName.Equals("Music.UI"))
                            {
                                targetProcess = true;
                            }
                        }
                        using (var audioMeter = session.QueryInterface<AudioMeterInformation>())
                        {
                            if (audioMeter.GetPeakValue() > 0 && targetProcess)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
