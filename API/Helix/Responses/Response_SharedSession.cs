using System;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_SharedSession
	{
		public class Participants
		{
			public string broadcaster_id;
		}

		public string session_id;
		public string host_broadcaster_id;
		public Participants[] participants;
		public DateTime? created_at;
		public DateTime? updated_at;
	}
}
