
namespace SuiBot_TwitchSocket.API.EventSub
{
	public class ES_SharedChatBegin
	{
		public string session_id;
		public string broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
		public string host_broadcaster_user_id;
		public string host_broadcaster_user_name;
		public string host_broadcaster_user_login;
		public ES_SharedChatUpdate.SharedChatParticipant[] participants;
	}

	public class ES_SharedChatUpdate
	{
		public class SharedChatParticipant
		{
			public string broadcaster_user_id;
			public string broadcaster_user_name;
			public string broadcaster_user_login;
		}

		public string session_id;
		public string broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
		public string host_broadcaster_user_id;
		public string host_broadcaster_user_name;
		public string host_broadcaster_user_login;
		public SharedChatParticipant[] participants;
	}

	public class ES_SharedChatEnd
	{
		public string session_id;
		public string broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
		public string host_broadcaster_user_id;
		public string host_broadcaster_user_name;
		public string host_broadcaster_user_login;
	}
}
