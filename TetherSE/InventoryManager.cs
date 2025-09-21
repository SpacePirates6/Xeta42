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
            if (GetTargetedBlock.selectedBlock == null)
            {
                return;
            }

            var inventory = (MyInventory)GetTargetedBlock.selectedBlock.GetInventory();
            var playerInventory = MySession.Static.LocalCharacter.GetInventory();
            var items = new List<MyPhysicalInventoryItem>(playerInventory.GetItems());

            var toolTypes = new List<string> { "Welder", "Grinder", "Drill" };
            var toolTiers = new List<string> { "Elite", "Proficient", "Enhanced" }; // Highest to lowest

            var bestTools = new Dictionary<string, MyPhysicalInventoryItem>();

            // Find the best tool of each type
            foreach (var item in items)
            {
                var subtypeName = item.Content.SubtypeName;
                foreach (var toolType in toolTypes)
                {
                    if (subtypeName.Contains(toolType))
                    {
                        if (!bestTools.ContainsKey(toolType))
                        {
                            bestTools[toolType] = item;
                        }
                        else
                        {
                            var existingTool = bestTools[toolType];
                            var existingTier = toolTiers.Count;
                            var currentTier = toolTiers.Count;

                            for (int i = 0; i < toolTiers.Count; i++)
                            {
                                if (existingTool.Content.SubtypeName.Contains(toolTiers[i]))
                                {
                                    existingTier = i;
                                    break;
                                }
                            }

                            for (int i = 0; i < toolTiers.Count; i++)
                            {
                                if (subtypeName.Contains(toolTiers[i]))
                                {
                                    currentTier = i;
                                    break;
                                }
                            }

                            if (currentTier < existingTier)
                            {
                                bestTools[toolType] = item;
                            }
                        }
                    }
                }
            }

            var itemsToKeep = new HashSet<uint>();
            foreach(var item in bestTools.Values)
            {
                itemsToKeep.Add(item.ItemId);
            }

            foreach (var item in items)
            {
                var amountToTransfer = item.Amount;
                if (itemsToKeep.Contains(item.ItemId))
                {
                    amountToTransfer = amountToTransfer - (MyFixedPoint)1;
                }

                if (amountToTransfer > 0)
                {
                    var contentId = item.Content.GetObjectId();
                    if (contentId.TypeId.IsNull) continue;

                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10000;
                    MyInventory.TransferByPlanner(playerInventory, inventory, contentId, MyItemFlags.None, amountToTransfer);
                    MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 10;

                    string sourceName = "user";
                    string destinationName = GetTargetedBlock.selectedBlock.DisplayNameText;
                    string itemName = item.Content.SubtypeName;
                    string quantity = FormatQuantity((float)amountToTransfer);

                    LootDisplay.AddMessage($"({sourceName}) ------> {itemName} ({quantity}) to ({destinationName})", Color.Green);
                }
            }
        }
    }
}