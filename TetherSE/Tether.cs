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
using VRage.Input;

namespace TetherSE
{
    class Tether
    {

        public static void Update()
        {
            if (MyAPIGateway.Multiplayer == null || MySession.Static.LocalCharacter == null)
            {
                return;
            }

            var localPlayer = MySession.Static.LocalCharacter;
            IMyUtilities utils = MyAPIGateway.Utilities;

            if (ticks < 50)
            {
                ticks++;
                return;
            }

            ticks = 0;


            if (GetTargetedBlock.selectedBlock == null)
            {
                return;
            }

            if (Vector3D.Distance(localPlayer.PositionComp.GetPosition(),
                    GetTargetedBlock.selectedBlock.GetPosition()) > Patches.maxUseDistance)
            {
                utils.ShowMessage("Tether Broke!",
                    $"You Moved More Than {Patches.maxUseDistance}m from tethered block.");
                GetTargetedBlock.selectedBlock = null;
                GetTargetedBlock.selectedObject = null;
                return;
            }

            var equippedTool = MySession.Static.LocalCharacter.HandItemDefinition;
            if (equippedTool != null)
            {
                var toolName = equippedTool.Id.SubtypeName;
                if (toolName.Contains("Welder", StringComparison.OrdinalIgnoreCase))
                {
                    if (localPlayer.BuildPlanner.Count == 0)
                    {
                        return;
                    }
                    DoWelder(localPlayer);
                    return;
                }
                if (toolName.Contains("Grinder", StringComparison.OrdinalIgnoreCase))
                {
                    DoGrinder();
                    return;
                }
                if (toolName.Contains("Drill", StringComparison.OrdinalIgnoreCase))
                {
                    DoDrill();
                    return;
                }
            }
        }

        public static void DoWelder(MyCharacter localPlayer)
        {
            Patches.UseObjectPatch(Patches.maxUseDistance);
            Reflections.Withdraw.Invoke(null, new object[] { (MyEntity)GetTargetedBlock.selectedObject.Owner, localPlayer.GetInventory(0), null });
            Patches.UseObjectPatch(5f);
            return;
        }

        public static void DoGrinder()
        {
            var caster = MySession.Static.LocalCharacter.EquippedTool?.Components?.Get<MyCasterComponent>();
            var hitSlimBlock = caster?.HitBlock;

            // Only loot if actively grinding
            if (MyAPIGateway.Input.IsLeftMousePressed() && hitSlimBlock != null && hitSlimBlock.FatBlock is IMyTerminalBlock terminalBlock && terminalBlock.HasInventory)
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

            // Only deposit if holding control
            if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftControl) || MyAPIGateway.Input.IsKeyPress(MyKeys.RightControl))
            {
                var tetheredInventory = (MyInventory)GetTargetedBlock.selectedBlock.GetInventory();
                var playerInv = MySession.Static.LocalCharacter.GetInventory();
                var playerItems = new List<MyPhysicalInventoryItem>(playerInv.GetItems());

                foreach (var item in playerItems)
                {
                    var contentId = item.Content.GetObjectId();
                    if (contentId.TypeId.IsNull) continue;

                    var typeIdString = contentId.ToString().ToLower();

                    if (!typeIdString.Contains("ore") &&
                        !typeIdString.Contains("ingot") &&
                        !typeIdString.Contains("component")) continue;

                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10000;
                    MyInventory.TransferByPlanner(playerInv, tetheredInventory, contentId, MyItemFlags.None, item.Amount);
                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10;
                }
            }
        }
        public static void DoDrill()
        {
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
        public static int ticks = 0;
    }
}