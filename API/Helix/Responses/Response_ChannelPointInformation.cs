using System;
using System.Diagnostics;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	[DebuggerDisplay(nameof(Response_ChannelPointInformation) + " {id} - {title}")]
	public class Response_ChannelPointInformation
	{
		public class ImageInformation
		{
			public string url_1x;
			public string url_2x;
			public string url_4x;
		}

		public class MaxPerStreamSetting
		{
			public bool is_enabled;
			public int max_per_stream;
		}

		public class MaxPerUserPerStreamSetting
		{
			public bool is_enabled;
			public int max_per_user_per_stream;
		}

		public class GlobalCooldownSetting
		{
			public bool is_enabled;
			public int global_cooldown_seconds;
		}

		public string broadcaster_name;
		public string broadcaster_login;
		public string broadcaster_id;
		public string id;
		public ImageInformation image;
		public string background_color;
		public bool is_enabled;
		public int cost;
		public string title;
		public string prompt;
		public bool is_user_input_required;
		public MaxPerStreamSetting max_per_stream_setting;
		public MaxPerUserPerStreamSetting max_per_user_per_stream_setting;
		public GlobalCooldownSetting global_cooldown_setting;
		public bool is_paused;
		public bool is_in_stock;
		public ImageInformation default_image;
		public bool should_redemptions_skip_request_queue;
		public int? redemptions_redeemed_current_stream;
		public DateTime? cooldown_expires_at;
	}
}
