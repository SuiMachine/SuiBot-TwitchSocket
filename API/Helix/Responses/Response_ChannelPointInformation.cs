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

		public Response_ChannelPointInformation CreateCopy()
		{
			return new Response_ChannelPointInformation()
			{
				broadcaster_name = broadcaster_name,
				broadcaster_login = broadcaster_login,
				broadcaster_id = broadcaster_id,
				id = id,
				image = new ImageInformation()
				{
					url_1x = this.image?.url_1x,
					url_2x = this.image?.url_2x,
					url_4x = this.image?.url_4x,
				},
				background_color = background_color,
				is_enabled = is_enabled,
				cost = cost,
				title = title,
				prompt = prompt,
				is_user_input_required = is_user_input_required,
				max_per_stream_setting = new MaxPerStreamSetting()
				{
					is_enabled = max_per_stream_setting.is_enabled,
					max_per_stream = max_per_stream_setting.max_per_stream
				},
				max_per_user_per_stream_setting = new MaxPerUserPerStreamSetting()
				{
					is_enabled = max_per_user_per_stream_setting.is_enabled,
					max_per_user_per_stream = max_per_user_per_stream_setting.max_per_user_per_stream
				},
				global_cooldown_setting = new GlobalCooldownSetting()
				{
					is_enabled = global_cooldown_setting.is_enabled,
					global_cooldown_seconds = global_cooldown_setting.global_cooldown_seconds
				},
				is_paused = is_paused,
				is_in_stock = is_in_stock,
				default_image = new ImageInformation()
				{
					url_1x = default_image?.url_1x,
					url_2x = default_image?.url_2x,
					url_4x = default_image?.url_4x,
				},
				should_redemptions_skip_request_queue = should_redemptions_skip_request_queue,
				redemptions_redeemed_current_stream = redemptions_redeemed_current_stream,
				cooldown_expires_at = cooldown_expires_at
			};
		}
	}
}
