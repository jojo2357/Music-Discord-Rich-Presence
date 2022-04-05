using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
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

			Uri queryString = new Uri(default_endpoint + "term=" + album + "&media=music&entity=album");
			HttpResponseMessage result = await myClient.GetAsync(queryString);

			JObject jObject = JObject.Parse(result.Content.ToString());

			if (jObject["resultCount"] != null && Int16.Parse(jObject["resultCount"].ToString()) > 0)
			{
				return cache[album] = jObject["results"].First["artworkUrl100"].ToString().Replace("100x100", "512x512");
			}

			return cache[album] = "";
		}
	}
}