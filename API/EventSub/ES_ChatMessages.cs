﻿using Newtonsoft.Json;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Diagnostics;

namespace SuiBot_TwitchSocket.API.EventSub
{
	[DebuggerDisplay(nameof(ES_ChatMessage) + " {chatter_user_name}: {message.text}")]
	public class ES_ChatMessage
	{
		/// <summary>
		/// Roles available on Twitch, where 0 is SuperMod and 5 is User
		/// </summary>
		public enum Role
		{
			SuperMod,
			Mod,
			VIP,
			Subscriber,
			User
		}

		[DebuggerDisplay(nameof(Message) + " {text}")]
		public class Message
		{
			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(Fragment) + " {text}")]
			public class Fragment
			{
				public string type;
				public string text;
				public ChatMessage_Fragment_Emote emote;
				public Mention mention;
			}

			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(ChatMessage_Fragment_Emote) + " {id}")]
			public class ChatMessage_Fragment_Emote
			{
				public string id;
				public string emote_set_id;
				public string owner_id;
			}

			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(Mention) + " {user_login}")]
			public class Mention
			{
				public ulong user_id;
				public string user_login;
				public string user_name;
			}

			public string text;
			public Fragment[] fragments;
		}

		[DebuggerDisplay(nameof(Badge) + " {setinfo}")]
		public class Badge
		{
			public string set_id;
			public int id;
			public string info;
		}

		[DebuggerDisplay(nameof(Cheer) + " {bits}")]
		public class Cheer
		{
			public int bits;
		}

		public class Event_Reply
		{
			public string parent_message_id;
			public string parent_message_body;
			public ulong parent_user_id;
			public string parent_user_name;
			public string parent_user_login;
			public string thread_message_id;
			public ulong thread_user_id;
			public string thread_user_name;
			public string thread_user_login;
		}

		public string broadcaster_user_id; //This should be string
		public string broadcaster_user_login;
		public string broadcaster_user_name;
		public string chatter_user_id;
		public string chatter_user_login;
		public string chatter_user_name;
		public string message_id;
		public string source_message_id;
		public Message message;
		public string color;
		public Badge[] badges = new Badge[0];
		public string message_type;
		public Cheer cheer;
		public Event_Reply reply;
		public string channel_points_custom_reward_id;
		public string channel_points_animation_id;
		[NonSerialized][JsonIgnore] public Role UserRole = Role.User;

		internal void SetupRole(IChannelInstance channel)
		{
			if (broadcaster_user_id == chatter_user_id)
			{
				UserRole = Role.SuperMod;
				return;
			}

			foreach (var badge in badges)
			{
				if (badge.set_id == "moderator")
				{
					UserRole = Role.Mod;
					return;
				}
				else if (badge.set_id == "vip" || badge.set_id == "artist")
				{
					UserRole = Role.VIP;
					return;
				}
				else if (badge.set_id == "subscriber")
				{
					UserRole = Role.Subscriber;
					return;
				}
			}

			if (channel != null)
			{
				if (channel.IsSuperMod(broadcaster_user_login))
				{
					UserRole = Role.SuperMod;
					return;
				}
			}
		}

		public static explicit operator ES_ChatMessage(ES_ChannelPoints.ES_ChannelPointRedeemRequest channel_point)
		{
			return new ES_ChatMessage()
			{
				UserRole = Role.User,
				channel_points_custom_reward_id = channel_point.reward.id,
				broadcaster_user_id = channel_point.broadcaster_user_id,
				broadcaster_user_name = channel_point.broadcaster_user_name,
				broadcaster_user_login = channel_point.broadcaster_user_login,
				chatter_user_id = channel_point.user_id,
				chatter_user_name = channel_point.user_name,
				chatter_user_login = channel_point.user_login,
				message_id = null,
				message = new Message()
				{
					fragments = new Message.Fragment[] { },
					text = channel_point.user_input
				},
				badges = null,
				reply = null,
				cheer = null,
				color = null,
				channel_points_animation_id = null,
				message_type = "text",
				source_message_id = channel_point.broadcaster_user_id,
			};
		}
	}
}
