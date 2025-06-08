using System;

namespace SuiBot_TwitchSocket.API.EventSub
{
	public class ES_AdBreakBeginNotification
	{
		public int duration_seconds;
		public DateTime started_at;
		public bool is_automatic;
		public string broadcaster_user_id;
		public string broadcaster_user_login;
		public string broadcaster_user_name;
		public string requester_user_id;
		public string requester_user_login;
		public string requester_user_name;
	}
}
