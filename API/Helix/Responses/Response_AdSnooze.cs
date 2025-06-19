using System;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_AdSnooze
	{
		public class Exception_NoSnoozesLeft : Exception { }

		public int snooze_count;
		public DateTime snooze_refresh_at;
		public DateTime next_ad_at;
	}
}
