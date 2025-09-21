using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace TetherSE
{
    class LootManager
    {
        private static int ticks = 0;
        private const int LOOT_RADIUS = 15;

        public static void Update()
        {
            if (MySession.Static == null || MySession.Static.LocalCharacter == null) return;

            var localPlayer = MySession.Static.LocalCharacter;
            if (localPlayer == null) return;

            if (!(localPlayer.EquippedTool is IMyAngleGrinder) || !MyAPIGateway.Input.IsRightMousePressed())
            {
                return;
            }

            if (ticks < 60) // Lowered polling rate
            {
                ticks++;
                return;
            }
            ticks = 0;

            VRage.Game.ModAPI.IMyInventory playerInventoryConcrete = localPlayer.GetInventory();
            if (playerInventoryConcrete == null) return;

            var playerPosition = localPlayer.PositionComp.GetPosition();
            var boundingSphere = new BoundingSphereD(playerPosition, LOOT_RADIUS);
            var nearbyEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref boundingSphere);

            foreach (var entity in nearbyEntities)
            {
                if (entity == localPlayer) continue;

                if (entity is MyFloatingObject floatingObject && !floatingObject.WasRemovedFromWorld)
                {
                    MyAPIGateway.Utilities.ShowNotification($"TetherSE: Taking floating object: {floatingObject.GetType().Name}", 1000, "Green");
                    (playerInventoryConcrete as MyInventory).TakeFloatingObject(floatingObject);
                    continue;
                }

                if (entity.HasInventory)
                {
                    VRage.Game.ModAPI.IMyInventory sourceInventory = (VRage.Game.ModAPI.IMyInventory)entity.GetInventory();
                    if (sourceInventory != null && sourceInventory.ItemCount > 0)
                    {
                        MyAPIGateway.Utilities.ShowNotification($"TetherSE: Transferring from {entity.GetType().Name} ({sourceInventory.ItemCount} items)", 1000, "Green");
                        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        sourceInventory.GetItems(items);
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            MyAPIGateway.Utilities.ShowNotification($"TetherSE: Attempting to transfer item {i}", 1000, "Yellow");
                            sourceInventory.TransferItemTo((VRage.Game.ModAPI.IMyInventory)playerInventoryConcrete, i, stackIfPossible: true, checkConnection: false);
                        }
                        MyAPIGateway.Utilities.ShowNotification("TetherSE: Transfer complete", 1000, "Green");
                    }
                }
            }
        }
    }
}
