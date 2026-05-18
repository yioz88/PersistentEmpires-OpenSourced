using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_SpawnFrame : SynchedMissionObject
    {
        public int CastleIndex = -1;
        public bool SpawnFromCastle = false;
        public int FactionIndex = 0;

        public PE_CastleBanner GetCastleBanner()
        {
            try
            {
                CastlesBehavior castlesBehavior = Mission.Current?.GetMissionBehavior<CastlesBehavior>();
                if (castlesBehavior == null || castlesBehavior.castles == null)
                {
                    return null;
                }
                if (!castlesBehavior.castles.ContainsKey(this.CastleIndex))
                {
                    return null;
                }
                return castlesBehavior.castles[this.CastleIndex];
            }
            catch
            {
                return null;
            }
        }

        public bool CanPeerSpawnHere(NetworkCommunicator peer)
        {
            if (peer == null) return false;
            
            PersistentEmpireRepresentative persistentEmpireRepresentative = peer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return false;
            if (persistentEmpireRepresentative.GetFaction() == null)
            {
                return !this.SpawnFromCastle && (this.FactionIndex == 0 || this.FactionIndex == -1);
            }
            if (this.SpawnFromCastle && this.GetCastleBanner() != null)
            {
                return this.GetCastleBanner().FactionIndex == persistentEmpireRepresentative.GetFactionIndex();
            }

            return this.FactionIndex == persistentEmpireRepresentative.GetFactionIndex();
        }

        public PE_SpawnFrame()
        {

        }
    }
}
