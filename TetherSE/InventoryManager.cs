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
    class InventoryManager
    {
        private static string FormatQuantity(float amount)
        {
            if (amount >= 1000)
            {
                return $"{Math.Round(amount / 1000f, 2)}k";
            }
            return Math.Round(amount, 1).ToString("F1", CultureInfo.InvariantCulture);
        }

        public static void DoDeposit()
        {
            if (GetTargetedBlock.selectedBlock == null) return;

            var inventory = (MyInventory)GetTargetedBlock.selectedBlock.GetInventory();
            if (inventory == null) return; // Ensure the targeted block has an inventory

            var playerInventory = MySession.Static.LocalCharacter?.GetInventory(); // Use null-conditional operator
            if (playerInventory == null) return; // Ensure the player has an inventory

            var items = new List<MyPhysicalInventoryItem>(playerInventory.GetItems());

            var toolTypes = new List<string> { "Welder", "Grinder", "Drill" };
            var toolTiers = new Dictionary<string, int> {
                { "Elite", 0 },
                { "Proficient", 1 },
                { "Enhanced", 2 }
            };
            int defaultTier = 3;

            var bestTools = new Dictionary<string, Tuple<MyPhysicalInventoryItem, int>>();

            // Find best tools
            foreach (var item in items)
            {
                var subtypeName = item.Content.SubtypeName;
                string matchedToolType = null;
                foreach (var toolType in toolTypes)
                {
                    if (subtypeName.Contains(toolType))
                    {
                        matchedToolType = toolType;
                        break;
                    }
                }

                if (matchedToolType != null)
                {
                    int currentTier = defaultTier;
                    foreach (var tier in toolTiers)
                    {
                        if (subtypeName.Contains(tier.Key))
                        {
                            currentTier = tier.Value;
                            break;
                        }
                    }

                    if (!bestTools.ContainsKey(matchedToolType) || currentTier < bestTools[matchedToolType].Item2)
                    {
                        bestTools[matchedToolType] = Tuple.Create(item, currentTier);
                    }
                }
            }

            var itemsToKeep = new HashSet<uint>();
            foreach (var bestTool in bestTools.Values)
            {
                itemsToKeep.Add(bestTool.Item1.ItemId);
            }

            // Find hydrogen bottle to keep
            var hydrogenBottle = items.FirstOrDefault(i => i.Content.SubtypeName.Contains("HydrogenBottle"));
            if (hydrogenBottle.Content != null)
            {
                itemsToKeep.Add(hydrogenBottle.ItemId);
            }


            // Create a list of items to transfer (excluding items to keep)
            var itemsToTransfer = new List<MyPhysicalInventoryItem>();
            foreach (var item in items)
            {
                if (!itemsToKeep.Contains(item.ItemId))
                {
                    itemsToTransfer.Add(item);
                }
            }

            // Transfer items from player inventory to target inventory
            foreach (var item in itemsToTransfer)
            {
                var amountToTransfer = item.Amount;
                var contentId = item.Content?.GetObjectId();

                if (contentId == null || contentId.Value.TypeId.IsNull) continue;

                // Remove from player inventory
                playerInventory.Remove(item, amountToTransfer);

                // Add to target inventory (should stack automatically)
                inventory.AddItems(amountToTransfer, item.Content);

                string sourceName = "user";
                string destinationName = GetTargetedBlock.selectedBlock.DisplayNameText;
                string itemName = item.Content.SubtypeName;

                LootDisplay.AddMessage(itemName, (float)amountToTransfer, sourceName, destinationName, Color.LimeGreen);
            }
        }
    }
}