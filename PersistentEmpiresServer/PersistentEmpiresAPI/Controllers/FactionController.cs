using Microsoft.AspNetCore.Mvc;
using PersistentEmpiresAPI.DTO;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresSave.Database;
using PersistentEmpiresSave.Database.Repositories;
using System.Net;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FactionController : ControllerBase
    {
        [HttpGet]
        public ActionResult<ResultDTO> Index()
        {
            return new ResultDTO
            {
                Status = true,
                Reason = "Faction API is running"
            };
        }

        [HttpGet("list")]
        public ActionResult<ResultDTO> GetFactions()
        {
            var missionBehavior = Mission.Current?.GetMissionBehavior<FactionsBehavior>();
            if (missionBehavior == null)
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Mission not found"
                };
            }

            var factions = new List<FactionInfoDTO>();
            foreach (var kvp in missionBehavior.Factions)
            {
                Faction faction = kvp.Value;
                factions.Add(new FactionInfoDTO
                {
                    FactionIndex = kvp.Key,
                    Name = faction.name,
                    LeaderId = faction.lordId,
                    Treasury = faction.treasury,
                    CultureId = faction.basicCultureObject?.StringId ?? "",
                    IsAtWar = faction.IsWarDeclared(),
                    IsAtPeace = faction.IsPeaceDeclared()
                });
            }

            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(factions)
            };
        }

        [HttpGet("{factionIndex}")]
        public ActionResult<ResultDTO> GetFaction(int factionIndex)
        {
            var missionBehavior = Mission.Current?.GetMissionBehavior<FactionsBehavior>();
            if (missionBehavior == null || !missionBehavior.Factions.ContainsKey(factionIndex))
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Faction not found"
                };
            }

            Faction faction = missionBehavior.Factions[factionIndex];
            var members = new List<int>();
            
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.IsConnectionActive)
                {
                    PersistentEmpireRepresentative rep = communicator.GetComponent<PersistentEmpireRepresentative>();
                    if (rep != null && rep.GetFactionIndex() == factionIndex)
                    {
                        members.Add(communicator.VirtualPlayer.ToPlayerId());
                    }
                }
            }

            var factionInfo = new FactionDetailDTO
            {
                FactionIndex = factionIndex,
                Name = faction.name,
                LeaderId = faction.lordId,
                Treasury = faction.treasury,
                CultureId = faction.basicCultureObject?.StringId ?? "",
                Members = members,
                Marshalls = faction.marshalls,
                DoorManagers = faction.doorManagers,
                Wars = faction.wars,
                Peaces = faction.peaces
            };

            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(factionInfo)
            };
        }

        [HttpPost("setleader")]
        public ActionResult<ResultDTO> SetFactionLeader([FromBody] SetLeaderDTO request)
        {
            var missionBehavior = Mission.Current?.GetMissionBehavior<FactionsBehavior>();
            if (missionBehavior == null || !missionBehavior.Factions.ContainsKey(request.FactionIndex))
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Faction not found"
                };
            }

            Faction faction = missionBehavior.Factions[request.FactionIndex];
            faction.lordId = request.PlayerId;

            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.VirtualPlayer.ToPlayerId() == request.PlayerId)
                {
                    InformationComponent.Instance.BroadcastMessage($"{communicator.UserName} is now the leader of {faction.name}!", 0xFF00FF00);
                    break;
                }
            }

            return new ResultDTO
            {
                Status = true,
                Reason = $"Set leader for faction {request.FactionIndex}"
            };
        }

        [HttpPost("settreasury")]
        public ActionResult<ResultDTO> SetTreasury([FromBody] SetTreasuryDTO request)
        {
            var missionBehavior = Mission.Current?.GetMissionBehavior<FactionsBehavior>();
            if (missionBehavior == null || !missionBehavior.Factions.ContainsKey(request.FactionIndex))
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Faction not found"
                };
            }

            Faction faction = missionBehavior.Factions[request.FactionIndex];
            faction.treasury = request.Amount;

            return new ResultDTO
            {
                Status = true,
                Reason = $"Set treasury for faction {request.FactionIndex} to {request.Amount}"
            };
        }

        [HttpGet("wars")]
        public ActionResult<ResultDTO> GetWars()
        {
            var missionBehavior = Mission.Current?.GetMissionBehavior<FactionsBehavior>();
            if (missionBehavior == null)
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Mission not found"
                };
            }

            var wars = new List<WarInfoDTO>();
            foreach (var kvp in missionBehavior.Factions)
            {
                foreach (var war in kvp.Value.wars)
                {
                    wars.Add(new WarInfoDTO
                    {
                        Faction1 = kvp.Key,
                        Faction2 = war,
                        Started = true
                    });
                }
            }

            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(wars)
            };
        }
    }

    public class FactionInfoDTO
    {
        public int FactionIndex { get; set; }
        public string Name { get; set; }
        public string LeaderId { get; set; }
        public int Treasury { get; set; }
        public string CultureId { get; set; }
        public bool IsAtWar { get; set; }
        public bool IsAtPeace { get; set; }
    }

    public class FactionDetailDTO
    {
        public int FactionIndex { get; set; }
        public string Name { get; set; }
        public string LeaderId { get; set; }
        public int Treasury { get; set; }
        public string CultureId { get; set; }
        public List<int> Members { get; set; }
        public List<string> Marshalls { get; set; }
        public List<string> DoorManagers { get; set; }
        public List<string> Wars { get; set; }
        public List<string> Peaces { get; set; }
    }

    public class SetLeaderDTO
    {
        public int FactionIndex { get; set; }
        public string PlayerId { get; set; }
    }

    public class SetTreasuryDTO
    {
        public int FactionIndex { get; set; }
        public int Amount { get; set; }
    }

    public class WarInfoDTO
    {
        public int Faction1 { get; set; }
        public string Faction2 { get; set; }
        public bool Started { get; set; }
    }
}
