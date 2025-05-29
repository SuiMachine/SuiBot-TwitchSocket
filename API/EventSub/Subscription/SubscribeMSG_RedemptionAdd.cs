namespace SuiBot_TwitchSocket.API.EventSub.Subscription
{
	internal class SubscribeMSG_RedemptionAdd
	{
		public string type = "channel.channel_points_custom_reward_redemption.add";
		public int version = 1;
		public ES_Subscribe_Condition condition = null;
		public ES_Subscribe_Transport_Websocket transport = null;

		public SubscribeMSG_RedemptionAdd() { }

		public SubscribeMSG_RedemptionAdd(string broadcasterID, string sessionId)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(broadcasterID);
			transport = new ES_Subscribe_Transport_Websocket(sessionId);
		}
	}
}
