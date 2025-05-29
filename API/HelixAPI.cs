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
using System.Net;
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

		public Response_ChannelPointInformation[] RewardsCache { get; private set; }
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		public string BotLoginName { get; private set; }
		public string BotUserId { get; private set; } //This should be string :(
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

		private async Task<Response_GetUserInfo> GetUserInfo(string userName)
		{
			if (UserNameToInfo.TryGetValue(userName, out Response_GetUserInfo userId))
				return userId;
			else
			{
				var result = await HttpWebRequestHandlers.PerformGetAsync("https://api.twitch.tv/helix/", $"users?login={userName}", "", BuildDefaultHeaders());
				if (!string.IsNullOrEmpty(result))
				{
					var response = JObject.Parse(result);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						var userInfo = response["data"].First.ToObject<Response_GetUserInfo>();
						UserNameToInfo.Add(userName, userInfo);
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

		public async Task<Subscription_Response_Data> SubscribeToChatMessage(string channel, string sessionId)
		{
			Response_GetUserInfo channelInfo = await GetUserInfo(channel);
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

		//Too much code repetition - should at least be seperate
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


		public void SendMessage(IChannelInstance instance, string text)
		{
			Task.Run(async () =>
			{
				var content = Request_SendChatMessage.CreateMessage(instance.ChannelID, BotUserId.ToString(), text);
				var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());

			});
		}

		public void SendResponse(ES_ChatMessage messageToRespondTo, string message)
		{
			Task.Run(async () =>
			{
				var content = Request_SendChatMessage.CreateResponse(messageToRespondTo.broadcaster_user_id.ToString(), BotUserId.ToString(), messageToRespondTo.message_id, message);
				var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());

			});
		}

		public void RequestShoutout(ES_ChatMessage lastMessage, string username)
		{
			Task.Run(async () =>
			{

			});
		}

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

		public async Task CloseSubscription(Subscription_Response_Data subscription)
		{
			await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "eventsub/subscriptions", $"?id={subscription.id}", BuildDefaultHeaders());
		}

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

				var pathRequest = HttpWebRequestHandlers.PerformPatchAsync(BASE_URI, "channel_points/custom_rewards/redemptions", $"?id={redeem.id}&broadcaster_id={redeem.broadcaster_user_id}&reward_id={redeem.reward.id}", serialize, BuildDefaultHeaders()); ;
			});

		}

		public async Task<bool> CreateRewardsCache()
		{
			var rewardsRequest = await HttpWebRequestHandlers.PerformGetAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={BotUserId}&only_manageable_rewards=true", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(rewardsRequest))
				throw new Exception("Failed to get rewards");

			var deserialize = (JToken)JsonConvert.DeserializeObject(rewardsRequest);
			if (deserialize["data"] != null)
			{
				RewardsCache = deserialize["data"].ToObject<Response_ChannelPointInformation[]>();
				return true;
			}
			else
				return false;
		}

		public static string GenerateAuthenticationURL(string client_id, string callbackAddress, string[] scopes)
		{
			var url = new Uri($"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={callbackAddress}&response_type=token&scope={string.Join(" ", scopes)}");

			return url.AbsoluteUri;
		}

		public async Task<Response_ChannelPointInformation> CreateOrUpdateReward(string rewardID, string rewardTitle, string rewardDescription, int rewardCost, int rewardCooldown, bool isEnabled, bool isUserInputRequired)
		{
			if (RewardsCache == null)
			{
				if (!await CreateRewardsCache())
					throw new Exception("Failed to download cache");
			}

			Response_ChannelPointInformation foundReward = RewardsCache.FirstOrDefault(x => x.id == rewardID);
			if(foundReward == null)
			{
				//Create reward
				var newReward = new Response_ChannelPointInformation()
				{
					title = rewardTitle,
					prompt = rewardDescription,
					cost = rewardCost,
					global_cooldown_setting = new Response_ChannelPointInformation.GlobalCooldownSetting()
					{
						is_enabled = rewardCooldown > 0,
						global_cooldown_seconds = rewardCooldown
					},
					is_enabled = isEnabled,
					is_user_input_required = isUserInputRequired,
				};
				var serialize = JsonConvert.SerializeObject(foundReward, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});
				var patch = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "channel_points/custom_rewards", $"?broadcaster_id={BotUserId}&id={foundReward.id}", serialize, BuildDefaultHeaders());
				await Task.Delay(2000);
				return newReward;
			}
			else
			{
				bool cooldownEnabled = rewardCooldown > 0;

				//Update reward
				if (foundReward.is_enabled != isEnabled || foundReward.title != rewardTitle || foundReward.prompt != rewardDescription || foundReward.cost != rewardCost || foundReward.global_cooldown_setting.is_enabled != cooldownEnabled || foundReward.global_cooldown_setting.global_cooldown_seconds == rewardCooldown)
				{
					foundReward.is_enabled = isEnabled;
					foundReward.title = rewardTitle;
					foundReward.prompt = rewardTitle;
					foundReward.cost = rewardCost;
					foundReward.global_cooldown_setting.is_enabled = cooldownEnabled;
					foundReward.global_cooldown_setting.global_cooldown_seconds = rewardCooldown;
					foundReward.is_user_input_required = isUserInputRequired;

					var serialize = JsonConvert.SerializeObject(foundReward, Formatting.Indented, new JsonSerializerSettings()
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

					for(int i=0; i<RewardsCache.Length; i++)
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
	}
}
