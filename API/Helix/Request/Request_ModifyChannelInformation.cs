namespace SuiBot_TwitchSocket.API.Helix.Request
{
	public class Request_ModifyChannelInformation
	{
		public class ContentClassification
		{
			public enum Classification
			{
				DebatedSocialIssuesAndPolitics,
				DrugsIntoxication,
				SexualThemes,
				ViolentGraphic,
				Gambling,
				ProfanityVulgarity
			}

			public Classification id;
			public bool is_enabled;
		}

		public string game_id;
		public string broadcaster_language;
		public string title;
		public int? delay;
		public string[] tags;
		public ContentClassification[] content_classification_labels;
		public bool? is_branded_content;
	}
}
