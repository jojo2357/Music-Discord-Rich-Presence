using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json.Linq;

namespace MDRP
{
	public class ExternalArtManager
	{
		private const string default_endpoint = "https://itunes.apple.com/search?limit=200&";
		public const string cacheFileLocation = "../../../clientdata/cachedImages.dat";
		public string langAddon = "lang=" + (Program.translateFromJapanese ? "en_us" : "ja_jp");

		private HttpClient myClient = new HttpClient();

		private Dictionary<Album, string> cache = new Dictionary<Album, string>();

		public bool HasAlbum(Album album)
		{
			return cache.ContainsKey(album);
		}

		public string GetAlbum(Album album)
		{
			if (HasAlbum(album))
			{
				return cache[album];
			}
			else
			{
				return "";
			}
		}

		public string AlbumLookup(Album album, string backupTitle)
		{
			if (cache.ContainsKey(album))
				return cache[album];
			string returnValue = ExternalAlbumLookup(album, backupTitle).Result;
			if (Program.createCacheFile)
			{
				if (!File.Exists(cacheFileLocation))
				{
					File.WriteAllText(cacheFileLocation, "*=*\nid=default");
				}
				File.AppendAllText(cacheFileLocation, "\n" + album.ExportStringWithKey(returnValue));
			}

			return cache[album] = returnValue;
		}

		private async Task<String> ExternalAlbumLookup(Album album, string backupTitle)
		{

			if (album.Name == "" || album.Name.ToLower() == "unknown album" || album.Artists.Length == 0 || (album.Artists.Length == 1 && album.Artists[0].ToLower() == "unknown artist"))
				return "";

			if (album.Artists[0] == "jojo2357" && album.Name.ToLower() == "king and lionheart")
			{
				return "https://jojo2357.github.io/Album-Arts/MHIA_my_watermark.png";
			}

			Uri queryString = new Uri(default_endpoint + langAddon + "&term=" + album.Name + "&media=music&entity=album");
			HttpResponseMessage result = await myClient.GetAsync(queryString);

			JObject jObject = JObject.Parse(result.Content.ToString());

			if (jObject["resultCount"] != null && Int16.Parse(jObject["resultCount"].ToString()) > 0)
			{
				string bestNotPerfectResult = "";
				bool hasNearPerfectResult = false;
				foreach (JToken albumObject in jObject["results"])
				{
					if (albumObject["artworkUrl100"] != null && albumObject["artistName"] != null)
					{
						if (album.Artists.Contains(albumObject["artistName"].ToString().ToLower()))
						{
							Functions.SendToDebugServer("Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
							return albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
						}
						else if (albumObject["artistName"].ToString().Trim().ToLower() == "various artists" && !hasNearPerfectResult)
						{
							hasNearPerfectResult = true;
							bestNotPerfectResult = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							Functions.SendToDebugServer("Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
						}
					}
				}

				if (hasNearPerfectResult)
				{
					return bestNotPerfectResult;
				}
			}

			if (Program.needsExactMatch)
				return "";

			if (Int16.Parse(jObject["resultCount"].ToString()) == 200)
			{
				queryString = new Uri(default_endpoint + langAddon +  "&term=" + album + "&media=music&entity=album");
				result = await myClient.GetAsync(queryString);

				jObject = JObject.Parse(result.Content.ToString());

				if (jObject["resultCount"] != null && Int16.Parse(jObject["resultCount"].ToString()) > 0)
				{
					string bestNotPerfectResult = "";
					bool hasNearPerfectResult = false;
					foreach (JToken albumObject in jObject["results"])
					{
						if (albumObject["artworkUrl100"] != null && albumObject["artistName"] != null)
						{
							if (album.Artists.Contains(albumObject["artistName"].ToString().ToLower()))
							{
								Functions.SendToDebugServer("Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
								return albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							}
							else if (albumObject["artistName"].ToString().Trim().ToLower() == "various artists" && !hasNearPerfectResult)
							{
								hasNearPerfectResult = true;
								bestNotPerfectResult = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
								Functions.SendToDebugServer("Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
							}
							else
							{
								bool isFuzzy = false;
								foreach (String artist in album.Artists)
								{
									if (FuzzySharp.Fuzz.TokenSortRatio(artist, albumObject["artistName"].ToString().ToLower()) >= 90)
									{
										isFuzzy = true;
									}
								}

								if (isFuzzy)
								{
									hasNearPerfectResult = true;
									bestNotPerfectResult = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
									Functions.SendToDebugServer("[FUZZY] Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
								}
							}
						}
					}

					if (hasNearPerfectResult)
					{
						return bestNotPerfectResult;
					}
				}
			}

			queryString = new Uri(default_endpoint + langAddon + "&term=" + backupTitle + "&media=music&entity=song");
			result = await myClient.GetAsync(queryString);

			jObject = JObject.Parse(result.Content.ToString());
			if (jObject["resultCount"] != null)
			{
				if (jObject["resultCount"].ToString() == "1")
				{
					Functions.SendToDebugServer("Found album " + jObject["results"].First["collectionName"] + " by artist " + jObject["results"].First["artistName"] + " using lang-tag " + langAddon);
					return jObject["results"].First["artworkUrl100"].ToString().Replace("100x100", "512x512");
				}
				else if (Int16.Parse(jObject["resultCount"].ToString()) > 0)
				{
					foreach (JToken albumObject in jObject["results"])
					{
						if (albumObject["artworkUrl100"] != null && albumObject["artistName"] != null)
						{
							if (album.Artists.Contains(albumObject["artistName"].ToString().ToLower()))
							{
								Functions.SendToDebugServer("Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
								return albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							}
							bool isFuzzy = false;
							foreach (String artist in album.Artists)
							{
								if (FuzzySharp.Fuzz.TokenSortRatio(artist, albumObject["artistName"].ToString().ToLower()) >= 90)
								{
									isFuzzy = true;
								}
							}
							if (isFuzzy)
							{
								Functions.SendToDebugServer("[FUZZY] Found album " + albumObject["collectionName"] + " by artist " + albumObject["artistName"] + " using lang-tag " + langAddon);
								return albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							}
						}
					}
				}
			}
			Functions.SendToDebugServer("Unable to find album " + album.Name + " by artist " + album.Artists[0] + " using lang-tag " + langAddon);
			return "";
		}
	}
}