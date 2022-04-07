using System;
using System.IO;
using System.Net;
using System.Text;
using Windows.Media.Control;
using DiscordRPC;
using IWshRuntimeLibrary;
using Microsoft.Toolkit.Uwp.Notifications;

namespace MDRP
{
	public class Functions
	{
		public static void GenerateShortcuts()
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

		public static void ClearAPresence(DiscordRpcClient client)
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

		private static void SendNotification(string message)
		{
			SendNotification("MDRP message", message);
		}

		public static void SendNotification(string messageTitle, string message)
		{
			//todo make a notif system for mb
			if (!Program.spawnedFromApplication)
				new ToastContentBuilder()
					.AddText(messageTitle)
					.AddText(message)
					.Show();
			/*ProcessStartInfo errornotif =
				new ProcessStartInfo("sendNotification.bat", "\"" + messageTitle + "\" \"" + message + "\"");
			errornotif.WindowStyle = ProcessWindowStyle.Hidden;
			Process.Start(errornotif);*/
		}

		public static GlobalSystemMediaTransportControlsSessionMediaProperties GetPlayingDetails()
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

		public static void SendToDebugServer(string message)
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
				Console.WriteLine(Program.langHelper[LocalizableStrings.REQUEST_DEBUG_TOOL]);
				Console.ForegroundColor = ConsoleColor.White;
			}
#endif
		}

		public static void SendToDebugServer(Exception exception)
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
				Console.WriteLine(Program.langHelper[LocalizableStrings.ERROR_OCCURRED]);
				Console.ForegroundColor = ConsoleColor.White;
			}
#endif
		}

		public static string GetLargeImageText(string albumName)
		{
			if (albumName.Length > 0)
				if (albumName.Length <= 2)
					return "_" + albumName + "_";
				else
					return CapLength(albumName, 128);
			else
				return Program.langHelper[LocalizableStrings.UNKNOWN_ALBUM];
		}

		public static void DrawPersistentHeader()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Program.Title);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(Program.langHelper.get(LocalizableStrings.VERSION) + ": ");

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(Program.Version);

			if (Program.UpdateAvailibleFlag)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(string.Format(" " + Program.langHelper[LocalizableStrings.NEW_UPDATE], Program.UpdateVersion));
			}

			Console.WriteLine();

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Github: ");

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(Program.Github);

			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.White;
		}

		public static string CapLength(string instring, int capLength)
		{
			return instring.Substring(0, Math.Min(capLength, instring.Length));
		}
	}
}