using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MarketItem
    {
        public ItemObject Item;
        public int Stock;
        public int MaximumPrice;
        public float CurrentPrice;
        public int MinimumPrice;
        public int Constant;
        public int Stability;
        public int Tier;
        public bool Dirty = false;
        // X is stock Y is price x^(1/stability) * y = k
        public MarketItem(string itemId, int maximumPrice, int minimumPrice, int stability, int tier)
        {
            this.Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.MaximumPrice = maximumPrice;
            this.MinimumPrice = minimumPrice;
            this.Constant = MaximumPrice;
            this.Stability = stability;
            this.CurrentPrice = MathF.Pow(this.Constant, 1f / this.Stability);
            this.Tier = tier;
        }
        public void UpdateReserve(int newStock)
        {
            Dirty = true;

            if (newStock > 999) newStock = 999;
            
            if (newStock < 0) newStock = 0;

            if (newStock < 1)
            {
                this.CurrentPrice = MathF.Pow(this.Constant, 1f / this.Stability);
            }
            else
            {
                this.CurrentPrice = MathF.Pow(this.Constant / (float)newStock, 1f / this.Stability);
            }
            this.Stock = newStock;
        }
        public int BuyPrice()
        {
            float fakeStock = this.Stock;
            if (this.Stock < 2) return this.MaximumPrice;
            float denominator = MathF.Pow(fakeStock - 1, 1f / this.Stability);
            float numerator = this.Constant;
            int buyPrice = Math.Abs((int)((numerator / denominator) - MathF.Pow(this.CurrentPrice, 1f / this.Stability)));
            if (buyPrice < this.MinimumPrice) buyPrice = this.MinimumPrice;
            return buyPrice;
        }
        public int SellPrice()
        {
            float fakeStock = this.Stock;
            if (this.Stock < 1) fakeStock = 1;
            float denominator = MathF.Pow(fakeStock + 1, 1f / this.Stability);
            float numerator = this.Constant;
            int price = Math.Abs((int)(MathF.Pow(this.CurrentPrice, 1f / this.Stability) - (numerator / denominator)));
            if (price < this.MinimumPrice) price = (this.MinimumPrice * 90) / 100;
            return (price * 85) / 100;
        }
    }
    public class CraftingBox
    {
        public ItemObject BoxItem;
        public int MinTierLevel;
        public int MaxTierLevel;
        public CraftingBox(string itemId, string minTierLevel, string maxTierLevel)
        {
            this.BoxItem = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.MinTierLevel = int.Parse(minTierLevel);
            this.MaxTierLevel = int.Parse(maxTierLevel);
        }
    }
    public class PE_StockpileMarket : PE_UsableFromDistance, IMissionObjectHash
    {

        public static int MAX_STOCK_COUNT = 1000;
        public string XmlFile = "examplemarket"; // itemId*minimum*maximum,itemId*minimum*maximum
        public string ModuleFolder = "PersistentEmpires";
        public override bool LockUserFrames
        {
            get
            {
                return false;
            }
        }
        public override bool LockUserPositions
        {
            get
            {
                return false;
            }
        }

        public List<MarketItem> MarketItems { get; private set; }
        public List<CraftingBox> CraftingBoxes { get; private set; }
        public StockpileMarketComponent stockpileMarketComponent { get; private set; }
        protected void LoadMarketItems(string innerText, int tier)
        {

            if (string.IsNullOrEmpty(innerText)) return;
            this.MarketItems = this.MarketItems ?? new List<MarketItem>();
            
            foreach (string marketItemStr in innerText.Trim().Split('|'))
            {
                if (string.IsNullOrWhiteSpace(marketItemStr)) continue;
                
                string[] values = marketItemStr.Split('*');
                if (values.Length < 3)
                {
                    Debug.Print("[PE_StockpileMarket] Invalid market item format: " + marketItemStr, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string itemId = values[0].Trim();
                int minPrice;
                int maxPrice;
                int stability = 10;
                
                if (!int.TryParse(values[1], out minPrice) || !int.TryParse(values[2], out maxPrice))
                {
                    Debug.Print("[PE_StockpileMarket] Invalid price values for item: " + itemId, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                if (values.Length > 4 && !int.TryParse(values[3], out stability))
                {
                    stability = 10;
                }

                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (item == null)
                {
                    Debug.Print(" ERROR IN MARKET SERIALIZATION " + this.XmlFile + " ITEM ID " + itemId + " NOT FOUND !!! ", 0, Debug.DebugColor.Red);
                }
                else
                {
                    this.MarketItems.Add(new MarketItem(itemId, maxPrice, minPrice, stability, tier));
                }
            }
        }
        protected void LoadCraftingBoxes(string innerText)
        {
            this.CraftingBoxes = new List<CraftingBox>();
            if (string.IsNullOrEmpty(innerText)) return;
            
            foreach (string craftingBoxStr in innerText.Trim().Split('|'))
            {
                if (string.IsNullOrWhiteSpace(craftingBoxStr)) continue;
                
                string[] values = craftingBoxStr.Split('*');
                if (values.Length < 3)
                {
                    Debug.Print("[PE_StockpileMarket] Invalid crafting box format: " + craftingBoxStr, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string itemId = values[0].Trim();
                string minTierLevel = values[1].Trim();
                string maxTierLevel = values[2].Trim();
                
                try
                {
                    this.CraftingBoxes.Add(new CraftingBox(itemId, minTierLevel, maxTierLevel));
                }
                catch (Exception e)
                {
                    Debug.Print("[PE_StockpileMarket] Error creating crafting box: " + e.Message, 0, Debug.DebugColor.Red);
                }
            }
        }

        protected override void OnInit()
        {
            base.OnInit();
            TextObject actionMessage = new TextObject("Browse the Market");
            base.ActionMessage = actionMessage;
            TextObject descriptionMessage = new TextObject("Press {KEY} To Browse");
            descriptionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            base.DescriptionMessage = descriptionMessage;
            
            this.stockpileMarketComponent = Mission.Current?.GetMissionBehavior<StockpileMarketComponent>();
            Debug.Print("Initiating Stockpile Market With " + this.ModuleFolder + " Module");
            
            try
            {
                string xmlPath = ModuleHelper.GetXmlPath(this.ModuleFolder, "Markets/" + this.XmlFile);
                if (string.IsNullOrEmpty(xmlPath))
                {
                    Debug.Print("[PE_StockpileMarket] XML path is null or empty!", 0, Debug.DebugColor.Red);
                    return;
                }
                
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlPath);
                this.MarketItems = new List<MarketItem>();
                
                this.LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier1Items")?.InnerText ?? "", 1);
                this.LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier2Items")?.InnerText ?? "", 2);
                this.LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier3Items")?.InnerText ?? "", 3);
                this.LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier4Items")?.InnerText ?? "", 4);
                this.LoadCraftingBoxes(xmlDocument.SelectSingleNode("/Market/CraftingBoxes")?.InnerText ?? "");
            }
            catch (Exception e)
            {
                Debug.Print("[PE_StockpileMarket] Error loading market XML: " + e.Message, 0, Debug.DebugColor.Red);
            }
        }
        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject("Stockpile Market");
        }

        public void OnUse(Agent userAgent)
        {
            if (!base.IsUsable(userAgent))
            {
                userAgent.StopUsingGameObjectMT(false);
                return;
            }
            base.OnUse(userAgent, preferenceIndex);
            Debug.Print("[USING LOG] AGENT USE " + this.GetType().Name);

            if (GameNetwork.IsServer)
            {
                this.stockpileMarketComponent.OpenStockpileMarketForPeer(this, userAgent.MissionPeer.GetNetworkPeer());
                userAgent.StopUsingGameObjectMT(true);
            }
        }
        public void DeserializeStocks(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                return;
            }
            
            string[] elements = serialized.Split('|');
            foreach (string s in elements)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                
                string[] parts = s.Split('*');
                if (parts.Length < 2)
                {
                    Debug.Print("[PE_StockpileMarket] Invalid serialized stock format: " + s, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                string itemId = parts[0].Trim();
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (item == null)
                {
                    Debug.Print(" ERROR IN MARKET SERIALIZATION " + this.XmlFile + " ITEM ID " + itemId + " NOT FOUND !!! ", 0, Debug.DebugColor.Red);
                    continue;
                }
                
                int stock;
                if (!int.TryParse(parts[1], out stock))
                {
                    Debug.Print("[PE_StockpileMarket] Invalid stock value for item: " + itemId, 0, Debug.DebugColor.Yellow);
                    continue;
                }
                
                MarketItem marketItem = this.MarketItems?.Find(m => m.Item?.StringId == item.StringId);
                if (marketItem != null)
                {
                    marketItem.UpdateReserve(stock);
                }
            }
        }
        public string SerializeStocks()
        {
            return string.Join("|", MarketItems.Select(s => s.Item.StringId + "*" + s.Stock));
        }

        public MissionObject GetMissionObject()
        {
            return this;
        }

        protected bool OnHit(Agent victimAgent, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = false;
            if (attackerAgent == null) return false;
            NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
            bool isAdmin = Main.IsPlayerAdmin(player);
            if (isAdmin && weapon.Item != null && weapon.Item.StringId == "pe_adminstockfiller")
            {
                foreach (MarketItem marketItem in this.MarketItems)
                {
                    var currentStock = marketItem.Stock;
                    if (currentStock + 10 < 900)
                    {
                        marketItem.UpdateReserve(currentStock + 10);
                    }
                }
                InformationComponent.Instance.SendMessage("Stocks updated", Colors.Blue.ToUnsignedInteger(), player);
            }
            return true;
        }
    }
}
