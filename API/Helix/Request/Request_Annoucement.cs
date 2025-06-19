using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SuiBot_TwitchSocket.API.Helix.Request
{
	public class Request_Announcement
	{
		public enum Color
		{
			blue,
			green,
			orange,
			purple,
			primary
		}

		public string message;
		[JsonConverter(typeof(StringEnumConverter))] public Color color;

		internal Request_Announcement()
		{
			this.message = null;
			this.color = Color.primary;
		}

		public static Request_Announcement CreateAnnouncement(string message, Color color)
		{
			return new Request_Announcement()
			{
				message = message,
				color = color
			};
		}
	}
}
