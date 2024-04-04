using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;


using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;
using Vintagestory.Server;
using Vintagestory.Client.NoObf;
using static Twitch_Integration.TwitchIntegration;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

//https://apidocs.vintagestory.at/api/Vintagestory.API.Datastructures.TreeAttribute.html?q=TreeAttribute

namespace Twitch_Integration {
	public class Twitch_IntegrationModSystem:ModSystem {


		private ICoreAPI coreApi;
		private ICoreServerAPI serverApi;
		private ICoreClientAPI clientApi;
		public TwitchIntegration twitch;
		public const string modid = "twitch_integration";


		//public override bool ShouldLoad(EnumAppSide side) { return side == EnumAppSide.Client; }
		public override bool ShouldLoad(EnumAppSide side) { return side == EnumAppSide.Server; }


		public override void Start(ICoreAPI api) {
			base.Start(api);
			api.Logger.Notification("Hello from template mod: " + api.Side);
			Debug.api = api;
			coreApi = api;

			/*
			// Chat příkaz .
			api.ChatCommands.Create("hp-").WithDescription("HP Decrease 1").HandleWith((args) => {
				HPDecrement();
				return TextCommandResult.Success("Command: hp-");
			});
			api.ChatCommands.Create("s").WithDescription("Spawn creature").HandleWith((args) => {
				SpawnCreatures();
				return TextCommandResult.Success("Command: s");
			});
			*/

		}


		public override void StartServerSide(ICoreServerAPI api) {
			base.StartServerSide(api);
			serverApi = api;

			api.Event.OnEntityLoaded += delegate (Entity entity) {
				api.Logger.Notification("StartServerSide() api.Event.OnEntityLoaded " + entity.GetName());


			};


			// Chat příkaz /
			api.ChatCommands.Create("hp+").WithDescription("HP Increase").RequiresPrivilege(Privilege.chat).RequiresPlayer().HandleWith((args) => {
				//Entity byEntity = args.Caller.Entity;
				Entity byEntity = api.World.AllPlayers[0].Entity;
				HPIncrement(byEntity, 1f);
				return TextCommandResult.Success("Command success: /hp+");
			});

			api.ChatCommands.Create("hp-").WithDescription("HP decrease").RequiresPrivilege(Privilege.chat).RequiresPlayer().HandleWith((args) => {
				//Entity byEntity = args.Caller.Entity;
				Entity byEntity = api.World.AllPlayers[0].Entity;
				HPIncrement(byEntity, -1f);
				return TextCommandResult.Success("Command success: /hp-");
			});

			api.ChatCommands.Create("spawn").WithDescription("spawns around the player").RequiresPrivilege(Privilege.chat).RequiresPlayer().HandleWith((args) => {
				//Entity byEntity = args.Caller.Entity;
				Entity byEntity = api.World.AllPlayers[0].Entity;
				SpawnCreatures();
				return TextCommandResult.Success("Command success: /spawn");
			});

			AssetLocation sound = new AssetLocation("here", "sounds/partyhorn");



			twitch = new TwitchIntegration();
			twitch.chanel = Lang.Get(modid+":twitch_chanel");
			twitch.clientId = Lang.Get(modid + ":twitch_client_id");
			twitch.password = Lang.Get(modid + ":twitch_pass");
			twitch.hlasovaciEvents.Add(new HlasovaciEvent("!hp-", delegate () { HPIncrement(api.World.AllPlayers[0].Entity, -1f); }));
			twitch.hlasovaciEvents.Add(new HlasovaciEvent("!hp+", delegate () { HPIncrement(api.World.AllPlayers[0].Entity, +1f); }));
			twitch.delkaHlasovasni = new TimeSpan(0, int.Parse(Lang.Get(modid + ":votingTimeMinutes")), 5);
			twitch.rozmeziHlasovasni = new TimeSpan(0, int.Parse(Lang.Get(modid + ":votingWaitingMinutes")), 5);
			twitch.Connect();
			//twitch.SendTestMessage();
			api.Event.RegisterGameTickListener(twitch.Update, 100);

		}


		public override void StartClientSide(ICoreClientAPI api) {
			base.StartClientSide(api);
			clientApi = api;

			api.Event.LevelFinalize += OnLevelFinalize;
		}


		public void OnLevelFinalize() {
			//HPDecrement();
		}



		public void HPIncrement( Entity aEntita, float aHP = 1f ) {
			//expression is type? (type) expression : (type)null    =======    expression as type
			//if( null != (playerEntita = aEntita as IPlayer ) )
			if(aEntita is EntityPlayer playerEntita) {
				if(playerEntita != null) {
					if(aHP >= 0) { // Heal
						playerEntita.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.Heal }, aHP);
						//clientApi.ShowChatMessage($"(" + playerEntita.Player.PlayerName + ") Heal " + aHP + "HP");
					} else { // DMG
						aEntita.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.BluntAttack }, -aHP);
						//clientApi.ShowChatMessage($"(" + playerEntita.Player.PlayerName + ") Take DMG " + -aHP + "HP");
					}
				} else { serverApi.World.Logger.Debug("LH> Twitch_IntegrationModSystem.HPIncrement() !!! playerEntita == null"); }
			} else { serverApi.World.Logger.Debug("LH> Twitch_IntegrationModSystem.HPIncrement() !!! aEntita is EntityPlayer playerEntita"); }
		}


		public void SpawnCreatures() {

			//string[] entitysTypes = { "wolf-male" };

			// Vytvoření instance entity
			EntityProperties properties = serverApi.World.GetEntityType( new AssetLocation("game:wolf-male") );

			// Zkontrolujeme, zda jsme úspěšně získali entity properties
			if(properties == null) {
				serverApi.World.Logger.Error("LH> Twitch_IntegrationModSystem.SpawnCreatures() !!! Entity properties not found!");
				return;
			}

			// Vytvoření instance entity
			Entity entity = serverApi.World.ClassRegistry.CreateEntity(properties);


			// Zkontrolujeme, zda se entita úspěšně vytvořila
			if(entity == null) {
				serverApi.World.Logger.Error("LH> Twitch_IntegrationModSystem.SpawnCreatures() !!! Failed to create entity!");
				return;
			}

			// Nastavení pozice, kde se má entita spawnout (např. v blízkosti hráče)
			var playerE = serverApi.World.AllPlayers[0].Entity;
			entity.Pos.SetPos(playerE.Pos.X, playerE.Pos.Y, playerE.Pos.Z);

			// Přidání entity do světa
			// Přidání entity do světa
			serverApi.World.SpawnEntity(entity);

		}



		public void DebugTest() {
			if(coreApi == null) { clientApi.ShowChatMessage($"coreApi == null"); }
			if(serverApi == null) { clientApi.ShowChatMessage($"serverApi == null"); }
			if(clientApi == null) { clientApi.ShowChatMessage($"clientApi == null"); }

			IPlayer clientPlayer = clientApi.World.Player;
			IPlayer corePlayer = coreApi.World.AllPlayers[0];

			clientApi.ShowChatMessage($"healt =" + clientPlayer.Entity.Stats.GetBlended("health"));
			clientApi.ShowChatMessage($"food =" + clientPlayer.Entity.Stats.GetBlended("food"));
			clientApi.ShowChatMessage($"currenthealth =" + clientPlayer.Entity.Stats.GetBlended("currenthealth"));


			DamageSource dmg = new DamageSource();
			dmg.Type = EnumDamageType.Fire;
			dmg.Source = EnumDamageSource.Player; // EnumDamageSource.Internal

			// 
			//clientPlayer.Entity.Die(EnumDespawnReason.Combusted); // OK
			//clientPlayer.Entity.Ignite();
			//clientPlayer.Entity.OnHurt(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Fire }, 5f);
			//clientPlayer.Entity.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Fire }, 5f);
			//clientPlayer.Entity.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Player, Type = EnumDamageType.Heal }, 5f); // Heal
			clientPlayer.Entity.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.BluntAttack }, 3f);
			//clientPlayer.Entity.Stats.Set("health", "currenthealth", 0.5f);
			//clientPlayer.Entity.Stats.Set("food", "", 0.5f);
			//lientPlayer.Entity.Stats.Set("walkspeed", "maltiezfirearms", 0f, true);


			//corePlayer.Entity.Ignite();
			//corePlayer.Entity.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Fire }, 5f);
			corePlayer.Entity.ReceiveDamage(new DamageSource { SourceEntity = null, Type = EnumDamageType.BluntAttack }, 3f);
			//corePlayer.Entity.OnHurt(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Fire }, 5f);
			//float r3 = 1f;
			//EntityBehavior.OnEntityReceiveDamage(dmg, ref r3);
			;



			EntityBehaviorHunger player_data_hunger = clientPlayer.Entity.GetBehavior<EntityBehaviorHunger>();
			EntityBehaviorHealth player_data_health = clientPlayer.Entity.GetBehavior<EntityBehaviorHealth>();
			if(player_data_health != null) {
				clientApi.ShowChatMessage($"player_data_health = " + player_data_health.ToString());
			} else {
				clientApi.ShowChatMessage($"player_data_health == null");
			}

			player_data_hunger = corePlayer.Entity.GetBehavior<EntityBehaviorHunger>();
			player_data_health = corePlayer.Entity.GetBehavior<EntityBehaviorHealth>();
			if(player_data_health != null) {
				clientApi.ShowChatMessage($"player_data_health2 = " + player_data_health.ToString());
			} else {
				clientApi.ShowChatMessage($"player_data_health2 == null");
			}



		}


	}
}
/*
EN: Exclusive ownership of this file cannot be claimed by persons other than the authors of the file.
CZ: Na tento soubor nelze uplatni výhradní vlastnicví, jiným osobám než autorm souboru.

LICENSE - MIT

Copyright (c) 2023 Hotárek Lukáš

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/