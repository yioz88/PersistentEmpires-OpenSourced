using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_TeleportDoor : PE_UsableFromDistance
    {
        private PE_TeleportDoor LinkedDoor { get; set; }
        public string LinkText = "Same Unique Text With Other Door";
        public int CastleId = -1;
        public bool Lockpickable = true;
        private bool AllowMembersWithoutKeys = false;
        public void SetLinkedDoor(PE_TeleportDoor LinkedDoor)
        {
            this.LinkedDoor = LinkedDoor;
        }
        protected override void OnInit()
        {
            base.OnInit();
            base.ActionMessage = new TextObject("Door");
            TextObject descriptionMessage = new TextObject("Press {KEY} To Use");
            descriptionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            base.DescriptionMessage = descriptionMessage;
        }
        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject("Use Door");
        }
        public PE_CastleBanner GetCastleBanner()
        {
            try
            {
                CastlesBehavior castleBehaviors = Mission.Current?.GetMissionBehavior<CastlesBehavior>();
                if (castleBehaviors == null)
                {
                    return null;
                }
                if (castleBehaviors.castles == null || !castleBehaviors.castles.ContainsKey(this.CastleId))
                {
                    return null;
                }
                return castleBehaviors.castles[this.CastleId];
            }
            catch (Exception e)
            {
                Debug.Print("[PE_TeleportDoor] GetCastleBanner error: " + e.Message, 0, Debug.DebugColor.Red);
                return null;
            }
        }
        
        protected bool OnHit(Agent victimAgent, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = false;
            if (this.Lockpickable == false) return false;
            if (this.CastleId == -1) return false;
            return false;
        }

        public bool CanPlayerUse(Agent userAgent)
        {
            if (this.CastleId == -1) return true;
            
            PE_CastleBanner castleBanner = this.GetCastleBanner();
            if (castleBanner == null)
            {
                return true;
            }
            
            Faction f = castleBanner.GetOwnerFaction();
            if (f == null)
            {
                return true;
            }
            
            if (userAgent?.MissionPeer == null || userAgent.MissionPeer.GetNetworkPeer() == null)
            {
                return false;
            }
            
            NetworkCommunicator player = userAgent.MissionPeer.GetNetworkPeer();
            PersistentEmpireRepresentative persistentEmpireRepresentative = player?.GetComponent<PersistentEmpireRepresentative>();
            
            if (persistentEmpireRepresentative == null)
            {
                return false;
            }
            
            if (this.AllowMembersWithoutKeys && persistentEmpireRepresentative.GetFaction() == f) return true;
            if (f.doorManagers.Contains(player.VirtualPlayer.ToPlayerId()) || f.marshalls.Contains(player.VirtualPlayer.ToPlayerId()) || f.lordId == player.VirtualPlayer.ToPlayerId()) return true;
            return false;
        }
        
        private bool IsWithinDistance(Agent userAgent)
        {
            Vec3 userPosition = userAgent.Position;
            
            if (userAgent.MountAgent != null)
            {
                userPosition = userAgent.MountAgent.Position;
            }
            
            float distance = base.GameEntity.GetGlobalFrame().origin.Distance(userPosition);
            return distance <= this.Distance;
        }
        
        public override bool IsDisabledForAgent(Agent agent)
        {
            if (base.IsDisabledForAgent(agent))
            {
                return true;
            }
            
            if (agent.MountAgent != null)
            {
                return !this.IsWithinDistance(agent);
            }
            
            return false;
        }
        
        public void OnUse(Agent userAgent)
        {
            Debug.Print("[USING LOG] AGENT USE " + this.GetType().Name);

            if (!base.IsUsable(userAgent))
            {
                userAgent.StopUsingGameObjectMT(false);
                return;
            }
            
            if (userAgent.MountAgent != null && !this.IsWithinDistance(userAgent))
            {
                InformationComponent.Instance.SendMessage("Your mount is too far from the door!", new Color(1f, 0.5f, 0f).ToUnsignedInteger(), userAgent.MissionPeer.GetNetworkPeer());
                userAgent.StopUsingGameObjectMT(false);
                return;
            }
            
            base.OnUse(userAgent, preferenceIndex);
            userAgent.StopUsingGameObjectMT(true);
            if (GameNetwork.IsServer)
            {
                if (LinkedDoor != null)
                {
                    bool canPlayerUse = this.CanPlayerUse(userAgent);
                    NetworkCommunicator player = userAgent.MissionPeer?.GetNetworkPeer();
                    if (player == null) return;
                    
                    if (this.CastleId > -1)
                    {
                        PE_RepairableDestructableComponent destructComponent = base.GameEntity.GetFirstScriptOfType<PE_RepairableDestructableComponent>();
                        if (destructComponent != null && destructComponent.IsBroken) canPlayerUse = true;
                    }
                    if (canPlayerUse)
                    {
                        GameEntity teleportPosEntity = this.LinkedDoor.GameEntity?.GetFirstChildEntityWithTag("position");
                        if (teleportPosEntity == null)
                        {
                            Debug.Print("[PE_TeleportDoor] Teleport position entity not found!", 0, Debug.DebugColor.Red);
                            InformationComponent.Instance.SendMessage("Teleport destination not found!", new Color(1f, 0f, 0f).ToUnsignedInteger(), player);
                            return;
                        }
                        
                        Vec3 teleportPosition = teleportPosEntity.GlobalPosition;
                        
                        if (userAgent.MountAgent != null)
                        {
                            Agent mountAgent = userAgent.MountAgent;
                            mountAgent.TeleportToPosition(teleportPosition);
                        }
                        
                        userAgent.TeleportToPosition(teleportPosition);
                    }
                    else
                    {
                        PE_CastleBanner castleBanner = this.GetCastleBanner();
                        if (castleBanner != null)
                        {
                            Faction f = castleBanner.GetOwnerFaction();
                            if (f != null)
                            {
                                InformationComponent.Instance.SendMessage("This door is locked by " + f.name, 0x0606c2d9, player);
                            }
                        }
                    }
                }
            }
        }
    }
}
