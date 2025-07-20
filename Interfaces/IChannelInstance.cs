using SuiBot_TwitchSocket.API.EventSub;
using SuiBot_TwitchSocket.API.Helix.Responses;
using System;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChannelPoints;

namespace SuiBot_TwitchSocket.Interfaces
{
	public interface IBotInstance: IDisposable
	{
		bool ShouldRun { get; set; }
		bool IsDisposed { get; }
		bool GetChannelInstanceUsingLogin(string login, out IChannelInstance channel);
		void TwitchSocket_Connected();
		void TwitchSocket_Disconnected();
		void TwitchSocket_ClosedViaSocket();
		void TwitchSocket_ChatMessage(ES_ChatMessage chatMessage);
		void TwitchSocket_StreamWentOnline(ES_StreamOnline onlineData);
		void TwitchSocket_StreamWentOffline(ES_StreamOffline offlineData);
		void TwitchSocket_AutoModMessageHold(ES_AutomodMessageHold messageHold);
		void TwitchSocket_SuspiciousMessageReceived(ES_Suspicious_UserMessage suspiciousMessage);
		void TwitchSocket_ChannelPointsRedeem(ES_ChannelPointRedeemRequest redeemInfo);
		void TwitchSocket_OnChannelGoalEnd(ES_ChannelGoal channelGoalEnded);
		void TwitchSocket_AdBreakBegin(ES_AdBreakBeginNotification infoAboutAd);
		/// <summary>
		/// This isn't actually a part of Twitch - we just abstract it to make it easier on Twitch Socket
		/// </summary>
		/// <param name="infoAboutAd">The same ES_AdBreakBeginNotification as at the beginning of an ad break</param>
		void TwitchSocket_AdBreakFinished(ES_AdBreakBeginNotification infoAboutAd);
		void TwitchSocket_ChannelRaid(ES_ChannelRaid raidInfo);
		void TwitchSocket_SharedChatBegin(ES_SharedChatBegin sharedChatBegin);
		void TwitchSocket_SharedChatUpdate(ES_SharedChatUpdate sharedChatUpdate);
		void TwitchSocket_SharedChatEnd(ES_SharedChatEnd sharedChatEnd);
	}

	public interface IChannelInstance
	{
		string ChannelID { get; set; }
		string Channel { get; set; }
		Response_StreamStatus StreamStatus { get; set; }
		bool IsSuperMod(string username);
		void SendChatMessage(string message);
	}
}
