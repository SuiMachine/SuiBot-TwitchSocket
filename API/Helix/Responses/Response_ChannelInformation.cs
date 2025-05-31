namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_ChannelInformation
	{
		public string broadcaster_id;
		public string broadcaster_login;
		public string broadcaster_name;
		public string broadcaster_language;
		public string game_id;
		public string game_name;
		public string title;
		public uint delay;
		public string[] tags;
		public string[] content_classification_labels;
		public bool is_branded_content;
	}
}
