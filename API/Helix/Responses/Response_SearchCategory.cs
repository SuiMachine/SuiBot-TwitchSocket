using System.Diagnostics;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	[DebuggerDisplay(nameof(Response_SearchCategory) + " ({id}) {name}")]
	public class Response_SearchCategory
	{
		public string box_art_url;
		public string name;
		public string id;
	}
}
