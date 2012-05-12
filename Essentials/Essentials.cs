using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Terraria_Server;
using Terraria_Server.Commands;
using Terraria_Server.Logging;

using Essentials;
using Essentials.Kits;
using Terraria_Server.Plugins;
using Essentials.God;
using Essentials.Warps;
using System.Threading;
using Terraria_Server.Misc;

namespace Essentials
{
    public class Essentials : BasePlugin
    {
        public Properties properties;
        public Dictionary<Int32, Boolean> essentialsPlayerList; //int - player ID, bool - god mode
        public Dictionary<Int32, Boolean> essentialsRPGPlayerList; //''
		public Dictionary<String, String> lastEventByPlayer;

		public Dictionary<String, Vector2> SpawnPositions { get; set; }

        public string KitFile { get; set; }
        public string WarpFile { get; set; }
        public GodMode God { get; set; }
        private static Dictionary<String, Int32> BuffList; // Lazily loaded (instead of setup once every time Buff command is called)

		public Essentials()
		{
			Name = "Essentials";
			Description = "Essential commands for TDSM.";
			Author = "Luke";
			Version = "0.7";
			TDSMBuild = 38;
		}

        protected override void Initialized(object state)
        {
            string pluginFolder = Statics.PluginPath + Path.DirectorySeparatorChar + "Essentials";
            string propertiesFile = pluginFolder + Path.DirectorySeparatorChar + "essentials.properties";

            KitFile = pluginFolder + Path.DirectorySeparatorChar + "kits.xml";
            WarpFile = pluginFolder + Path.DirectorySeparatorChar + "warps.xml";
            
            lastEventByPlayer = new Dictionary<String, String>();
            essentialsPlayerList = new Dictionary<Int32, Boolean>();
            essentialsRPGPlayerList = new Dictionary<Int32, Boolean>();
			SpawnPositions = new Dictionary<String, Vector2>();
            BuffList = new Dictionary<String, Int32>();
            SetBuffList();

            if (!Directory.Exists(pluginFolder))
                CreateDirectory(pluginFolder); //Touch Directory, We need this.

            //We do not want to delete records!
            if (!File.Exists(KitFile))
                File.Create(KitFile).Close();
            if (!File.Exists(WarpFile))
                File.Create(WarpFile).Close();

            if (!File.Exists(propertiesFile))
                File.Create(propertiesFile).Close();

            properties = new Properties(pluginFolder + Path.DirectorySeparatorChar + "essentials.properties");
            properties.Load();
            properties.pushData();
            properties.Save(false);
        }

        public void LoadData<T>(string RecordsFile, string Identifier, 
                                Func<String, List<T>> LoadMethod, Action<String, String> CreateMethod)
        {
            Log("Loading {0}s...", Identifier);

        LOAD:
            List<T> Items = null;
            try
            {
                Items = LoadMethod.Invoke(RecordsFile);
            }
            catch (Exception)
            {
                Console.Write("Create a parsable file? [Y/n]: ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    CreateMethod.Invoke(RecordsFile, Identifier);
                    goto LOAD;
                }
            }

            Log("Complete, Loaded " + ((Items != null) ? Items.Count : 0) + " {0}(s)", Identifier);
        }

        protected override void Enabled()
        {
            //Prepare & Start the God Mode Thread.
            God = new GodMode(this);

            //Add Commands
            AddCommand("!")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.LastCommand)
                .WithPermissionNode("essentials.!");

			AddCommand("bloodmoon")
				.WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.BloodMoon)
                .WithPermissionNode("essentials.bloodmoon");

            AddCommand("slay")
                .WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.Slay)
                .WithPermissionNode("essentials.slay");

            AddCommand("heal")
                .WithAccessLevel(AccessLevel.OP)
				.Calls(Commands.HealPlayer)
				.WithHelpText("Usage:    -goblin")
				.WithHelpText("          -frost")
                .WithPermissionNode("essentials.heal");

			AddCommand("invasion")
				.WithAccessLevel(AccessLevel.OP)
				.Calls(Commands.Invasion)
                .WithPermissionNode("essentials.invasion");

            AddCommand("ping")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.ConnectionTest_Ping) //Need to make a single static function
                .WithPermissionNode("essentials.ping");

            AddCommand("pong")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.ConnectionTest_Ping) //^^
                .WithPermissionNode("essentials.pong");

            AddCommand("suicide")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.Suicide)
                .WithPermissionNode("essentials.suicide");

            AddCommand("butcher")
                .WithAccessLevel(AccessLevel.OP)
                .WithDescription("Kill all NPC's within a radius")
                .WithHelpText("Usage:    butcher <radius>")
                .WithHelpText("          butcher <radius> -g[uide]")
                .WithHelpText("          butcher <radius> <npc-name>")
                .Calls(Commands.Butcher)
                .WithPermissionNode("essentials.butcher");

            AddCommand("kit")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.Kit)
                .WithPermissionNode("essentials.kit");

            AddCommand("god")
                .WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.GodMode)
                .WithPermissionNode("essentials.god");

            AddCommand("spawn")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.Spawn)
                .WithPermissionNode("essentials.spawn");

            AddCommand("info")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.Info)
                .WithPermissionNode("essentials.info");

            AddCommand("setspawn")
                .WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.SetSpawn)
                .WithPermissionNode("essentials.setspawn");

            AddCommand("pm")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.MessagePlayer)
                .WithPermissionNode("essentials.pm");

            AddCommand("warp")
                .WithAccessLevel(AccessLevel.PLAYER)
                .WithHelpText("Usage:    warp <warp-name>")
                .WithHelpText("          setwarp <warp-name>")
                .WithHelpText("          warplist")
                .Calls(Commands.Warp)
                .WithPermissionNode("essentials.warp");

            AddCommand("setwarp")
                .WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.SetWarp)
                .WithPermissionNode("essentials.setwarp");

            AddCommand("mwarp")
                .WithAccessLevel(AccessLevel.OP)
                .Calls(Commands.ManageWarp)
                .WithPermissionNode("essentials.mwarp");

            AddCommand("warplist")
                .WithAccessLevel(AccessLevel.PLAYER)
                .Calls(Commands.ListWarp)
                .WithPermissionNode("essentials.warplist");

            AddCommand("buff")
                .WithAccessLevel(AccessLevel.OP)
                .WithHelpText("Usage:    buff <OPTIONAL-playername> <itemname> <length>")
                .Calls(Commands.Buff)
                .WithPermissionNode("essentials.buff");

			Hook(HookPoints.PlayerEnteredGame, OnPlayerEnterGame);
			Hook(HookPoints.UnkownSendPacket, Net.OnUnkownPacketSend);
			Hook(HookPoints.WorldLoaded, OnWorldLoaded);
			Hook(HookPoints.PlayerLeftGame, OnPlayerLeave);
			Hook(HookPoints.WorldRequestMessage, OnWorldRequest);

            ProgramLog.Log(base.Name + " enabled.");
        }

        protected override void Disabled()
        {
            God.Running = false;

            while (God.Running)
                Thread.Sleep(100);

            God.Dispose();

            ProgramLog.Plugin.Log(base.Name + " disabled.");
        }

        public static void Log(LogChannel Level, string message)
        {
            Level.Log("[Essentials] " + message);
        }

        public static void Log(string message)
        {
            Log(ProgramLog.Plugin, message);
        }

        public static void Log(string message, params object[] args)
        {
            Log(String.Format(message, args));
        }

        public static int GetBuffID(string itemName)
        {
            int buffID;
            if (BuffList.TryGetValue(itemName.ToLower(), out buffID))
            {
                return buffID;
            }
            return -1;
        }

        private void SetBuffList() // Uses item name when possible, otherwise uses buff name
        {
            BuffList.Add("obsidianskinpotion", 1);
            BuffList.Add("regenerationpotion", 2);
            BuffList.Add("swiftnesspotion", 3);
            BuffList.Add("gillspotion", 4);
            BuffList.Add("ironskinpotion", 5);
            BuffList.Add("manaregenerationpotion", 6);
            BuffList.Add("magicpowerpotion", 7);
            BuffList.Add("featherfallpotion", 8);
            BuffList.Add("spelunkerpotion", 9);
            BuffList.Add("invisibilitypotion", 10);
            BuffList.Add("shinepotion", 11);
            BuffList.Add("nightowlpotion", 12);
            BuffList.Add("battlepotion", 13);
            BuffList.Add("thornspotion", 14);
            BuffList.Add("waterwalkingpotion", 15);
            BuffList.Add("archerypotion", 16);
            BuffList.Add("hunterpotion", 17);
            BuffList.Add("gravitationpotion", 18);
            BuffList.Add("orboflight", 19);
            BuffList.Add("poisoned", 20);
            BuffList.Add("potionsickness", 21);
            BuffList.Add("darkness", 22);
            BuffList.Add("cursed", 23);
            BuffList.Add("onfire", 24);
            BuffList.Add("ale", 25);
            BuffList.Add("bowlofsoup", 26);
            BuffList.Add("fairybell", 27);
            //BuffList.Add("?", 28); // Not set to anything.
            BuffList.Add("crystalball", 29);
            BuffList.Add("bleeding", 30);
            BuffList.Add("confused", 31);
            BuffList.Add("slow", 32);
            BuffList.Add("weak", 33);
            BuffList.Add("merfolk", 34);
            BuffList.Add("silenced", 35);
            BuffList.Add("brokenarmor", 36);
            //BuffList.Add("?", 37); // Not set to anything.
            //BuffList.Add("?", 38); // Not set to anything.
            BuffList.Add("cursedinferno", 39);
            BuffList.Add("petbunny", 40);
            // WARNING: ID's 41 and above will cause crash (they don't exist).
        }

#region Hooks

        void OnWorldLoaded(ref HookContext ctx, ref HookArgs.WorldLoaded args)
        {
            /* For the template Warp, it uses  spawn axis, so they need to be loaded. */
            LoadData(KitFile, typeof(Kit).Name, KitManager.LoadData, KitManager.CreateTemplate);
            LoadData(WarpFile, typeof(Warp).Name, WarpManager.LoadData, WarpManager.CreateTemplate);
        }

        void OnPlayerEnterGame(ref HookContext ctx, ref HookArgs.PlayerEnteredGame args)
        {
			var player = ctx.Player;
			if (essentialsPlayerList.ContainsKey(player.Connection.SlotIndex))
            {
				essentialsPlayerList.Remove(player.Connection.SlotIndex);
			}
			
			if (properties.RememberPlayerPositions && player.AuthenticatedAs != null)
			{
				NetMessage.SendData(7, player.whoAmi);
			}
        }

		void OnPlayerLeave(ref HookContext ctx, ref HookArgs.PlayerLeftGame args)
		{
			var player = ctx.Player;
			if (properties.RememberPlayerPositions && player != null && player.AuthenticatedAs != null)
			{
				SpawnPositions[player.Name] = player.Position / 16;
			}
		}

		void OnWorldRequest(ref HookContext ctx, ref HookArgs.WorldRequestMessage args)
		{
			var player = ctx.Player;
			if (properties.RememberPlayerPositions && player != null && !String.IsNullOrEmpty(player.AuthenticatedAs))
			{
				Vector2 pos;
				if (SpawnPositions.TryGetValue(player.AuthenticatedAs, out pos))
				{
					args.SpawnX = (int)pos.X;
					args.SpawnY = (int)pos.Y;
				}
			}
		}

#endregion
       
        private static void CreateDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}
