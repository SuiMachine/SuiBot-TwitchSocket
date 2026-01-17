using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Websocket.Client;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChannelPoints;

namespace SuiBot_TwitchSocket
{
	public class TwitchSocket
	{
#if LOCAL_API
		private const string WEBSOCKET_BASE_URI = "ws://127.0.0.1:8080/ws";
#else
		private const string WEBSOCKET_BASE_URI = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";
#endif

		private int Reconnect_Failures = 0;

		private IBotInstance BotInstance;

		public TwitchSocket(IBotInstance botInstance)
		{
			this.BotInstance = botInstance;
			CreateSessionAndSocket(0);
		}

		private Task SubscribingTask;
		private volatile bool m_Connected;
		private volatile bool m_Connecting;

		public string SessionID { get; private set; }
		public bool Connected => m_Connected;
		public bool Connecting => m_Connecting;
		public volatile bool AutoReconnect;
		public DateTime LastMessageAt { get; private set; }
		public WebsocketClient Socket { get; private set; }
		public WebsocketClient Secondary_Socket { get; private set; }

		private System.Timers.Timer DelayConnectionTimer;
		private System.Timers.Timer KeepAliveCheck;
		private System.Timers.Timer m_Temp_AdBreakEnd;

		private void CreateSessionAndSocket(int delay)
		{
			if (m_Connected)
				BotInstance?.TwitchSocket_Disconnected();
			m_Connected = false;
			m_Connecting = true;

			Socket = new WebsocketClient(new Uri(WEBSOCKET_BASE_URI));
			Socket.IsReconnectionEnabled = false; //Twitch doesn't allow for normal reconnects!

#if !LOCAL_API
			//Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
#endif

			Socket.ReconnectionHappened.Subscribe(info => Socket_Reconnected(Socket, info));
			Socket.MessageReceived.Subscribe(msg => Socket_OnMessage(Socket, msg));
			Socket.DisconnectionHappened.Subscribe(disconnectMsg => Socket_OnClose(Socket, disconnectMsg));

			DelayConnectionTimer?.Dispose();

			if (delay <= 0)
				delay = 1;

			if (Reconnect_Failures > 0)
			{
				ErrorLoggingSocket.WriteLine($"Reconnect number {0}");
			}

			Reconnect_Failures++;

			DelayConnectionTimer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = delay,
				Enabled = true
			};
			DelayConnectionTimer.Elapsed += ((sender, e) =>
			{
				Task.Run(async () =>
				{
					await Socket.Start();
				});
				if (DelayConnectionTimer == sender)
				{
					DelayConnectionTimer.Dispose();
					DelayConnectionTimer = null;
				}
			});
		}

		private void ReconnectWithUrl(string reconnect_url)
		{
			//rewrite this - it needs 2 sockets running and checking receiver
			if (Secondary_Socket != null)
			{
				Secondary_Socket.Dispose();
			}


			var newSocket = new WebsocketClient(new Uri(reconnect_url))
			{
				IsReconnectionEnabled = false
			};

			this.AutoReconnect = true;
			newSocket.DisconnectionHappened.Subscribe(disconnectMsg => Socket_OnClose(newSocket, disconnectMsg));
			newSocket.MessageReceived.Subscribe(msg => Socket_OnMessage(newSocket, msg));
			newSocket.ReconnectionHappened.Subscribe(msg => Socket_Reconnected(newSocket, msg));
			Secondary_Socket = newSocket;

			Task.Run(async () =>
			{
				await newSocket.Start();
			});
		}

		private void Socket_Reconnected(WebsocketClient sender, ReconnectionInfo info)
		{
			switch (info.Type)
			{
				case ReconnectionType.Initial:
					Socket_OnOpen(sender);
					break;
				default:
					Socket_OnOpen(sender);
					ErrorLoggingSocket.WriteLine($"Reconnection happend of type {info.Type}");
					break;
			}
		}

		private void Socket_OnOpen(WebsocketClient sender)
		{
			if (sender == Socket)
			{
				m_Connected = true;
				m_Connecting = false;
				ErrorLoggingSocket.WriteLine("Opened Twitch socket");
				KeepAliveCheck = new System.Timers.Timer(5 * 1000);
				KeepAliveCheck.Elapsed += KeepAliveCheck_Elapsed;
				KeepAliveCheck.Start();
			}
			else if(sender == Secondary_Socket)
			{
				m_Connected = true;
				m_Connecting = false;
				ErrorLoggingSocket.WriteLine("Secondary socket opened");
			}
			else
			{

			}
		}

		private void KeepAliveCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var currentTime = DateTime.UtcNow;
			if (LastMessageAt + TimeSpan.FromSeconds(45) < currentTime)
				Socket.Stop(WebSocketCloseStatus.EndpointUnavailable, "KeepAlive failed");
		}

		private void Socket_OnClose(WebsocketClient socketToClose, DisconnectionInfo e)
		{
			if(socketToClose == Secondary_Socket)
			{
				ErrorLoggingSocket.WriteLine($"Secondary socket closed with code {e.Type} - {e.Exception?.Message ?? "None"}");
				return;
			}
			else if (socketToClose != Socket)
			{
				ErrorLoggingSocket.WriteLine($"Some unknown socket closed with {e.Type} - {e.Exception?.Message ?? "None"}");
				return;
			}

			EventSubClose_Code closeType = e.CloseStatus == null ? EventSubClose_Code.INVALID : (EventSubClose_Code)e.CloseStatus;
			m_Connected = false;
			m_Connecting = false;
			KeepAliveCheck?.Stop();
			ErrorLoggingSocket.WriteLine($"Closed due to code {e.CloseStatus} - {e?.Exception?.Message ?? "None"}");

			if (Reconnect_Failures > 100)
			{
				AutoReconnect = false;
				ErrorLoggingSocket.WriteLine("Reached limit of reconnect attempts");
				BotInstance.TwitchSocket_ClosedViaSocket();
			}

			if (AutoReconnect)
			{
				int delay = 0;
				switch ((ushort)closeType)
				{
					case (ushort)WebSocketCloseStatus.NormalClosure:
						delay = 0;
						break;
					case (ushort)WebSocketCloseStatus.EndpointUnavailable:
						delay = 60_000;
						break;
					case 4000: //Internal server error
					case 4005: //Network timeout
					case 4006: //Network error
					case (ushort)WebSocketCloseStatus.ProtocolError:
					//case (ushort)WebSocketCloseStatus.Undefined:
					//case (ushort)WebSocketCloseStatus.Abnormal:
					case (ushort)WebSocketCloseStatus.MessageTooBig:
					case (ushort)WebSocketCloseStatus.InternalServerError:
						//case (ushort)CloseStatusCode.TlsHandshakeFailure:
						delay = 60_000;
						break;
					case (ushort)WebSocketCloseStatus.InvalidPayloadData:
					case (ushort)WebSocketCloseStatus.PolicyViolation:
					case (ushort)WebSocketCloseStatus.MandatoryExtension:
						AutoReconnect = false;
						BotInstance?.TwitchSocket_ClosedViaSocket();
						return;
					case 4002:
						ErrorLoggingSocket.WriteLine("Failed ping-pong!");
						delay = 60_000;
						AutoReconnect = true;
						break;
					case 4003:
						ErrorLoggingSocket.WriteLine("Connection unused - no subscriptions!");
						AutoReconnect = false;
						return;
					case 4004: //Reconnect grace time expired
						ErrorLoggingSocket.WriteLine("Grace period expired!");
						AutoReconnect = false;
						BotInstance?.TwitchSocket_ClosedViaSocket();
						return;
					case 4007: //Invalid reconnect
						AutoReconnect = false;
						BotInstance?.TwitchSocket_ClosedViaSocket();
						return;
					case 4001:
						AutoReconnect = false;
						ErrorLoggingSocket.WriteLine("Data was send via websocket! THIS IS SO WRONG");
						BotInstance?.TwitchSocket_ClosedViaSocket();
						return;
					case (ushort)WebSocketCloseStatus.Empty:
					default:
						delay = 10_000;
						break;
				}

				CreateSessionAndSocket(delay);
			}
			else
			{
				Socket = null;

				if (BotInstance != null)
				{
					BotInstance.TwitchSocket_ClosedViaSocket();
				}
			}
		}

		private void Socket_OnMessage(WebsocketClient sourceSocket, ResponseMessage e)
		{
			if (sourceSocket == Socket)
			{
				var message = JsonConvert.DeserializeObject<ES_ServerMessage>(e.Text);
				if (message == null)
				{
					//Should be ping?
					return;
				}

				LastMessageAt = message.metadata.message_timestamp;
				switch (message.metadata.message_type)
				{
					case EventSub_MessageType.session_welcome:
						ProcessWelcome(message.payload, sourceSocket);
						break;
					case EventSub_MessageType.session_keepalive:
						break;
					case EventSub_MessageType.notification:
						ProcessNotification(message);
						break;
					case EventSub_MessageType.session_reconnect:
						ProcessReconnect(message.payload);
						break;
					default:
						Debug.WriteLine($"Unhandled message: {message}");
						break;
				}
			}
			else
			{
				var message = JsonConvert.DeserializeObject<ES_ServerMessage>(e.Text);
				if (message == null)
				{
					//Socket.Ping();
					return;
				}

				LastMessageAt = message.metadata.message_timestamp;
				switch (message.metadata.message_type)
				{
					case EventSub_MessageType.session_welcome:
						ProcessWelcome(message.payload, sourceSocket);
						break;
					default:
						Debug.WriteLine($"Unhandled message: {message}");
						break;
				}
			}
		}

		private void ProcessReconnect(JToken payload)
		{
			var sessionField = payload["session"];
			if (sessionField == null)
			{
				ErrorLoggingSocket.WriteLine($"Something when wrong with reconnect, debug this message:\n{payload}");
				return;
			}

			var reconnect = sessionField.ToObject<ES_ReconnectSession>();
			if (reconnect == null)
			{
				ErrorLoggingSocket.WriteLine($"Something went wrong with reconnect, debug this message:\n{sessionField}");
				return;
			}

			if (reconnect.id != SessionID)
			{
				ErrorLoggingSocket.WriteLine("Wrong session ID?!");
				return;
			}

			ErrorLoggingSocket.WriteLine($"Received reconnect with: {reconnect.reconnect_url}");
			this.ReconnectWithUrl(reconnect.reconnect_url);
		}

		private void ProcessNotification(ES_ServerMessage message)
		{
			switch (message.metadata.subscription_type)
			{
				case null:
					return;
				case "channel.chat.message":
					ProcessChatMessage(message.payload);
					return;
				case "channel.channel_points_custom_reward_redemption.add":
					ProcessChannelRedeem(message.payload);
					return;
				case "stream.online":
					ProcessStreamOnline(message.payload);
					return;
				case "stream.offline":
					ProcessStreamOffline(message.payload);
					return;
				case "automod.message.hold":
					ProcessAutomodMessageHold(message.payload);
					return;
				case "channel.suspicious_user.message":
					ProcessSuspiciousUserMessage(message.payload);
					return;
				case "channel.goal.end":
					ProcessChannelGoalEnd(message.payload);
					return;
				case "channel.ad_break.begin":
					ProcessAdBreakBegin(message.payload);
					return;
				case "channel.raid":
					ProcessChannelRaid(message.payload);
					return;
				case "channel.shared_chat.begin":
					ProcessSharedChatBegin(message.payload);
					return;
				case "channel.shared_chat.update":
					ProcessSharedChatUpdate(message.payload);
					return;
				case "channel.shared_chat.end":
					ProcessSharedChatEnd(message.payload);
					return;
				default:
					Console.WriteLine($"Unhandled message type: {message.metadata.subscription_type}");
					return;
			}
		}

		private void ProcessChannelRaid(JToken payload)
		{
			if (payload["event"] == null)
				return;

			var obj = payload["event"].ToObject<ES_ChannelRaid>();
			if (obj == null)
				return;

			BotInstance?.TwitchSocket_ChannelRaid(obj);
		}

		private void ProcessAdBreakBegin(JToken payload)
		{
			if (payload["event"] == null)
				return;

			var obj = payload["event"].ToObject<ES_AdBreakBeginNotification>();
			BotInstance.TwitchSocket_AdBreakBegin(obj);
			if (obj == null)
				return;

			if (m_Temp_AdBreakEnd != null)
				m_Temp_AdBreakEnd.Dispose();
			m_Temp_AdBreakEnd = new System.Timers.Timer(obj.duration_seconds * 1000) { AutoReset = false };

			m_Temp_AdBreakEnd.Elapsed += ((object sender, System.Timers.ElapsedEventArgs e) =>
			{
				BotInstance?.TwitchSocket_AdBreakFinished(obj);
				m_Temp_AdBreakEnd.Dispose();
				m_Temp_AdBreakEnd = null;
			});
			m_Temp_AdBreakEnd.Start();
		}

		private void ProcessChatMessage(JToken payload)
		{
			var eventText = payload["event"];

			var msg = eventText.ToObject<ES_ChatMessage>();

			//"text"
			//"channel_points_highlighted"
			//"channel_points_sub_only"
			//"power_ups_message_effect"
			//"power_ups_gigantified_emote"
			//"user_intro"
			if (msg.message_type == "user_intro")
			{
				var dbg = eventText.ToString();
				ErrorLoggingSocket.WriteLine($"Verify this potential first message:\n{dbg}");
			}

			if (BotInstance.GetChannelInstanceUsingLogin(msg.broadcaster_user_login, out IChannelInstance instance))
				instance = null; //Not needed, but makes VS shutup
			msg.SetupRole(instance);
			BotInstance.TwitchSocket_ChatMessage(msg);
		}

		private void ProcessChannelRedeem(JToken payload)
		{
			if (payload["event"] == null)
				return;
			//var content = payload.ToString();
			ES_ChannelPointRedeemRequest obj = payload["event"]?.ToObject<ES_ChannelPointRedeemRequest>();
			BotInstance?.TwitchSocket_ChannelPointsRedeem(obj);
		}

		private void ProcessChannelGoalEnd(JToken payload)
		{
			if (payload["event"] == null)
				return;

			ES_ChannelGoal obj = payload["event"]?.ToObject<ES_ChannelGoal>();
			if (obj != null)
				BotInstance?.TwitchSocket_OnChannelGoalEnd(obj);
		}

		private void ProcessStreamOnline(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_StreamOnline>();
			if (msg == null)
				return;

			BotInstance?.TwitchSocket_StreamWentOnline(msg);
		}

		private void ProcessStreamOffline(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			//var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_StreamOffline>();
			if (msg == null)
				return;

			BotInstance?.TwitchSocket_StreamWentOffline(msg);
		}

		private void ProcessAutomodMessageHold(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			//var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_AutomodMessageHold>();
			if (msg == null)
				return;

			BotInstance?.TwitchSocket_AutoModMessageHold(msg);
		}

		private void ProcessSuspiciousUserMessage(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			//var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_Suspicious_UserMessage>();
			if (msg == null)
				return;

			BotInstance?.TwitchSocket_SuspiciousMessageReceived(msg);
		}

		private void ProcessWelcome(JToken payload, WebsocketClient socket)
		{
			var content = payload["session"].ToObject<ES_SessionMessage>();
			if (socket == Socket)
			{
				ErrorLoggingSocket.WriteLine("Processing welcome msg on primary soecket...");
				SessionID = content.id;
				AutoReconnect = true;
				BotInstance?.TwitchSocket_Connected();
				Reconnect_Failures = 0;
			}
			else if (socket == Secondary_Socket)
			{
				SessionID = content.id;
				ErrorLoggingSocket.WriteLine("Closing primary socket and swapping secondary to be primary!");
				Socket.Stop(WebSocketCloseStatus.NormalClosure, "Closing primary socket and swapping secondary to be primary!");
				Socket = socket;
				AutoReconnect = true;
			}
			else
			{
				ErrorLoggingSocket.WriteLine("Socket that processed a welcome wasn't either a primary or secondary - so something is wrong in code!");
				socket.Dispose();
			}
		}

		private void ProcessSharedChatBegin(JToken payload)
		{
			if (payload["event"] == null)
				return;

			var obj = payload["event"].ToObject<ES_SharedChatBegin>();
			if (obj == null)
				return;

			BotInstance?.TwitchSocket_SharedChatBegin(obj);
		}

		private void ProcessSharedChatUpdate(JToken payload)
		{
			if (payload["event"] == null)
				return;

			var obj = payload["event"].ToObject<ES_SharedChatUpdate>();
			if (obj == null)
				return;

			BotInstance?.TwitchSocket_SharedChatUpdate(obj);
		}

		private void ProcessSharedChatEnd(JToken payload)
		{
			if (payload["event"] == null)
				return;

			var obj = payload["event"].ToObject<ES_SharedChatEnd>();
			if (obj == null)
				return;

			BotInstance?.TwitchSocket_SharedChatEnd(obj);
		}

		internal void Close()
		{
			AutoReconnect = false;
			Socket?.Stop(WebSocketCloseStatus.NormalClosure, "Intended Closure");
			DelayConnectionTimer?.Dispose();
		}
	}
}
