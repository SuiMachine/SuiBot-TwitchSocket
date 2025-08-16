using Newtonsoft.Json.Linq;
using System.Xml.Serialization;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_GetChatters
	{
		public ChatterInfo[] data;
		[XmlIgnore] public JToken pagination;
		public int total;
		public class ChatterInfo
		{
			public string user_id;
			public string user_login;
			public string user_name;
		}
	}
}
