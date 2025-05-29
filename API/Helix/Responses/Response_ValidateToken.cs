namespace SuiBot_TwitchSocket.API.Helix.Responses
{
	public class Response_ValidateToken
	{
		public string client_id;
		public string login;
		public string[] scopes;
		public string user_id;
		public ulong expires_in;
	}
}
