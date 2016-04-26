using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;
using Bungie;
using Bungie.Models;
using Microsoft.Bot.Builder.Luis;
using System.Collections.Generic;
using System.Reflection;

namespace DestinyGhostBot
{
	[BotAuthentication]
	public class MessagesController : ApiController
	{
		/// <summary>
		/// POST: api/Messages
		/// Receive a message from a user and reply to it
		/// </summary>
		public async Task<Message> Post([FromBody]Message message)
		{
			if (message.Type == "Message")
			{
				// return our reply to the user
				return await Conversation.SendAsync(message, () => new DestinyDialog());
			}
			else
			{
				return HandleSystemMessage(message);
			}
		}

		private Message HandleSystemMessage(Message message)
		{
			if (message.Type == "Ping")
			{
				Message reply = message.CreateReplyMessage();
				reply.Type = "Ping";
				return reply;
			}
			else if (message.Type == "DeleteUserData")
			{
				// Implement user deletion here
				// If we handle user deletion, return a real message

			}
			else if (message.Type == "BotAddedToConversation")
			{
			}
			else if (message.Type == "BotRemovedFromConversation")
			{
			}
			else if (message.Type == "UserAddedToConversation")
			{
			}
			else if (message.Type == "UserRemovedFromConversation")
			{
			}
			else if (message.Type == "EndOfConversation")
			{
			}

			return null;
		}
	}

	[LuisModel("APP_ID", "SUBSCRIPTION_KEY")]
	[Serializable]
	public class DestinyDialog : LuisDialog<object>
	{
		// Get Destiny API key from https://www.bungie.net/en/User/API
		private readonly string apiKey = "API_KEY";

		private const string Entity_PlayerIdentity_Console = "PlayerIdentity::Console";
		private const string Entity_PlayerIdentity_GamerTag = "PlayerIdentity::Gamertag";
		private const string Entity_PlayerPair_Player1 = "PlayerPair::Player1";
		private const string Entity_PlayerPair_Player2 = "PlayerPair::Player2";
		private const string Entity_PlayerIdentity = "PlayerIdentity";
		private const string Entity_Statistic = "Statistic";
		private const string Entity_Vendor = "Vendor";
		private const string Entity_Activity = "Activity";


		public DestinyDialog(ILuisService service = null)
			: base(service)
		{
		}

		[LuisIntent("")]
		public async Task None(IDialogContext context, LuisResult result)
		{
			context.Wait(MessageReceived);
		}

		[LuisIntent("WhatIsLove")]
		public async Task WhatIsLove(IDialogContext context, LuisResult result)
		{
			await context.PostAsync($"Baby don't hurt me");
			context.Wait(MessageReceived);
		}

		[LuisIntent("NoTimeToExplain")]
		public async Task NoTimeToExplain(IDialogContext context, LuisResult result)
		{
			await context.PostAsync($"I don't have time to explain why I don't have time to explain.");
			context.Wait(MessageReceived);
		}

		[LuisIntent("Greetings")]
		public async Task Greetings(IDialogContext context, LuisResult result)
		{
			string message = $"Greetings Guardian. You can ask me about the daily or weekly activies, I can check Xur's inventory for you, and even look up stats.";
			await context.PostAsync(message);
			context.Wait(MessageReceived);
		}

		[LuisIntent("Vendor")]
		public async Task GetVendor(IDialogContext context, LuisResult result)
		{
			EntityRecommendation vendorName;
			if (!result.TryFindEntity(Entity_Vendor, out vendorName))
			{
				string message = $"Sorry I did not understand";
				await context.PostAsync(message);
				context.Wait(MessageReceived);
				return;
			}

			var service = new DestinyService(apiKey);

			switch (vendorName.Entity)
			{
				case "xur":
					{
						var response = await service.GetXur(true);

						if (response != null)
						{
							var vendor = response.Vendor;
							var definitions = response.Definitions;

							var message = "This week Xur has. . .  ";

							foreach (var category in vendor.SaleItemCategories)
							{
								message += category.CategoryTitle + ":  ";

								foreach (var item in category.SaleItems)
								{
									var itemDefinition = definitions.Items[item.Item.ItemHash];
									message += itemDefinition.ItemName.ToString() + "-";

									foreach (var cost in item.Costs)
									{
										message += definitions.Items[cost.ItemHash].ItemName.ToString() + ":" + cost.Value + " ";
									}

									message += "  ";
								}
							}
							await context.PostAsync(message);
						}
						else
						{
							await context.PostAsync("Xur is not available at this time");
						}
						break;
					}
				default:
					{
						var response = await service.GetManifest();


						await context.PostAsync($"I am unable to check the status of {vendorName.Entity} at this time.");
						break;
					}
			}

			context.Wait(MessageReceived);
		}

		[LuisIntent("Nightfall")]
		public async Task GetNightfall(IDialogContext context, LuisResult result)
		{
			var service = new DestinyService(apiKey);

			var response = await service.GetAdvisors(true);

			var nightfall = response.Definitions.Activities[response.Advisors.NightfallActivityHash];
			var place = response.Definitions.Places[nightfall.PlaceHash];
			var activity = response.Definitions.Activities[response.Advisors.Nightfall.SpecificActivityHash];

			var text = activity.ActivityName + " on " + place.PlaceName;
			await context.PostAsync(text);

			context.Wait(MessageReceived);
		}

		[LuisIntent("DailyHeroic")]
		public async Task GetDailyHeroic(IDialogContext context, LuisResult result)
		{
			var service = new DestinyService(apiKey);
			var response = await service.GetAdvisors(true);

			var activity = response.Definitions.Activities[response.Advisors.DailyChapter.ActivityBundleHash];
			var place = response.Definitions.Places[activity.PlaceHash];

			await context.PostAsync($"{activity.ActivityName} on {place.PlaceName}");

			context.Wait(MessageReceived);
		}


		[LuisIntent("DailyCrucible")]
		public async Task GetDailyCrucible(IDialogContext context, LuisResult result)
		{
			var service = new DestinyService(apiKey);
			var response = await service.GetAdvisors(true);

			var activity = response.Definitions.ActivityBundles[response.Advisors.DailyCrucibleHash];

			await context.PostAsync(activity.ActivityName);

			context.Wait(MessageReceived);
		}

		[LuisIntent("WeeklyCrucible")]
		public async Task GetWeeklyCrucible(IDialogContext context, LuisResult result)
		{
			var service = new DestinyService(apiKey);
			var response = await service.GetAdvisors(true);

			var activity = response.Definitions.ActivityBundles[response.Advisors.WeeklyCrucible[0].ActivityBundleHash];

			await context.PostAsync(activity.ActivityName);

			context.Wait(MessageReceived);
		}

		[LuisIntent("EnumerateStats")]
		public async Task EnumerateStats(IDialogContext context, LuisResult result)
		{
			EntityRecommendation activity;
			if (!result.TryFindEntity(Entity_Activity, out activity))
			{
				activity = new EntityRecommendation() { Entity = "" };
			}

			var newline = "  " + Environment.NewLine;
			var message = $"{activity.Entity} Guardian Stats I can display are:" + newline;

			Type statsType = typeof(MergedStats);
			switch (activity.Entity)
			{
				case "crucible":
				case "pvp":
					statsType = typeof(PvpStats);
					break;
				case "pve":
					statsType = typeof(PveStats);
					break;
			}

			PropertyInfo[] propertyInfos = statsType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo info in propertyInfos)
			{
				message += $" {BeautifyPropertyName(info.Name)}" + newline;
			}

			await context.PostAsync(message);
			context.Wait(MessageReceived);
		}

		private string BeautifyPropertyName(string name)
		{
			string lowercase = name.ToLower();
			string text = "";

			for (int i = 0; i < name.Length; i++)
			{
				if (i > 0 && name[i] != lowercase[i])
				{
					text += " ";
				}
				text += name[i];
			}

			if (text.StartsWith("Weapon Kills"))
			{
				text = text.Substring("Weapon Kills".Length) + " Kills";
			}

			return text;
		}

		[LuisIntent("MyStats")]
		public async Task BeginGetMyStats(IDialogContext context, LuisResult result)
		{
			EntityRecommendation statistic;
			if (result.TryFindEntity(Entity_Statistic, out statistic))
			{
				context.PerUserInConversationData.SetValue<string>("Statistic", statistic.Entity);
			}

			EntityRecommendation activity;
			if (result.TryFindEntity(Entity_Activity, out activity))
			{
				context.PerUserInConversationData.SetValue<string>("Activity", activity.Entity);
			}
			else
			{
				context.PerUserInConversationData.SetValue<string>("Activity", "");
			}

			Player player;
			if (context.PerUserInConversationData.TryGetValue<Player>("Player", out player))
			{
				await EndGetMyStats(context);
			}
			else
			{
				PromptDialog.Text(context, EnsureMyIdentity, "What is your PSN ID or Gamertag?");
			}
		}

		public async Task EnsureMyIdentity(IDialogContext context, IAwaitable<string> identity)
		{
			string value = await identity;

			var service = new DestinyService(apiKey);
			var player = await GetPlayerByDisplayName(service, value);

			if (player.Type == MembershipType.None)
			{
				await context.PostAsync("I'm sorry, I couldn't find any Guardians with that name");
				context.Wait(MessageReceived);
				return;
			}

			context.PerUserInConversationData.SetValue<Player>("Player", player);
			await EndGetMyStats(context);
		}

		public async Task EndGetMyStats(IDialogContext context)
		{
			var player = context.PerUserInConversationData.Get<Player>("Player");
			var statistic = context.PerUserInConversationData.Get<string>("Statistic");
			string activity; 
			context.PerUserInConversationData.TryGetValue<string>("Activity", out activity);

			var service = new DestinyService(apiKey);

			var stat = await GetStatisticForPlayer(service, player, statistic, activity);

			if (stat != null)
			{
				await context.PostAsync($"{player.Name}: {this.BeautifyStat(stat)}");
			}
			else
			{
				await context.PostAsync($"I do not understand {statistic}");
			}

			context.Wait(MessageReceived);
		}

		[LuisIntent("Stats")]
		public async Task GetStatForGamertag(IDialogContext context, LuisResult result)
		{
			EntityRecommendation statistic;
			if (!result.TryFindEntity(Entity_Statistic, out statistic))
			{
				statistic = new EntityRecommendation() { Entity = "" };
			}

			EntityRecommendation gamertag;
			if (!result.TryFindEntity(Entity_PlayerIdentity_GamerTag, out gamertag))
			{
				gamertag = new EntityRecommendation() { Entity = "" };
			}

			EntityRecommendation activity;
			if (!result.TryFindEntity(Entity_Activity, out activity))
			{
				activity = new EntityRecommendation() { Entity = "" };
			}

			var service = new DestinyService(apiKey);
			var player = await GetPlayerByDisplayName(service, gamertag.Entity);

			if (player.Type == MembershipType.None)
			{
				await context.PostAsync("I'm sorry, I couldn't find any Guardians with that name");
				context.Wait(MessageReceived);
				return;
			}

			var stat = await GetStatisticForPlayer(service, player, statistic.Entity, activity.Entity);

			if (stat != null)
			{
				await context.PostAsync($"{player.Name}: {this.BeautifyStat(stat)}");
			}
			else
			{
				await context.PostAsync($"I do not understand {statistic.Entity}");
			}

			context.Wait(MessageReceived);
		}

		[LuisIntent("ComparePlayers")]
		public async Task GetStatCompareForPlayers(IDialogContext context, LuisResult result)
		{
			EntityRecommendation statistic;
			if (!result.TryFindEntity(Entity_Statistic, out statistic))
			{
				statistic = new EntityRecommendation() { Entity = "" };
			}

			EntityRecommendation gamertag1;
			if (!result.TryFindEntity(Entity_PlayerPair_Player1, out gamertag1))
			{
				gamertag1 = new EntityRecommendation() { Entity = "" };
			}

			EntityRecommendation gamertag2;
			if (!result.TryFindEntity(Entity_PlayerPair_Player2, out gamertag2))
			{
				gamertag2 = new EntityRecommendation() { Entity = "" };
			}

			EntityRecommendation activity;
			if (!result.TryFindEntity(Entity_Activity, out activity))
			{
				activity = new EntityRecommendation() { Entity = "" };
			}

			var service = new DestinyService(apiKey);
			var player1 = await GetPlayerByDisplayName(service, gamertag1.Entity);
			if (player1.Type == MembershipType.None)
			{
				await context.PostAsync($"I'm sorry, I couldn't find any Guardians with that name ({gamertag1.Entity})");
				context.Wait(MessageReceived);
				return;
			}

			var player2 = await GetPlayerByDisplayName(service, gamertag2.Entity);
			if (player2.Type == MembershipType.None)
			{
				await context.PostAsync($"I'm sorry, I couldn't find any Guardians with that name ({gamertag2.Entity})");
				context.Wait(MessageReceived);
				return;
			}

			var stat1 = await GetStatisticForPlayer(service, player1, statistic.Entity, activity.Entity);
			var stat2 = await GetStatisticForPlayer(service, player2, statistic.Entity, activity.Entity);

			if (stat1 != null && stat2 != null)
			{
				if (stat2.Basic.Value > stat1.Basic.Value)
				{
					//flip!
					var tempStat = stat1;
					var tempPlayer = player1;
					stat1 = stat2;
					player1 = player2;
					stat2 = tempStat;
					player2 = tempPlayer;				
				}

				if (stat1.Basic.Value == stat2.Basic.Value)
				{					
					await context.PostAsync($"It's a tie! {player1.Name} and {player2.Name} both have {this.BeautifyStat(stat1)}!!");
				}
				else
				{
					var diff = stat1.Basic.Value - stat2.Basic.Value;
					await context.PostAsync($"{player1.Name} has {diff:#,###.##} more than {player2.Name}!");
				}
			}
			else
			{
				await context.PostAsync($"I do not understand {statistic.Entity}");
			}

			context.Wait(MessageReceived);
		}

		private string BeautifyStat(Bungie.Models.Stat stat)
		{
			Int32 value;
			if (Int32.TryParse(stat.Basic.DisplayValue, out value))
			{
				return $"{stat.Basic.Value:#,###.##}";
			}
			else
			{
				return stat.Basic.DisplayValue;
			}
		}

		public async Task<Bungie.Models.Stat>GetStatisticForPlayer(DestinyService service, Player identity, string key, string activity = null)
		{
			var response = await service.GetStatsForAccount(identity.Type, identity.Id);
			var allStats = response.MergedAllCharacters.Merged.AllTime;

			#region Prepare Key
			string keyStr = key.ToLower();
			keyStr = keyStr.Replace(" ", "");

			string alias;
			if (StatAliases.TryGetValue(keyStr, out alias))
			{
				keyStr = alias;
			}
			else if (keyStr != "kills" && keyStr.EndsWith("kills"))
			{
				keyStr = keyStr.Substring(0, keyStr.Length - "kills".Length);
				keyStr = "WeaponKills" + keyStr;
			}
			#endregion

			Type statsType = typeof(MergedStats);
			switch (activity)
			{
				case "crucible":
				case "pvp":
					statsType = typeof(PvpStats);
					break;
				case "pve":
					statsType = typeof(PveStats);
					break;
			}

			PropertyInfo[] propertyInfos = statsType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

			var results = from p in propertyInfos
						  where p.Name.ToLower() == keyStr.ToLower()
						  select p;
			foreach (var property in results)
			{
				switch (activity)
				{
					case "crucible":
					case "pvp":
						return property.GetValue(response.MergedAllCharacters.Results.AllPvp.AllTime) as Bungie.Models.Stat;
					case "pve":
						return property.GetValue(response.MergedAllCharacters.Results.AllPve.AllTime) as Bungie.Models.Stat;
					default:
						return property.GetValue(response.MergedAllCharacters.Merged.AllTime) as Bungie.Models.Stat;
				}
			}

			return null;
		}

		public async Task<Player> GetPlayerByDisplayName(DestinyService service, string displayName)
		{
			var searchResults = await service.SearchPlayers(MembershipType.All, displayName);

			if (searchResults.Count == 1)
			{
				var player = searchResults[0];
				return new Player()
				{
					Name = player.DisplayName,
					Type = player.MembershipType,
					Id = player.MembershipId
				};
			}


			return new Player()
			{
				Name = displayName,
				Type = MembershipType.None,
				Id = 0
			};
		}

		[Serializable]
		public class Player
		{
			public string Name;
			public MembershipType Type;
			public long Id;
		}

	}
}

