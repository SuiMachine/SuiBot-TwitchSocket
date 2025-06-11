using System;

namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class UniversalResponse_Transport
	{
		public string method;
		public string session_id;
		public DateTime connected_at;
	}

	public class Pagination
	{
		public string cursor;
	}
}
