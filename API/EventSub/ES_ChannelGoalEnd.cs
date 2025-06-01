using System;

namespace SuiBot_TwitchSocket.API.EventSub
{
	public class ES_ChannelGoal
	{
		//Can probably rework to just channel goal
		public string id;
		public string broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
		public string type;
		public string description;
		public bool? is_achieved;
		public string current_amount;
		public string target_amount;
		public DateTime started_at;
		public DateTime? ended_at;
	}
}
