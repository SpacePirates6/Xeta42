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
using VRage;
using Sandbox.Game.EntityComponents;
using VRage.Input;
using System.Globalization;

namespace TetherSE
{
    class Tether
    {
        public static int ticks = 0;

        public static void Update()
        {
            if (MyAPIGateway.Multiplayer == null || MySession.Static.LocalCharacter == null)
            {
                return;
            }

            var localPlayer = MySession.Static.LocalCharacter;
            IMyUtilities utils = MyAPIGateway.Utilities;

            if (ticks < 1)
            {
                ticks++;
                return;
            }

            ticks = 0;

            if (GetTargetedBlock.selectedBlock != null)
            {
                if (Vector3D.Distance(localPlayer.PositionComp.GetPosition(),
                    GetTargetedBlock.selectedBlock.GetPosition()) > Patches.maxUseDistance)
                {
                    utils.ShowMessage("Tether Broke!",
                        $"You Moved More Than {Patches.maxUseDistance}m from tethered block.");
                    GetTargetedBlock.selectedBlock = null;
                    GetTargetedBlock.selectedObject = null;
                    return;
                }
            }

            var equippedTool = MySession.Static.LocalCharacter.HandItemDefinition;
            bool isWelderEquipped = false;
            if (equippedTool != null)
            {
                var toolName = equippedTool.Id.SubtypeName;
                if (toolName.Contains("Welder", StringComparison.OrdinalIgnoreCase))
                {
                    isWelderEquipped = true;
                    if (GetTargetedBlock.selectedBlock != null && MySession.Static.LocalCharacter.BuildPlanner.Count > 0)
                    {
                        DoWelder(MySession.Static.LocalCharacter);
                    }
                }
                else if (toolName.Contains("Grinder", StringComparison.OrdinalIgnoreCase))
                {
                    Grnd2Loot.DoGrinder();
                }
                else if (toolName.Contains("Drill", StringComparison.OrdinalIgnoreCase))
                {
                    DoDrill();
                }
            }

            if (GetTargetedBlock.selectedBlock != null && !isWelderEquipped && MyAPIGateway.Input.IsKeyPress(MyKeys.Control))
            {
                InventoryManager.DoDeposit();
            }
        }

        public static void DoWelder(MyCharacter localPlayer)
        {
            Patches.UseObjectPatch(Patches.maxUseDistance);
            Reflections.Withdraw.Invoke(null, new object[] { (MyEntity)GetTargetedBlock.selectedObject.Owner, localPlayer.GetInventory(0), null });
            Patches.UseObjectPatch(5f);
            return;
        }

        public static void DoDrill()
        {
            if (GetTargetedBlock.selectedBlock == null)
            {
                return;
            }

            var inventory = (MyInventory)GetTargetedBlock.selectedBlock.GetInventory();
            var playerInventory = MySession.Static.LocalCharacter.GetInventory();
            var items = new List<MyPhysicalInventoryItem>(playerInventory.GetItems());

            foreach (var item in items)
            {
                var contentId = item.Content.GetObjectId();
                if (contentId.TypeId.IsNull) continue;

                if (!contentId.ToString().ToLower().Contains("ore")) continue;

                MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10000;
                MyInventory.TransferByPlanner(playerInventory, inventory, contentId, MyItemFlags.None, item.Amount);
                MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10;
            }
        }
    }
}