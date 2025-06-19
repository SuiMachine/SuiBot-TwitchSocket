using Newtonsoft.Json;
using SuiBot_TwitchSocket.API.EventSub.Subscription;
using SuiBot_TwitchSocket.API.EventSub.Subscription.Responses;
using SuiBot_TwitchSocket.API.Helix.Responses;
using System.Linq;
using System.Threading.Tasks;
using static SuiBot_TwitchSocket.API.EventSub.Subscription.Responses.Response_SubscribeTo;

namespace SuiBot_TwitchSocket.API
{
	public partial class HelixAPI
	{
		/// <summary>
		/// Subscribes to receiving chat message for a given channel
		/// </summary>
		/// <param name="twitchLoginName">Channel login name for which to start receiving messages</param>
		/// <param name="sessionId">Websocket session ID</param>
		/// <returns>Subscription_Response_Data object or null.</returns>
		public async Task<Subscription_Response_Data> SubscribeToChatMessage(string twitchLoginName, string sessionId)
		{
			Response_GetUserInfo channelInfo = await GetUserInfoByUserLogin(twitchLoginName);
			if (channelInfo == null)
				return null;

			var content = new SubscribeMSG_ReadChannelMessage(channelInfo.id, User_Id.ToString(), sessionId);
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
			var content = new SubscribeMSG_ReadChannelMessage(channelID, User_Id.ToString(), sessionId);
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
			var request = new SubscribeMSG_AutomodMessageHold(channelID, User_Id.ToString(), sessionID);
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
			var request = new SubscribeMSG_ChannelSuspiciousUserMessage(channelID, User_Id.ToString(), sessionID);
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
		/// This can only be subscribed to with channel owner account!
		/// </summary>
		/// <param name="channelID">ID of a channel</param>
		/// <param name="sessionID">ID of Websocket session</param>
		/// <returns>True/False depending on success of an operation</returns>
		public async Task<bool> SubscribeToChannelAdBreak(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_ChannelAdBreakBegin(channelID, User_Id.ToString(), sessionID);
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
		/// Subscribes to receiving raid notifications over websocket
		/// </summary>
		/// <param name="channelID">Channel for which to subscribe</param>
		/// <param name="sessionID">Websocket session id</param>
		/// <returns>True/False depending on success of operation</returns>
		public async Task<bool> SubscribeToRaidNotification(string channelID, string sessionID)
		{
			var request = new SubscribeMSG_ChannelRaid(channelID, sessionID);
			var serialize = DefaultSerialize(request);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.to_broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}
	}
}
