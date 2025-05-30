using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using SuiBot_TwitchSocket.API.EventSub;

namespace SuiBot_TwitchSocket.API.Helix.Request
{
	public class Request_UpdateChannelPointsRedemptionStatus
	{
		[JsonConverter(typeof(StringEnumConverter))] public ES_ChannelPoints.RedemptionStates status;
	
		public static Request_UpdateChannelPointsRedemptionStatus UpdateWith(ES_ChannelPoints.RedemptionStates status)
		{
			return new Request_UpdateChannelPointsRedemptionStatus()
			{
				status = status
			};
		}
	}
}
