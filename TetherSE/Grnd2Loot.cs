using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Entities.Cube;

namespace TetherSE
{
    class Grnd2Loot
    {
        public static void DoGrinder()
        {
            var caster = MySession.Static.LocalCharacter.EquippedTool?.Components?.Get<MyCasterComponent>();
            var hitSlimBlock = caster?.HitBlock;

            if (hitSlimBlock != null && hitSlimBlock.FatBlock is IMyTerminalBlock terminalBlock && terminalBlock.HasInventory)
            {
                var groundInventory = (MyInventory)terminalBlock.GetInventory();
                var playerInventory = MySession.Static.LocalCharacter.GetInventory();
                var items = new List<MyPhysicalInventoryItem>(groundInventory.GetItems());

                foreach (var item in items)
                {
                    var contentId = item.Content.GetObjectId();
                    if (contentId.TypeId.IsNull) continue;

                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10000;
                    MyInventory.TransferByPlanner(groundInventory, playerInventory, contentId, MyItemFlags.None, item.Amount);
                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10;
                }
            }
        }
    }
}