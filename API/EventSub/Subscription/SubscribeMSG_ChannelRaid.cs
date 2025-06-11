namespace SuiBot_TwitchSocket.API.EventSub.Subscription
{
	public class SubscribeMSG_ChannelRaid
	{
		public string type = "channel.raid";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		internal SubscribeMSG_ChannelRaid() { }

		public SubscribeMSG_ChannelRaid(string channelId, string sessionID)
		{
			condition = ES_Subscribe_Condition_Variant_FromTo.CreateToBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
