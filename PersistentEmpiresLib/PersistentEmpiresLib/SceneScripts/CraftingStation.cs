using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.SceneScripts
{
    public struct CraftingRecipe
    {
        public ItemObject Item;
        public int NeededCount;
        public CraftingRecipe(String itemId, int neededCount)
        {
            this.Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.NeededCount = neededCount;
        }
    }
    public struct Craftable
    {
        public List<CraftingRecipe> Recipe;
        public int OutputCount;
        public ItemObject Item;
        public int Tier;
        public int RequiredEngineering;
        public int CraftTime;
        public SkillObject RelevantSkill;
        public Craftable(List<CraftingRecipe> receipts, String itemId, int outputCount, int tier, int requiredEngineering, int craftTime, string relevantSkill)
        {
            this.Recipe = receipts;
            this.Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.OutputCount = outputCount;
            this.Tier = tier;
            this.RequiredEngineering = requiredEngineering;
            this.CraftTime = craftTime;
            this.RelevantSkill = MBObjectManager.Instance.GetObject<SkillObject>(relevantSkill);
        }
    }
    public class PE_CraftingStation : PE_UsableFromDistance
    {
        public string StationName = "Carpenter Bench";
        public string ModuleFolder = "PersistentEmpires";
        public CraftingComponent craftingComponent { get; private set; }
        public PE_UpgradeableBuildings upgradeableBuilding { get; private set; }
        private PlayerInventoryComponent playerInventoryComponent;
        public List<Craftable> Craftables { get; private set; }
        public string Animation = "";
        public string CraftingRecieptTag = "";
        public string RelevantSkillId = "Engineering";
        private string Tier1Craftings = "";
        private string Tier2Craftings = "";
        private string Tier3Craftings = "";
        public List<Craftable> ParseStringToCraftables(string allCraftableReceipt, int tier, int requiredEngineering)
        {
            List<Craftable> craftables = new List<Craftable>();
            if (string.IsNullOrEmpty(allCraftableReceipt)) return craftables;
            
            string[] receipts = allCraftableReceipt.Split('|');
            foreach (string receipt in receipts)
            {
                if (string.IsNullOrWhiteSpace(receipt)) continue;
                
                string[] sides = receipt.Split('=');
                if (sides.Length < 2)
                {
                    Debug.Print("[PE_CraftingStation] Invalid crafting receipt format: " + receipt, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string leftSide = sides[0].Trim();
                string rightSide = sides[1].Trim();
                List<CraftingRecipe> cReceipts = new List<CraftingRecipe>();
                
                string[] rightParts = rightSide.Split(',');
                foreach (string r in rightParts)
                {
                    if (string.IsNullOrWhiteSpace(r)) continue;
                    
                    string[] itemParts = r.Split('*');
                    if (itemParts.Length < 2)
                    {
                        Debug.Print("[PE_CraftingStation] Invalid recipe item format: " + r, 0, Debug.DebugColor.Yellow);
                        continue;
                    }
                    
                    string itemId = itemParts[0].Trim();
                    if (!int.TryParse(itemParts[1].Trim(), out int count))
                    {
                        Debug.Print("[PE_CraftingStation] Invalid count for item: " + itemId, 0, Debug.DebugColor.Yellow);
                        continue;
                    }
                    
                    cReceipts.Add(new CraftingRecipe(itemId, count));
                }
                
                if (cReceipts.Count == 0)
                {
                    Debug.Print("[PE_CraftingStation] No valid recipes in receipt: " + receipt, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string[] leftParts = leftSide.Split('*');
                if (leftParts.Length < 3)
                {
                    Debug.Print("[PE_CraftingStation] Invalid left side format: " + leftSide, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                if (!int.TryParse(leftParts[0].Trim(), out int craftTime))
                {
                    Debug.Print("[PE_CraftingStation] Invalid craft time: " + leftParts[0], 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string craftableItemId = leftParts[1].Trim();
                
                if (!int.TryParse(leftParts[2].Trim(), out int outputAmount))
                {
                    Debug.Print("[PE_CraftingStation] Invalid output amount: " + leftParts[2], 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                Craftable craftable = new Craftable(cReceipts, craftableItemId, outputAmount, tier, requiredEngineering, craftTime, this.RelevantSkillId);
                craftables.Add(craftable);
            }
            return craftables;
        }
        public void LoadCraftables()
        {
            List<Craftable> tier1Crafts = this.ParseStringToCraftables(this.Tier1Craftings, 1, this.upgradeableBuilding.Tier1CraftingEngineering);
            List<Craftable> tier2Crafts = this.ParseStringToCraftables(this.Tier2Craftings, 2, this.upgradeableBuilding.Tier2CraftingEngineering);
            List<Craftable> tier3Crafts = this.ParseStringToCraftables(this.Tier3Craftings, 3, this.upgradeableBuilding.Tier3CraftingEngineering);
            this.Craftables = new List<Craftable>();
            this.Craftables.AddRange(tier1Crafts);
            this.Craftables.AddRange(tier2Crafts);
            this.Craftables.AddRange(tier3Crafts);
        }
        protected override void OnInit()
        {
            base.OnInit();
            TextObject actionMessage = new TextObject("Use {Station} To Craft");
            actionMessage.SetTextVariable("Station", this.StationName);
            base.ActionMessage = actionMessage;
            TextObject descriptionMessage = new TextObject("Press {KEY} To Interact");
            descriptionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            base.DescriptionMessage = descriptionMessage;
            this.upgradeableBuilding = base.GameEntity.Parent.Parent.GetFirstScriptOfType<PE_UpgradeableBuildings>();
            if (base.GameEntity.Parent.Parent.GetFirstChildEntityWithTag(this.CraftingRecieptTag) != null)
            {
                PE_CraftingReceipt craftingRecieptEntity = base.GameEntity.Parent.Parent.GetFirstChildEntityWithTag(this.CraftingRecieptTag).GetFirstScriptOfType<PE_CraftingReceipt>();
                this.Tier1Craftings = craftingRecieptEntity.Tier1Craftings;
                this.Tier2Craftings = craftingRecieptEntity.Tier2Craftings;
                this.Tier3Craftings = craftingRecieptEntity.Tier3Craftings;
            }
            else
            {
                string xmlPath = ModuleHelper.GetXmlPath(this.ModuleFolder, "CraftingRecipies/" + this.CraftingRecieptTag);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlPath);
                foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                {
                    if (node.Name == "Tier1Craftings") this.Tier1Craftings = node.InnerText.Trim();
                    else if (node.Name == "Tier2Craftings") this.Tier2Craftings = node.InnerText.Trim();
                    else if (node.Name == "Tier3Craftings") this.Tier3Craftings = node.InnerText.Trim();
                }
            }
            this.playerInventoryComponent = Mission.Current.GetMissionBehavior<PlayerInventoryComponent>();
            this.craftingComponent = Mission.Current.GetMissionBehavior<CraftingComponent>();
            this.LoadCraftables();
        }
        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject("Crafting Station Named As " + this.StationName);
        }



        public void OnUse(Agent userAgent)
        {
            Debug.Print("[USING LOG] AGENT USE " + this.GetType().Name);
            if (!base.IsUsable(userAgent))
            {
                userAgent.StopUsingGameObjectMT(false);
                return;
            }
            base.OnUse(userAgent);
            userAgent.StopUsingGameObjectMT(true);
            if (GameNetwork.IsServer)
            {
                this.craftingComponent.AgentRequestCrafting(userAgent, this);
            }

        }
    }
}
