namespace SuiBot_TwitchSocket.API.EventSub.Subscription
{
	internal class SubscribeMSG_SharedChatBegin
	{
		public string type = "channel.shared_chat.begin";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_SharedChatBegin() { }

		public SubscribeMSG_SharedChatBegin(string channelId, string bot_Id, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}

	internal class SubscribeMSG_SharedChatUpdate
	{
		public string type = "channel.shared_chat.update";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_SharedChatUpdate() { }

		public SubscribeMSG_SharedChatUpdate(string channelId, string bot_Id, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}

	internal class SubscribeMSG_SharedChatEnd
	{
		public string type = "channel.shared_chat.end";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_SharedChatEnd() { }

		public SubscribeMSG_SharedChatEnd(string channelId, string bot_Id, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
