﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria_Server;

namespace Essentials.God
{
    public class GodMode
    {
        Essentials plugin;
        public Thread godThread;
        int secondRotation;

        public GodMode(Essentials Plugin)
        {
            plugin = Plugin;
            secondRotation = plugin.properties.GodModeRotation;
            godThread = new Thread(this.Run);
            godThread.Start();
        }

        public void Run()
        {
            while (plugin.isEnabled)
            {
                for (int i = 0; i < plugin.essentialsPlayerList.Count; i++)
                {
                    Player eplayer = Main.players[plugin.essentialsPlayerList.Keys.ElementAt(i)];
                    if (eplayer.statLife != eplayer.statLifeMax)
                    {
                        if (plugin.essentialsPlayerList.Values.ElementAt(i))
                        {
                            Item.NewItem((int)eplayer.Position.X, (int)eplayer.Position.Y, eplayer.Width, eplayer.Height, 58, 1, false);
                        }
                    }
                }
                Thread.Sleep(secondRotation * 1000);
            }
        }
    }
}
