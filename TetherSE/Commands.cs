using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using System.Linq;

namespace TetherSE
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Commands : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            MyAPIUtilities.Static.MessageEntered += Command;
        }

        protected override void UnloadData()
        {
            MyAPIUtilities.Static.MessageEntered -= Command;
            base.UnloadData();
        }

        private static void Command(string message, ref bool sendToOthers)
        {
            if (message.StartsWith("/setlootdisplay", StringComparison.OrdinalIgnoreCase))
            {
                sendToOthers = false;
                var args = message.Split(' ').Skip(1).ToList();

                if (args.Count != 2)
                {
                    MyAPIGateway.Utilities.ShowMessage("Error", "Invalid arguments. Usage: /setlootdisplay <X> <Y>");
                    return;
                }

                if (!float.TryParse(args[0], out float x))
                {
                    MyAPIGateway.Utilities.ShowMessage("Error", "Invalid X coordinate. Must be a float.");
                    return;
                }

                if (!float.TryParse(args[1], out float y))
                {
                    MyAPIGateway.Utilities.ShowMessage("Error", "Invalid Y coordinate. Must be a float.");
                    return;
                }

                LootDisplay.SetStartPosition(new Vector2(x, y));
                MyAPIGateway.Utilities.ShowMessage("Success", $"Loot display position set to X:{x}, Y:{y}");
                return;
            }

            if (message.StartsWith("/getlootdisplay", StringComparison.OrdinalIgnoreCase))
            {
                sendToOthers = false;
                Vector2 currentPosition = LootDisplay.startPositionNormalized;
                MyAPIGateway.Utilities.ShowMessage("Info", $"Current loot display position: X:{currentPosition.X}, Y:{currentPosition.Y}");
                return;
            }
            
            if(!message.StartsWith("/tether", StringComparison.OrdinalIgnoreCase))
            {         
                return;
            }

            sendToOthers = false;

            GetTargetedBlock.GetPort();


        }


    }
}