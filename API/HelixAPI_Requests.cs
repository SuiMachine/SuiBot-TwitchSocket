using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBot_TwitchSocket.API.EventSub.Subscription.Responses;
using SuiBot_TwitchSocket.API.Helix.Request;
using SuiBot_TwitchSocket.API.Helix.Responses;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static SuiBot_TwitchSocket.API.EventSub.Subscription.Responses.Response_SubscribeTo;
using static SuiBot_TwitchSocket.API.Helix.Responses.Response_AdSnooze;

namespace SuiBot_TwitchSocket.API
{
	public partial class HelixAPI
	{
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
		/// (In Dev) Starts an ad in user's channel (there is no parameter specifying a target, because only broadcaster can start a commercial / ad)
		/// </summary>
		/// <param name="length">Length of an ad / commercial in seconds</param>
		/// <returns>Task</returns>
		public async Task StartCommercial(uint length)
		{
			if (!Scopes.Contains("channel:read:ads"))
			{
				ErrorLoggingSocket.WriteLine("Client doesn't have required scope");
				return;
			}

			if (length > 180)
				length = 180;

			var requestData = new Request_StartCommercial()
			{
				broadcaster_id = this.User_Id,
				length = length
			};
			var serialize = DefaultSerialize(requestData);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "channels/commercial", $"", serialize, BuildDefaultHeaders());
		}

		/// <summary>
		/// (In Dev) Gets a stream schedule info, which contains when the next ad starts, when was the last one, how long they will least etc.
		/// </summary>
		/// <returns>Response_AdSchedule object or null if failed to obtain it</returns>
		public async Task<Response_AdSchedule> GetAdSchedule()
		{
			var response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "channels/ads", $"?broadcaster_id={User_Id}", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(response))
			{
				return null;
			}

			var content = JsonConvert.DeserializeObject<JToken>(response);
			if (content["data"] == null)
				return null;

			try
			{
				var adData = content["data"].ToObject<Response_AdSchedule[]>();
				if (adData.Length > 0)
				{
					return adData[0];
				}
				else
					return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// (InDev) Snoozes upcoming ad
		/// </summary>
		/// <returns>Response_AdSnooze or null or throws Exception_NoSnoozesLeft</returns>
		public async Task<Response_AdSnooze> SnoozeNextAd()
		{
			var response = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "channels/ads/schedule/snooze", $"?broadcaster_id={User_Id}", "", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(response))
				return null;

			var content = JsonConvert.DeserializeObject<JToken>(response);
			if (content["data"] == null)
				return null;

			try
			{
				var snoozeData = content["data"].ToObject<Response_AdSnooze[]>();
				if (snoozeData.Length > 0)
					return snoozeData[0];
				else
					return null;
			}
			catch (Exception_NoSnoozesLeft ex)
			{
				//TODO - handle 429
				throw ex;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// (InDev) Modifiers user's channel information (stream information)
		/// </summary>
		/// <param name="modifyData">Object containing what to modify - all fields are optional (use nulls)</param>
		/// <returns>Task</returns>
		public async Task ModifyChannelInformation(Request_ModifyChannelInformation modifyData)
		{
			var serialize = DefaultSerialize(modifyData);

			var post = await HttpWebRequestHandlers.PerformPatchAsync(BASE_URI, "channels", $"?broadcaster_id={User_Id}", serialize, BuildDefaultHeaders());
		}

		/// <summary>
		/// Finds games by their Twitch game IDs
		/// </summary>
		/// <param name="ids">Twitch IDs</param>
		/// <returns>Found games</returns>
		public async Task<Response_GetGames[]> GetGamesByID(params string[] ids)
		{
			if (ids.Length == 0)
				return null;
			else if (ids.Length > 100)
				return null;

			StringBuilder sb = new StringBuilder();
			foreach (var id in ids)
			{
				if (sb.Length == 0)
					sb.Append($"?id={id}");
				else
					sb.Append($"&id={id}");
			}

			var response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "games", sb.ToString(), BuildDefaultHeaders());
			if (string.IsNullOrEmpty(response))
				return null;

			var deserialize = JsonConvert.DeserializeObject<JToken>(response);
			if (deserialize["data"] == null)
				return null;

			return deserialize["data"].ToObject<Response_GetGames[]>();
		}

		/// <summary>
		/// Finds games on Twitch with given names
		/// </summary>
		/// <param name="names">Names</param>
		/// <returns>Found games</returns>
		public async Task<Response_GetGames[]> GetGamesByName(params string[] names)
		{
			if (names.Length == 0)
				return null;
			else if (names.Length > 100)
				return null;

			StringBuilder sb = new();
			foreach (var name in names)
			{
				if (sb.Length == 0)
					sb.Append($"?name={name}");
				else
					sb.Append($"&name={name}");
			}

			var response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "games", sb.ToString(), BuildDefaultHeaders());
			if (string.IsNullOrEmpty(response))
				return null;

			var deserialize = JsonConvert.DeserializeObject<JToken>(response);
			if (deserialize["data"] == null)
				return null;

			return deserialize["data"].ToObject<Response_GetGames[]>();
		}

		/// <summary>
		/// Finds games on Twitch by their IGDB IDs
		/// </summary>
		/// <param name="names">IGDB IDs</param>
		/// <returns>Found games</returns>
		public async Task<Response_GetGames[]> GetGamesByIgdbId(params string[] ids)
		{
			if (ids.Length == 0)
				return null;
			else if (ids.Length > 100)
				return null;

			StringBuilder sb = new StringBuilder();
			foreach (var id in ids)
			{
				if (sb.Length == 0)
					sb.Append($"?igdb_id={id}");
				else
					sb.Append($"&igdb_id={id}");
			}

			var response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "games", sb.ToString(), BuildDefaultHeaders());
			if (string.IsNullOrEmpty(response))
				return null;

			var deserialize = JsonConvert.DeserializeObject<JToken>(response);
			if (deserialize["data"] == null)
				return null;

			return deserialize["data"].ToObject<Response_GetGames[]>();
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

		public async Task<Response_SharedSession> GetChatSharedSession(string channelID)
		{
			var result = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "shared_chat/session", $"?broadcaster_id={channelID}", BuildDefaultHeaders());
			if(result != "")
			{
				var deserialize = JsonConvert.DeserializeObject<JToken>(result);
				if (deserialize["data"] != null)
				{
					var node = deserialize["data"].ToString();
					var content = deserialize["data"].ToObject<Response_SharedSession[]>();
					return content.FirstOrDefault();
				}
				else
					return null;
			}
			else
			{
				ErrorLoggingSocket.WriteLine($"Error checking shared chat state for {channelID}");
				return null;
			}
		}

		/// <summary>
		/// Removes a given message (if possible)
		/// </summary>
		/// <param name="messageID">Message to delete</param>
		public void RequestRemoveMessage(ES_ChatMessage messageID)
		{
			if (!Scopes.Contains("moderator:manage:chat_messages"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:chat_messages");
				return;
			}

			Task.Run(async () =>
			{
				try
				{
					_ = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "moderation/chat", $"?broadcaster_id={messageID.broadcaster_user_id}&moderator_id={User_Id}&message_id={messageID}", BuildDefaultHeaders());
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
			if (!Scopes.Contains("moderator:manage:banned_users"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:banned_users ");
				return;
			}

			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());

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
			if (!Scopes.Contains("moderator:manage:banned_users"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:banned_users ");
				return;
			}

			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, (int)length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());
			});
		}

		public void RequestTimeout(string broadcaster_id, string chatter_user_id, uint length, string reason)
		{
			if (!Scopes.Contains("moderator:manage:banned_users"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:banned_users ");
				return;
			}

			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(chatter_user_id, (int)length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={broadcaster_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());
			});
		}

		/// <summary>
		/// Bans a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to ban</param>
		/// <param name="reason">Optimal reason for a ban - can be null</param>
		public void RequestBan(ES_ChatMessage message, string reason)
		{
			if (!Scopes.Contains("moderator:manage:banned_users"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:banned_users ");
				return;
			}

			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(message.chatter_user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={message.broadcaster_user_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());

			});
		}

		/// <summary>
		/// Bans a user based on a message
		/// </summary>
		/// <param name="message">Message based on which to ban</param>
		/// <param name="reason">Optimal reason for a ban - can be null</param>
		public void RequestBan(string broadcaster_id, string chatter_user_id, string reason)
		{
			if (!Scopes.Contains("moderator:manage:banned_users"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:banned_users ");
				return;
			}

			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(chatter_user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={broadcaster_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());
			});
		}

		/// <summary>
		/// Unbans a user
		/// </summary>
		/// <param name="broadcaster_id">Channel in which to unban</param>
		/// <param name="chatter_id">User id</param>
		public void RequestUnban(string broadcaster_id, string chatter_id)
		{
			Task.Run(async () =>
			{
				var result = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "moderation/bans", $"?broadcaster_id={broadcaster_id}&moderator_id={User_Id}&user_id={chatter_id}", BuildDefaultHeaders());
			});
		}

		/// <summary>
		/// Searches for a category (game) on Twitch
		/// </summary>
		/// <param name="search">What to look for</param>
		/// <param name="first">How many results</param>
		/// <param name="afterCursor">After cursor - currently not implemented</param>
		/// <returns>Array of Response_SearchCategory[] objects</returns>
		public async Task<Response_SearchCategory[]> SearchCategory(string search, uint first = 20, string afterCursor = null)
		{
			if (first > 100)
				first = 100;
			var unescape = Uri.UnescapeDataString(search);

			string response;
			if (afterCursor == null)
				response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "search/categories", $"?query={unescape}&first={first}", BuildDefaultHeaders());
			else
				response = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "search/categories", $"?query={unescape}&first{first}&after={afterCursor}", BuildDefaultHeaders());


			if (string.IsNullOrEmpty(response))
				return null;

			var data = JsonConvert.DeserializeObject<JToken>(response);
			if (data == null || data["data"] == null)
				return null;

			return data["data"].ToObject<Response_SearchCategory[]>();
		}

		/// <summary>
		/// Creates a stream marker in user's channel
		/// </summary>
		/// <param name="description">Optional description text (use null is none) - can't be longer than 140 characters</param>
		/// <returns>True/False based on whatever it was successful</returns>
		public async Task<bool> CreateStreamMarker(string description)
		{
			if (!Scopes.Contains("channel:manage:broadcast"))
			{
				ErrorLoggingSocket.WriteLine("Client doesn't have a scope channel:manage:broadcast");
				return false;
			}

			if (description != null && description.Length > 140)
				description = description.Substring(0, 140);

			var obj = new Request_CreateStreamMarker()
			{
				user_id = User_Id,
				description = description
			};

			var serialize = DefaultSerialize(obj);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "streams/markers", "", serialize, BuildDefaultHeaders());
			if (string.IsNullOrEmpty(result))
				return false;

			var deserializeResponse = JsonConvert.DeserializeObject<JToken>(result);
			if (deserializeResponse["data"]?.Count() > 0)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Gets user information based on Twitch login name
		/// </summary>
		/// <param name="twitchLogin">Twitch login name</param>
		/// <returns>Response_GetUserInfo object or null</returns>
		public async Task<Response_GetUserInfo> GetUserInfoByUserLogin(string twitchLogin)
		{
			if (UserNameToInfo.TryGetValue(twitchLogin, out Response_GetUserInfo userInfo))
				return userInfo;
			else
			{
				var result = await HttpWebRequestHandlers.PerformGetAsync("https://api.twitch.tv/helix/", "users", $"?login={twitchLogin}", BuildDefaultHeaders());
				if (!string.IsNullOrEmpty(result))
				{
					var response = JObject.Parse(result);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						userInfo = response["data"].First.ToObject<Response_GetUserInfo>();
						UserNameToInfo.Add(twitchLogin, userInfo);
						return userInfo;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets user information based on Twitch user id
		/// </summary>
		/// <param name="userID">Twitch login name</param>
		/// <returns>Response_GetUserInfo object or null</returns>
		public async Task<Response_GetUserInfo> GetUserInfoByUserID(string userID)
		{
			var result = await HttpWebRequestHandlers.PerformGetAsync("https://api.twitch.tv/helix/", "users", $"?id={userID}", BuildDefaultHeaders());
			if (!string.IsNullOrEmpty(result))
			{
				var response = JObject.Parse(result);
				if (response["data"] != null && response["data"].Children().Count() > 0)
				{
					var userInfo = response["data"].First.ToObject<Response_GetUserInfo>();
					return userInfo;
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
		/// Sends a message in a chat for a specific channel
		/// </summary>
		/// <param name="instance">Instance of a channel (IChannelInstance)</param>
		/// <param name="text">Text to send in chat</param>
		public async Task SendMessageAsync(IChannelInstance instance, string text)
		{
			if (!Scopes.Contains("user:write:chat"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have user:write:chat");
				return;
			}

			var content = Request_SendChatMessage.CreateMessage(instance.ChannelID, User_Id.ToString(), text);
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
			if (!Scopes.Contains("user:write:chat"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have user:write:chat");
				return;
			}

			var content = Request_SendChatMessage.CreateResponse(messageToRespondTo.broadcaster_user_id.ToString(), User_Id.ToString(), messageToRespondTo.message_id, message);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());
		}

		public async Task SendResponseAsync(string broadcaster_id, string message_id, string message)
		{
			if (!Scopes.Contains("user:write:chat"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have user:write:chat");
				return;
			}

			var content = Request_SendChatMessage.CreateResponse(broadcaster_id, User_Id.ToString(), message_id, message);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());
		}

		/// <summary>
		/// Sends an announcement
		/// </summary>
		/// <param name="broadcaster_id">Where to send an announcement</param>
		/// <param name="message">Content of an announcement message (max 500 characters!)</param>
		/// <param name="color">Color (optional)</param>
		/// <returns>Task</returns>
		public async Task SendAnnouncement(string broadcaster_id, string message, Request_Announcement.Color color = Request_Announcement.Color.primary)
		{
			if (!Scopes.Contains("moderator:manage:announcements"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:announcements");
				return;
			}

			var content = Request_Announcement.CreateAnnouncement(message, color);
			var serialize = DefaultSerialize(content);

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/announcements", $"?broadcaster_id={broadcaster_id}&moderator_id={User_Id}", serialize, BuildDefaultHeaders());
		}

		/// <summary>
		/// Sends a shoutout (note that this can be done at most every 2 minutes, with 60 min cooldown for the same broadcaster being shoutout)
		/// </summary>
		/// <param name="channel_id">Channel in which to send a shoutout</param>
		/// <param name="target_channel_id">Which cannel to shoutout</param>
		/// <returns>Task without a result</returns>
		public async Task SendShoutout(string channel_id, string target_channel_id)
		{
			if (!Scopes.Contains("moderator:manage:shoutouts"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have moderator:manage:shoutouts");
				return;
			}

			var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/shoutouts", $"?from_broadcaster_id={channel_id}&to_broadcaster_id={target_channel_id}&moderator_id={User_Id}", "", BuildDefaultHeaders());
		}

		/// <summary>
		/// Gets the list of current subscriptions - this should be done when connecting or disconnecting with a websocket and to 
		/// </summary>
		/// <returns>Response_SubscribeTo object containing information about your current and previous subscriptions including costs</returns>
		public async Task<Response_SubscribeTo> GetCurrentEventSubscriptions()
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
			if (!Scopes.Contains("channel:manage:redemptions"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have channel:manage:redemptions");
				return;
			}

			Task.Run(async () =>
			{
				if (fullfilmentStatus != ES_ChannelPoints.RedemptionStates.UNFULFILLED)
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
			if (!Scopes.Contains("channel:manage:redemptions"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have channel:manage:redemptions");
				return false;
			}

			var rewardsRequest = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={User_Id}&only_manageable_rewards=true", BuildDefaultHeaders());
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
			if (!Scopes.Contains("channel:manage:redemptions"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have channel:manage:redemptions");
				return false;
			}

			var result = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={reward.broadcaster_id}&id={reward.id}", BuildDefaultHeaders());
			if (result != null)
				return true;
			else
				return false;
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
			if (!Scopes.Contains("channel:manage:redemptions"))
			{
				ErrorLoggingSocket.WriteLine("Can't perform - client doesn't have channel:manage:redemptions");
				return null;
			}

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
				var post = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={User_Id}", serialize, BuildDefaultHeaders());
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

					var patch = await HttpWebRequestHandlers.PerformPatchAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={User_Id}&id={foundReward.id}", serialize, BuildDefaultHeaders());
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

		public async Task<Response_Emote> GetAvailableEmotes(string broadcasterID, string cursor = null)
		{
			string get;
			if (cursor == null)
				get = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "chat/emotes/user", $"?user_id={this.User_Id}&broadcaster_id={broadcasterID}&first=50", BuildDefaultHeaders());
			else
				get = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "chat/emotes/user", $"?user_id={this.User_Id}&broadcaster_id={broadcasterID}&after={cursor}", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(get))
				return null;
			else
			{
				return JsonConvert.DeserializeObject<Response_Emote>(get);
			}
		}


	}
}
