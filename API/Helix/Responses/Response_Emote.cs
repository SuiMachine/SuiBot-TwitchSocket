namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_Emote
	{
		public class Emotes
		{
			public string id;
			public string name;
			public string emote_type;
			public string emote_set_id;
			public string owner_id;
			public string[] format;
			public string[] scale;
			public string[] theme_mode;
			public string template;
		}

		public Emotes[] data;
		public Pagination pagination;
		public string template;
	}
}
