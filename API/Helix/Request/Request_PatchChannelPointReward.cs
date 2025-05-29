using SuiBot_TwitchSocket.API.Helix.Responses;

namespace SuiBot_TwitchSocket.API.Helix.Request
{
	//BECAUSE OF COURSE WE CAN'T REUSE "Response_ChannelPointInformation"
	//THAT WOULD BE ACTUALLY NICE AND EASY
	//NO, WE NEED A COMPLETELY DIFFERENT STRUCTURE
	//THANKS TWITCH
	public class Request_CreateOrPatchChannelPointReward
	{
		public string title;
		public string prompt;
		public int? cost;
		public string background_color;
		public bool? is_enabled;
		public bool? is_user_input_required;
		public bool? is_max_per_stream_enabled;
		public int? max_per_stream;
		public bool? is_max_per_user_per_stream_enabled;
		public int? max_per_user_per_stream;
		public bool? is_global_cooldown_enabled;
		public int? global_cooldown_seconds;
		public bool? is_paused;
		public bool? should_redemptions_skip_request_queue;

		public static Request_CreateOrPatchChannelPointReward CreateByComparingRewards(Response_ChannelPointInformation newReward, Response_ChannelPointInformation oldReward)
		{
			bool max_per_stream_setting_Changed = newReward.max_per_stream_setting.is_enabled != oldReward.max_per_stream_setting.is_enabled || newReward.max_per_stream_setting.max_per_stream != oldReward.max_per_stream_setting.max_per_stream;
			bool max_per_user_per_stream_setting_Changed = newReward.max_per_user_per_stream_setting.is_enabled != oldReward.max_per_user_per_stream_setting.is_enabled || newReward.max_per_user_per_stream_setting.max_per_user_per_stream != oldReward.max_per_user_per_stream_setting.max_per_user_per_stream;
			bool globalCooldownChanged = newReward.global_cooldown_setting.is_enabled != oldReward.global_cooldown_setting.is_enabled || newReward.global_cooldown_setting.global_cooldown_seconds != oldReward.global_cooldown_setting.global_cooldown_seconds;


			return new Request_CreateOrPatchChannelPointReward()
			{
				title = newReward.title != oldReward.title ? newReward.title : null,
				prompt = newReward.prompt != oldReward.prompt ? newReward.prompt : null,
				cost = newReward.cost != oldReward.cost ? new int?(newReward.cost) : null,
				background_color = newReward.background_color != oldReward.background_color ? newReward.background_color : null,
				is_enabled = newReward.is_enabled != oldReward.is_enabled ? new bool?(newReward.is_enabled) : null,
				is_user_input_required = newReward.is_user_input_required != oldReward.is_user_input_required ? new bool?(newReward.is_user_input_required) : null,
				is_max_per_stream_enabled = max_per_stream_setting_Changed ? new bool?(newReward.max_per_stream_setting.is_enabled) : null,
				max_per_stream = max_per_stream_setting_Changed ? new int?(newReward.max_per_stream_setting.max_per_stream) : null,
				is_max_per_user_per_stream_enabled = max_per_user_per_stream_setting_Changed ? new bool?(newReward.max_per_stream_setting.is_enabled) : null,
				max_per_user_per_stream = max_per_user_per_stream_setting_Changed ? new int?(newReward.max_per_stream_setting.max_per_stream) : null,
				is_global_cooldown_enabled = globalCooldownChanged ? new bool?(newReward.global_cooldown_setting.is_enabled) : null,
				global_cooldown_seconds = globalCooldownChanged ? new int?(newReward.global_cooldown_setting.global_cooldown_seconds) : null,
				is_paused = newReward.is_paused != oldReward.is_paused ? new bool?(newReward.is_paused) : null,
				should_redemptions_skip_request_queue = newReward.should_redemptions_skip_request_queue != oldReward.should_redemptions_skip_request_queue ? new bool?(newReward.should_redemptions_skip_request_queue) : null
			};
		}
	}
}
