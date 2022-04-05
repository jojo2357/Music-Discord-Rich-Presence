using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json.Linq;

namespace MDRP
{
	public class ExternalArtManager
	{
		private const string default_endpoint = "https://itunes.apple.com/search?";

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
		
		public async Task<String> AlbumLookup(Album album)
		{
			if (cache.ContainsKey(album))
				return cache[album];

			if (album.Name == "")
				return cache[album] = "";
			
			/*Console.WriteLine(album.ToString());
			
			Thread.Sleep(2000);*/

			Uri queryString = new Uri(default_endpoint + "term=" + album.Name + "&media=music&entity=album");
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
							return cache[album] = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
						}
						else if (albumObject["artistName"].ToString().Trim().ToLower() == "various artists" && !hasNearPerfectResult)
						{
							hasNearPerfectResult = true;
							bestNotPerfectResult = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
						}
					}
				}

				if (hasNearPerfectResult)
				{
					return cache[album] = bestNotPerfectResult;
				}
			}
			
			if (Int16.Parse(jObject["resultCount"].ToString()) == 50)
			{
				queryString = new Uri(default_endpoint + "term=" + album + "&media=music&entity=album");
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
								return cache[album] = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							}
							else if (albumObject["artistName"].ToString().Trim().ToLower() == "various artists" && !hasNearPerfectResult)
							{
								hasNearPerfectResult = true;
								bestNotPerfectResult = albumObject["artworkUrl100"].ToString().Replace("100x100", "512x512");
							}
						}
					}

					if (hasNearPerfectResult)
					{
						return cache[album] = bestNotPerfectResult;
					}
				}
			}

			return cache[album] = "";
		}
	}
}