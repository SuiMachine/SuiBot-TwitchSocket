namespace SuiBot_TwitchSocket.API.EventSub.Subscription
{
	public class SubscribeMSG_ChannelGoalEnd
	{
		public string type = "channel.goal.end";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		internal SubscribeMSG_ChannelGoalEnd() { }

		public SubscribeMSG_ChannelGoalEnd(string channelId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
