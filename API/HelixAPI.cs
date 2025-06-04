using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBot_TwitchSocket.API.EventSub.Subscription;
using SuiBot_TwitchSocket.API.EventSub.Subscription.Responses;
using SuiBot_TwitchSocket.API.Helix.Request;
using SuiBot_TwitchSocket.API.Helix.Responses;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SuiBot_TwitchSocket.API.EventSub.Subscription.Responses.Response_SubscribeTo;

namespace SuiBot_TwitchSocket.API
{
	public class HelixAPI
	{
		public enum ValidationResult
		{
			NoResponse,
			Successful,
			Failed
		}

		public List<Response_ChannelPointInformation> RewardsCache { get; private set; }
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		public string BotLoginName { get; private set; }
		public string BotUserId { get; private set; }
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

			BotLoginName = obj.login;
			BotUserId = obj.user_id;
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
			this.BotLoginName = validation.login;
			this.BotUserId = validation.user_id;
			return validation;
		}

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

		public void GetStatus(IChannelInstance instance)
		{
			var oldStatus = instance.StreamStatus;

			var result = HttpWebRequestHandlers.PerformGetSync(BASE_URI, "streams", $"?user_login={instance.Channel}", BuildDefaultHeaders());
			if (result != "")
			{
				var response = JObject.Parse(result);
				if (response["data"] != null)
				{
					var data = response["data"].ToObject<Response_StreamStatus[]>();
					if (data.Length > 0)
					{
						instance.StreamStatus = data[0];
						instance.StreamStatus.IsOnline = true;
						instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
					}
					else
					{
						instance.StreamStatus = new Response_StreamStatus();
						instance.StreamStatus.IsOnline = false;
						instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
					}
				}
				else
				{
					instance.StreamStatus = new Response_StreamStatus();
					instance.StreamStatus.IsOnline = false;
					instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
				}
			}
			else
			{
				ErrorLoggingSocket.WriteLine($"Error checking status for {instance.Channel}");
			}
		}

		/// <summary>
		/// Gets a streamers stream information - if the stream isn't live, this will be null. To get offline info use GetChannelInformation.
		/// </summary>
		/// <param name="channelID">Channel ID to get information for</param>
		/// <returns>Response_StreamStatus object containing a response or null at failure</returns>
		public async Task<Response_StreamStatus> GetStatus(string channelID)
		{
			var result = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "streams", $"?user_login={channelID}", BuildDefaultHeaders());
			if (result != "")
			{
				var response = JObject.Parse(result);
				if (response["data"] != null)
				{
					var data = response["data"].ToObject<Response_StreamStatus[]>();
					if (data.Length > 0)
						return data[0];
					else
						return null;
				}
				else
					return null;
			}
			else
			{
				ErrorLoggingSocket.WriteLine($"Error checking status for {channelID}");
				return null;

			}
		}

		/// <summary>
		/// Gets the channel information - this will include information like last stream category/game, title etc. but won't tell you whatever the stream is live or not - to check whatever streamer is live use GetStatus
		/// </summary>
		/// <param name="channelID">Channel ID to get information for</param>
		/// <returns>Response_ChannelInformation object containing a response or null at failure</returns>
		public async Task<Response_ChannelInformation> GetChannelInformation(string channelID)
		{
			var result = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "channels", $"?broadcaster_id={channelID}", BuildDefaultHeaders());
			if (result != "")
			{
				var deserialize = JsonConvert.DeserializeObject<JToken>(result);
				if (deserialize["data"] != null)
				{
					var content = deserialize["data"].ToObject<Response_ChannelInformation[]>();
					if (content != null && content.Length > 0)
						return content[0];
					else
						return null;
				}
				else
					return null;
			}
			else
			{
				ErrorLoggingSocket.WriteLine($"Error checking status for {channelID}");
				return null;
			}
		}

		/// <summary>
		/// Removes a given message (if possible)
		/// </summary>
		/// <param name="messageID">Message to delete</param>
		public void RequestRemoveMessage(ES_ChatMessage messageID)
		{
			Task.Run(async () =>
			{
				try
				{
					_ = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "moderation/chat", $"?broadcaster_id={messageID.broadcaster_user_id}&moderator_id={BotUserId}&message_id={messageID}", BuildDefaultHeaders());
				}
				catch (Exception e)
				{
					ErrorLoggingSocket.WriteLine($"Failed to remove message: {e}");
				}
			});
		}

		/// <summary>
		/// Times out a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to timeout</param>
		/// <param name="length">For how long to timeout a user (this has to shorter than 2 weeks)</param>
		/// <param name="reason">Optimal reason for a timeout - can be null</param>
		public void RequestTimeout(ES_ChatMessage message, TimeSpan length, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={BotUserId}", serialize, BuildDefaultHeaders());

			});
		}

		/// <summary>
		/// Times out a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to timeout</param>
		/// <param name="length">For how long to timeout a user (in seconds) - can not be larger than 1_209_600.</param>
		/// <param name="reason">Optimal reason for a timeout - can be null</param>
		public void RequestTimeout(ES_ChatMessage message, uint length, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, (int)length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={BotUserId}", serialize, BuildDefaultHeaders());

			});
		}

		public void RequestTimeout(string broadcaster_id, string chatter_user_id, uint length, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(chatter_user_id, (int)length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={broadcaster_id}&moderator_id={BotUserId}", serialize, BuildDefaultHeaders());

			});
		}

		/// <summary>
		/// Bans a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to ban</param>
		/// <param name="reason">Optimal reason for a ban - can be null</param>
		public void RequestBan(ES_ChatMessage message, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(message.chatter_user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={BotUserId}", serialize, BuildDefaultHeaders());

			});
		}

		/// <summary>
		/// Bans a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to ban</param>
		/// <param name="reason">Optimal reason for a ban - can be null</param>
		public void RequestBan(string broadcaster_id, string chatter_user_id, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(chatter_user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={broadcaster_id}&moderator_id={BotUserId}", serialize, BuildDefaultHeaders());
			});
		}

		/// <summary>
		/// Gets user information based on Twitch login name
		/// </summary>
		/// <param name="twitchLogin">Twitch login name</param>
		/// <returns>Response_GetUserInfo object or null</returns>
		private async Task<Response_GetUserInfo> GetUserInfo(string twitchLogin)
		{
			if (UserNameToInfo.TryGetValue(twitchLogin, out Response_GetUserInfo userId))
				return userId;
			else
			{
				var result = await HttpWebRequestHandlers.PerformGetAsync("https://api.twitch.tv/helix/", "users", $"?login={twitchLogin}", BuildDefaultHeaders());
				if (!string.IsNullOrEmpty(result))
				{
					var response = JObject.Parse(result);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						var userInfo = response["data"].First.ToObject<Response_GetUserInfo>();
						UserNameToInfo.Add(twitchLogin, userInfo);
						return userInfo;
					}
				}
			}

			return null;
		}

		public void RequestUpdate(IChannelInstance instance)
		{
			//var oldStatus = instance.StreamStatus;
			GetStatus(instance);
			if (instance.StreamStatus.game_name != string.Empty)
			{
				instance.SendChatMessage($"New isOnline status is - {instance.StreamStatus.IsOnline} and the game is: {instance.StreamStatus.game_name}");
			}
			else
			{
				instance.SendChatMessage($"New isOnline status is - {instance.StreamStatus.IsOnline}");
			}
		}

		/// <summary>
		/// Subscribes to receiving chat message for a given channel
		/// </summary>
		/// <param name="twitchLoginName">Channel login name for which to start receiving messages</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>Subscription_Response_Data object or null.</returns>
		public async Task<Subscription_Response_Data> SubscribeToChatMessage(string twitchLoginName, string sessionId)
		{
			Response_GetUserInfo channelInfo = await GetUserInfo(twitchLoginName);
			if (channelInfo == null)
				return null;

			var content = new SubscribeMSG_ReadChannelMessage(channelInfo.id, BotUserId.ToString(), sessionId);
			var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (!string.IsNullOrEmpty(result))
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					if (deserialize.data.Length > 0)
					{
						return deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelInfo.id);
					}
					else
						return null;
				}
				else
					return null;
			}
			else
				return null;
		}

		/// <summary>
		/// Subscribes to receiving chat message for a given channel using its ID (this is more optimal than SubscribeToChatMessage)
		/// </summary>
		/// <param name="channelID">Channel ID for which to start receiving messages</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>Subscription_Response_Data object or null</returns>
		public async Task<Subscription_Response_Data> SubscribeToChatMessageUsingID(string channelID, string sessionId)
		{
			var content = new SubscribeMSG_ReadChannelMessage(channelID, BotUserId.ToString(), sessionId);
			var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (!string.IsNullOrEmpty(result))
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					if (deserialize.data.Length > 0)
					{
						return deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					}
					else
						return null;
				}
				else
					return null;
			}
			else
				return null;
		}

		/// <summary>
		/// Subscribes to start receiving "went online" status
		/// </summary>
		/// <param name="channelID">Channel ID for which to start receiving the status updates</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToOnlineStatus(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_StreamOnline(channelID, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		/// <summary>
		/// Subscribes to start receiving "went offline" status updates
		/// </summary>
		/// <param name="channelID">Channel ID for which to start receiving the status updates</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToOfflineStatus(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_StreamOffline(channelID, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		public async Task<bool> SubscribeToAutoModHold(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_AutomodMessageHold(channelID, BotUserId.ToString(), sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		/// <summary>
		/// Subscribes to start receiving "suspicious message was held" type messages for a channel - a bot needs to have a moderator status in the channel
		/// </summary>
		/// <param name="channelID">Channel ID for which to start receiving the status updates</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToChannelSuspiciousUserMessage(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_ChannelSuspiciousUserMessage(channelID, BotUserId.ToString(), sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		//Broken
		public async Task<bool> SubscribeToChannelAdBreak(string channelID, string sessionID)
		{
			//Idk... why this breaks with 403
			var request = new SubscribeMSG_ChannelAdBreakBegin(channelID, BotUserId.ToString(), sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id?.ToString() == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		/// <summary>
		/// Subscribes to receiving channel redeems in the channel
		/// </summary>
		/// <param name="channelID">Channel ID for which to start receiving the status updates</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToChannelRedeem(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_RedemptionAdd(channelID, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		/// <summary>
		/// Subscribes to receiving information about goal end
		/// </summary>
		/// <param name="channelID">Channel for which to receive messages over websocket</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToGoalEnd(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_ChannelGoalEnd(channelID, sessionID);
			var serialize = DefaultSerialize(request);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		/// <summary>
		/// Sends a message in a chat for a specific channel
		/// </summary>
		/// <param name="instance">Instance of a channel (IChannelInstance)</param>
		/// <param name="text">Text to send in chat</param>
		public async Task SendMessageAsync(IChannelInstance instance, string text)
		{
			var content = Request_SendChatMessage.CreateMessage(instance.ChannelID, BotUserId.ToString(), text);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());
		}

		/// <summary>
		/// Sends a response to a message (ES_ChatMessage contains a channel to which to send a response) etc.
		/// </summary>
		/// <param name="messageToRespondTo">Message to which to respond</param>
		/// <param name="message">Content of the response</param>
		public async Task SendResponseAsync(ES_ChatMessage messageToRespondTo, string message)
		{
			var content = Request_SendChatMessage.CreateResponse(messageToRespondTo.broadcaster_user_id.ToString(), BotUserId.ToString(), messageToRespondTo.message_id, message);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());
		}

		public async Task SendResponseAsync(string broadcaster_id, string message_id, string message)
		{
			var content = Request_SendChatMessage.CreateResponse(broadcaster_id, BotUserId.ToString(), message_id, message);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());
		}

		//Not implemented
		public void RequestShoutout(ES_ChatMessage lastMessage, string username)
		{
/*			Task.Run(async () =>
			{

			});*/
		}

		/// <summary>
		/// Gets the list of current subscriptions - this should be done when connecting or disconnecting with a websocket and to 
		/// </summary>
		/// <returns>Response_SubscribeTo object containing information about your current and previous subscriptions including costs</returns>
		public async Task<Response_SubscribeTo> GetCurrentSubscriptions()
		{
			var result = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "eventsub/subscriptions", "", BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					return deserialize;
				}
				else
					return null;
			}

			return null;
		}

		/// <summary>
		/// Closes a subscription (preferably used after GetCurrentSubscriptions).
		/// </summary>
		/// <param name="subscription">Subscription to close</param>
		/// <returns>Awaitable task (no result)</returns>
		public async Task CloseSubscription(Subscription_Response_Data subscription)
		{
			await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "eventsub/subscriptions", $"?id={subscription.id}", BuildDefaultHeaders());
		}

		/// <summary>
		/// Updates a redemption status of an award. NOTE - the award has to be created by the same client id (bot) for it to be managed.
		/// </summary>
		/// <param name="redeem">Channel point redemption to set the redeem status of</param>
		/// <param name="fullfilmentStatus">Status to set (has to be either FULFILLED / CANCELLED)</param>
		public void UpdateRedemptionStatus(ES_ChannelPoints.ES_ChannelPointRedeemRequest redeem, ES_ChannelPoints.RedemptionStates fullfilmentStatus)
		{
			Task.Run(async () =>
			{
				if (fullfilmentStatus == ES_ChannelPoints.RedemptionStates.UNFULFILLED)
					return; //I have no idea why would anyone do this
				var redeemStatus = Helix.Request.Request_UpdateChannelPointsRedemptionStatus.UpdateWith(fullfilmentStatus);
				var serialize = JsonConvert.SerializeObject(redeemStatus, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var pathRequest = await HttpWebRequestHandlers.PerformPatchAsync(BASE_URI, "channel_points/custom_rewards/redemptions", $"?id={redeem.id}&broadcaster_id={redeem.broadcaster_user_id}&reward_id={redeem.reward.id}", serialize, BuildDefaultHeaders()); ;
			});
		}

		/// <summary>
		/// Creates a local rewards cache (list) that can be managed by the bot - these are stored in HelixAPI object instance - RewardsCache
		/// </summary>
		/// <returns>True/False depending on success of the operation</returns>
		public async Task<bool> CreateRewardsCache()
		{
			var rewardsRequest = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={BotUserId}&only_manageable_rewards=true", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(rewardsRequest))
			{
				ErrorLoggingSocket.WriteLine("Failed to get rewards");
				return false;
			}

			var deserialize = (JToken)JsonConvert.DeserializeObject(rewardsRequest);
			if (deserialize["data"] != null)
			{
				RewardsCache = deserialize["data"].ToObject<List<Response_ChannelPointInformation>>();
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Deletes the channel reward from the channel
		/// </summary>
		/// <param name="reward">Reward to delete</param>
		/// <returns>True/False depending on success of the operation</returns>
		public async Task<bool> DeleteCustomReward(Response_ChannelPointInformation reward)
		{
			var result = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={reward.broadcaster_id}&id={reward.id}", BuildDefaultHeaders());
			if (result != null)
				return true;
			else
				return false;
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

		/// <summary>
		/// Creates or updates the reward
		/// </summary>
		/// <param name="rewardID">Reward of an ID to udpate</param>
		/// <param name="rewardTitle">Desired title</param>
		/// <param name="rewardDescription">Desired description / prompt</param>
		/// <param name="rewardCost">Desired cost</param>
		/// <param name="rewardCooldown">Desired cooldown</param>
		/// <param name="isEnabled">Whatever it should be enabled</param>
		/// <param name="isUserInputRequired">Whatever user input is required</param>
		/// <returns>Response_ChannelPointInformation object containing a created or updated reward</returns>
		/// <exception cref="Exception">Exception if rewards cache could not be created</exception>
		public async Task<Response_ChannelPointInformation> CreateOrUpdateReward(string rewardID, string rewardTitle, string rewardDescription, int rewardCost, int rewardCooldown, bool isEnabled, bool isUserInputRequired)
		{
			Response_ChannelPointInformation foundReward = null;
			if (RewardsCache == null && rewardID != null)
			{
				if (!await CreateRewardsCache())
					throw new Exception("Failed to download cache");
			}
			foundReward = RewardsCache.FirstOrDefault(x => x.id == rewardID);

			if (foundReward == null)
			{
				//Create reward
				var newReward = new Request_CreateOrPatchChannelPointReward()
				{
					title = rewardTitle,
					prompt = rewardDescription,
					cost = rewardCost,
					is_global_cooldown_enabled = isEnabled,
					global_cooldown_seconds = rewardCooldown,
					is_enabled = isEnabled,
					is_user_input_required = isUserInputRequired,
				};
				var serialize = JsonConvert.SerializeObject(newReward, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});
				var post = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={BotUserId}", serialize, BuildDefaultHeaders());
				if (post == null)
					return null;

				var deserialize = (JToken)JsonConvert.DeserializeObject(post);
				if (deserialize["data"] == null)
					return null;
				var newAward = deserialize["data"].ToObject<Response_ChannelPointInformation[]>();
				if (newAward.Length == 0)
					return null;

				await Task.Delay(2000);
				RewardsCache.Add(newAward[0]);
				return newAward[0];
			}
			else
			{
				bool cooldownEnabled = rewardCooldown > 0;

				//Update reward
				if (foundReward.is_enabled != isEnabled || foundReward.title != rewardTitle || foundReward.prompt != rewardDescription || foundReward.cost != rewardCost || foundReward.global_cooldown_setting.is_enabled != cooldownEnabled || foundReward.global_cooldown_setting.global_cooldown_seconds != rewardCooldown)
				{
					var modifiedCopy = foundReward.CreateCopy();

					modifiedCopy.is_enabled = isEnabled;
					modifiedCopy.title = rewardTitle;
					modifiedCopy.prompt = rewardDescription;
					modifiedCopy.cost = rewardCost;
					modifiedCopy.global_cooldown_setting.is_enabled = cooldownEnabled;
					modifiedCopy.global_cooldown_setting.global_cooldown_seconds = rewardCooldown;

					var serialize = JsonConvert.SerializeObject(Request_CreateOrPatchChannelPointReward.CreateByComparingRewards(modifiedCopy, foundReward), Formatting.Indented, new JsonSerializerSettings()
					{
						NullValueHandling = NullValueHandling.Ignore
					});

					var patch = await HttpWebRequestHandlers.PerformPatchAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={BotUserId}&id={foundReward.id}", serialize, BuildDefaultHeaders());
					if (patch == null)
						return null;
					await Task.Delay(2000);

					var deserialize = (JToken)JsonConvert.DeserializeObject(patch);
					if (deserialize["data"] == null)
						return null;

					var newAward = deserialize["data"].ToObject<Response_ChannelPointInformation[]>();
					if (newAward.Length == 0)
						return null;

					for (int i = 0; i < RewardsCache.Count; i++)
					{
						if (RewardsCache[i] == foundReward)
						{
							RewardsCache[i] = newAward[0];
							return newAward[0];
						}
					}
					return null;
				}
				else
					return foundReward;
			}
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
