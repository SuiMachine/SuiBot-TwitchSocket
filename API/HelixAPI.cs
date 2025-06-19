using Newtonsoft.Json;
using SuiBot_TwitchSocket.API.Helix.Responses;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Collections.Generic;

namespace SuiBot_TwitchSocket.API
{
	public partial class HelixAPI
	{
		public enum ValidationResult
		{
			NoResponse,
			Successful,
			Failed
		}

		public List<Response_ChannelPointInformation> RewardsCache { get; private set; }
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		public string User_LoginName { get; private set; }
		public string User_Id { get; private set; }
		public string CLIENT_ID { get; private set; }

#if LOCAL_API
		//Local user - 92987419
		//Authentication - 2ae883f289a6106
		//Local secret - 1f078371035dec7aaef27b955fabbe
		private const string BASE_URI = "http://localhost:8080/";
#else
		private const string BASE_URI = "https://api.twitch.tv/helix/";
#endif

		private readonly string OAUTH = "";
		//private readonly DateTime LastRequest = DateTime.MinValue;
		private IBotInstance m_BotInstance;

		private Dictionary<string, string> BuildDefaultHeaders()
		{
			return new Dictionary<string, string>()
			{
				{ "Client-ID", CLIENT_ID },
				{ "Authorization", "Bearer " + OAUTH }
			};
		}

		#region Token Validation
		/// <summary>
		/// Wrapper for token validation for more simple operations that returns Enum based on validation state
		/// </summary>
		/// <returns>ValidationResult result - can be NoResponse / Successful / Failed</returns>
		public ValidationResult ValidateToken()
		{
#if LOCAL_API
			var res = HttpWebRequestHandlers.GetSync("http://localhost:8080/mock", "validate", "", BuildDefaultHeaders());
#else
			var res = HttpWebRequestHandlers.PerformGetSync("https://id.twitch.tv/oauth2/", "validate", "", BuildDefaultHeaders());
#endif
			if (string.IsNullOrEmpty(res))
				return ValidationResult.NoResponse;

			Response_ValidateToken obj = JsonConvert.DeserializeObject<Response_ValidateToken>(res);
			if (obj == null)
				return ValidationResult.Failed;

			User_LoginName = obj.login;
			User_Id = obj.user_id;
			if (obj.expires_in < 60 * 60 * 24 * 7) //expires in less than 7 days
			{
				var ts = TimeSpan.FromSeconds(obj.expires_in);
				ErrorLoggingSocket.WriteLine($"Token expires in: {ts}");
			}
			if (obj.client_id != CLIENT_ID)
			{
				ErrorLoggingSocket.WriteLine("Invalid client ID for this token!");
				return ValidationResult.Failed;
			}

			return ValidationResult.Successful;
		}

		/// <summary>
		/// Validates a ouath token with Twitch
		/// </summary>
		/// <returns>Response_ValidateToken object containing information for validation or null</returns>
		public Response_ValidateToken GetValidation()
		{
			var res = HttpWebRequestHandlers.PerformGetSync("https://id.twitch.tv/oauth2/", "validate", "", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(res))
				return null;

			var validation = JsonConvert.DeserializeObject<Response_ValidateToken>(res);
			this.User_LoginName = validation.login;
			this.User_Id = validation.user_id;
			return validation;
		}


		/// <summary>
		/// Creates an authentication URL by combining client id, callback address and list of required scopes
		/// </summary>
		/// <param name="client_id">Client ID for which to create URL</param>
		/// <param name="callbackAddress">Callback address</param>
		/// <param name="scopes">Scopes</param>
		/// <returns>URL that can be opened to receive OAUTH from Twitch</returns>
		public static string GenerateAuthenticationURL(string client_id, string callbackAddress, string[] scopes)
		{
			var url = new Uri($"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={callbackAddress}&response_type=token&scope={string.Join(" ", scopes)}");

			return url.AbsoluteUri;
		}
		#endregion
		//For testing
		public HelixAPI(string clientID, IBotInstance bot, string aouth)
		{
			this.m_BotInstance = bot;
			this.OAUTH = aouth;
			this.CLIENT_ID = clientID;
#if LOCAL_API
			this.BotLoginName = "fishershepard595";
#endif
		}

		private string DefaultSerialize(object obj)
		{
			return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}
	}
}
