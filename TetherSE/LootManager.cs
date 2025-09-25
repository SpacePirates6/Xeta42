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
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.EntityComponents;

namespace TetherSE
{
    class LootManager
    {
        private static int ticks = 0;
        private const int LOOT_RADIUS = 15;
        private const int MAX_LOOT_DISTANCE = int.MaxValue;
        private const float CONE_ANGLE = 15.0f; // Angle in degrees

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

            if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl) || MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.RightControl))
            {
                GrinderLootUpdate();
            }
            else
            {
                RadiusLootUpdate();
            }
        }

        private static void GrinderLootUpdate()
        {
            if (ticks < 30) // Polling rate
            {
                ticks++;
                return;
            }
            ticks = 0;

            var localPlayer = MySession.Static.LocalCharacter;
            VRage.Game.ModAPI.IMyInventory playerInventoryConcrete = localPlayer.GetInventory();
            if (playerInventoryConcrete == null) return;

            MatrixD headMatrix = localPlayer.GetHeadMatrix(true);
            Vector3D viewDirection = headMatrix.Forward;
            Vector3D startPosition = headMatrix.Translation;

            var boundingSphere = new BoundingSphereD(startPosition, MAX_LOOT_DISTANCE);
            var nearbyEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref boundingSphere);

            foreach (var entity in nearbyEntities)
            {
                if (entity == localPlayer || !entity.HasInventory) continue;

                Vector3D entityPosition = entity.PositionComp.GetPosition();
                Vector3D directionToEntity = Vector3D.Normalize(entityPosition - startPosition);
                double angle = Math.Acos(Vector3D.Dot(viewDirection, directionToEntity)) * (180.0 / Math.PI);

                if (angle <= CONE_ANGLE)
                {
                    VRage.Game.ModAPI.IMyInventory sourceInventory = (VRage.Game.ModAPI.IMyInventory)entity.GetInventory();
                    if (sourceInventory != null && sourceInventory.ItemCount > 0)
                    {
                        TransferAllItems(sourceInventory, playerInventoryConcrete, entity);
                    }
                }
            }
        }

        private static void RadiusLootUpdate()
        {
            if (ticks < 60) // Lowered polling rate
            {
                ticks++;
                return;
            }
            ticks = 0;

            var localPlayer = MySession.Static.LocalCharacter;
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
                    (playerInventoryConcrete as MyInventory).TakeFloatingObject(floatingObject);

                    string sourceName = "World"; // Floating objects are from the world
                    string destinationName = "user";
                    string itemName = floatingObject.Item.Content.SubtypeName;

                    LootDisplay.AddMessage(itemName, (float)floatingObject.Item.Amount, sourceName, destinationName, Color.OrangeRed);
                    continue;
                }

                if (entity.HasInventory)
                {
                    VRage.Game.ModAPI.IMyInventory sourceInventory = (VRage.Game.ModAPI.IMyInventory)entity.GetInventory();
                    if (sourceInventory != null && sourceInventory.ItemCount > 0)
                    {
                        TransferAllItems(sourceInventory, playerInventoryConcrete, entity);
                    }
                }
            }
        }

        private static void TransferAllItems(VRage.Game.ModAPI.IMyInventory sourceInventory, VRage.Game.ModAPI.IMyInventory destinationInventory, IMyEntity sourceEntity)
        {
            var myInventory = sourceInventory as MyInventory;
            if (myInventory == null) return;

            var items = new List<MyPhysicalInventoryItem>(myInventory.GetItems());

            if (items.Count > 0)
            {
                LootDisplay.AddMessage($"Transferring from {sourceEntity.DisplayName ?? sourceEntity.GetType().Name}", Color.LightGray);
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                var contentId = item.Content.GetObjectId();
                var amount = item.Amount;

                MyInventory.TransferByPlanner(sourceInventory as MyInventory, destinationInventory as MyInventory, contentId, MyItemFlags.None, amount);

                string sourceName = sourceEntity.DisplayName ?? sourceEntity.GetType().Name;
                string destinationName = "user";
                string itemName = item.Content.SubtypeName;
                string quantity = FormatQuantity((float)item.Amount);
                LootDisplay.AddMessage($"({sourceName}) ------> {itemName} ({quantity}) to ({destinationName})", Color.OrangeRed);
            }

            if (items.Count > 0)
            {
                LootDisplay.AddMessage("Transfer complete", Color.LightGray);
            }
        }
    }
}