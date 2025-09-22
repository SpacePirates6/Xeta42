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
using System.Globalization;

namespace TetherSE
{
    class LootManager
    {
        private static int ticks = 0;
        private const int LOOT_RADIUS = 15;

        private static string FormatQuantity(float amount)
        {
            if (amount >= 1000)
            {
                return $"{Math.Round(amount / 1000f, 2)}k";
            }
            return Math.Round(amount, 1).ToString("F1", CultureInfo.InvariantCulture);
        }

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

                    string sourceName = "World"; // Floating objects are from the world
                    string destinationName = "user";
                    string itemName = floatingObject.Item.Content.SubtypeName;
                    string quantity = FormatQuantity((float)floatingObject.Item.Amount);

                    LootDisplay.AddMessage($"({sourceName}) ------> {itemName} ({quantity}) to ({destinationName})", Color.Red);
                    continue;
                }

                if (entity.HasInventory)
                {
                    // Only loot containers if Control is held down
                    if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl) || MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.RightControl))
                    {
                        VRage.Game.ModAPI.IMyInventory sourceInventory = (VRage.Game.ModAPI.IMyInventory)entity.GetInventory();
                        if (sourceInventory != null && sourceInventory.ItemCount > 0)
                        {
                            LootDisplay.AddMessage($"Transferring from {entity.GetType().Name} ({FormatQuantity((float)sourceInventory.ItemCount)} items)", Color.White);
                            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                            sourceInventory.GetItems(items);
                            for (int i = items.Count - 1; i >= 0; i--)
                            {
                                var itemToTransfer = items[i];
                                string sourceName = entity.DisplayName ?? entity.GetType().Name;
                                string destinationName = "user";
                                string itemName = itemToTransfer.Type.SubtypeId;
                                string quantity = FormatQuantity((float)itemToTransfer.Amount);

                                sourceInventory.TransferItemTo((VRage.Game.ModAPI.IMyInventory)playerInventoryConcrete, i, stackIfPossible: true, checkConnection: false);
                                LootDisplay.AddMessage($"({sourceName}) ------> {itemName} ({quantity}) to ({destinationName})", Color.Red);
                            }
                            LootDisplay.AddMessage("Transfer complete", Color.White);
                        }
                    }
                }
            }
        }
    }
}
