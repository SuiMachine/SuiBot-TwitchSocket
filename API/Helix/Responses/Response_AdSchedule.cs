using System;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_AdSchedule
	{
		public DateTime? next_ad_at;
		public DateTime? last_ad_at;
		public int duration;
		public int preroll_free_time;
		public int snooze_count;
		public DateTime? snooze_refresh_at;
	}
}
