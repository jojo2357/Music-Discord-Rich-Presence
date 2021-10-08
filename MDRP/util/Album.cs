using System.Linq;

namespace MDRP
{
	internal class Album
	{
		private readonly string[] Artists;
		public readonly string Name;

		public Album(string name) : this(name, "*")
		{
		}

		public Album(string name, params string[] artists)
		{
			Name = name;
			Artists = artists.Where(artist => artist != "").ToArray();
			for (int i = 0; i < Artists.Length; i++) Artists[i] = Artists[i].ToLower();
		}

		//dont hash the artists, we are interested in dict#contains to return true for the same name, we will sort out
		//different artists later. (keyed wrong issue)
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override string ToString()
		{
			return Name + " by " + string.Join(",", Artists);
		}

		/**
		 * Returns true if and only if the provided album has the same name and accepts the same artists
		 */
		public override bool Equals(object obj)
		{
			if (typeof(Album).IsInstanceOfType(obj))
				return Equals((Album)obj);
			return false;
		}

		/**
		 * Returns true if and only if the provided album has the same name and accepts the same artists
		 */
		public bool Equals(Album otherAlbum)
		{
			if (otherAlbum != null && otherAlbum.Name.Equals(Name))
			{
				if (otherAlbum.AcceptsAnyArtist() || AcceptsAnyArtist())
					return true;
				foreach (string myArtist in Artists.Where(artist => artist != ""))
				foreach (string theirArtist in otherAlbum.Artists.Where(artist => artist != ""))
					if (theirArtist.Contains(myArtist) || myArtist.Contains(theirArtist))
						return true;
			}

			return false;
		}

		private bool AcceptsAnyArtist()
		{
			return Artists.Length == 0 || Artists[0] == "*";
		}
	}
}