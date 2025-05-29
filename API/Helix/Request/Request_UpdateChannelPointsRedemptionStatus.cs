using SuiBot_TwitchSocket.API.EventSub;

namespace SuiBot_TwitchSocket.API.Helix.Request
{
	public class Request_UpdateChannelPointsRedemptionStatus
	{
		public RedemptionStatus Data;
		
		public static Request_UpdateChannelPointsRedemptionStatus UpdateWith(ES_ChannelPoints.RedemptionStates status)
		{
			return new Request_UpdateChannelPointsRedemptionStatus()
			{
				Data = new RedemptionStatus()
				{
					status = status
				}
			};
		}


		public class RedemptionStatus
		{
			public ES_ChannelPoints.RedemptionStates status;
		}
	}
}
