using Microsoft.AspNetCore.Mvc;
using PersistentEmpiresAPI.DTO;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresSave.Database;
using PersistentEmpiresSave.Database.Repositories;
using System.Net;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace PersistentEmpiresAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayerController : ControllerBase
    {
        [HttpGet]
        public ActionResult<ResultDTO> Index()
        {
            return new ResultDTO
            {
                Status = true,
                Reason = "Player API is running"
            };
        }

        [HttpGet("online")]
        public ActionResult<ResultDTO> GetOnlinePlayers()
        {
            var players = new List<PlayerInfoDTO>();
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.IsConnectionActive)
                {
                    PersistentEmpireRepresentative rep = communicator.GetComponent<PersistentEmpireRepresentative>();
                    players.Add(new PlayerInfoDTO
                    {
                        PlayerId = communicator.VirtualPlayer.ToPlayerId(),
                        Username = communicator.UserName,
                        FactionIndex = rep?.GetFactionIndex() ?? -1,
                        Gold = rep?.Gold ?? 0,
                        ClassId = rep?.GetClassId() ?? "",
                        IpAddress = GetIpAddress(communicator)
                    });
                }
            }
            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(players)
            };
        }

        [HttpGet("{playerId}")]
        public ActionResult<ResultDTO> GetPlayer(int playerId)
        {
            IEnumerable<DBPlayer> players = DBPlayerRepository.GetPlayerFromId(playerId);
            if (players.Count() == 0)
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Player not found"
                };
            }
            DBPlayer player = players.First();
            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(player)
            };
        }

        [HttpGet("{playerId}/stats")]
        public ActionResult<ResultDTO> GetPlayerStats(int playerId)
        {
            IEnumerable<DBPlayer> players = DBPlayerRepository.GetPlayerFromId(playerId);
            if (players.Count() == 0)
            {
                return new ResultDTO
                {
                    Status = false,
                    Reason = "Player not found"
                };
            }
            DBPlayer player = players.First();
            
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.VirtualPlayer.ToPlayerId() == playerId && communicator.IsConnectionActive)
                {
                    PersistentEmpireRepresentative rep = communicator.GetComponent<PersistentEmpireRepresentative>();
                    return new ResultDTO
                    {
                        Status = true,
                        Reason = System.Text.Json.JsonSerializer.Serialize(new PlayerStatsDTO
                        {
                            PlayerId = playerId,
                            Name = player.Name,
                            Online = true,
                            Gold = rep?.Gold ?? player.Money,
                            FactionIndex = rep?.GetFactionIndex() ?? player.FactionIndex,
                            ClassId = rep?.GetClassId() ?? player.Class,
                            Health = rep?.LoadedHealth ?? 100,
                            Hunger = rep?.GetHunger() ?? 100,
                            Position = rep?.LoadedDbPosition.ToString() ?? "Unknown"
                        })
                    };
                }
            }
            
            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(new PlayerStatsDTO
                {
                    PlayerId = playerId,
                    Name = player.Name,
                    Online = false,
                    Gold = player.Money,
                    FactionIndex = player.FactionIndex,
                    ClassId = player.Class,
                    Health = player.Health > 0 ? player.Health : 100,
                    Hunger = player.Hunger > 0 ? player.Hunger : 100,
                    Position = $"({player.PosX}, {player.PosY}, {player.PosZ})"
                })
            };
        }

        [HttpGet("teleport/{playerId}")]
        public ActionResult<ResultDTO> TeleportPlayer(int playerId, [FromQuery] float x, [FromQuery] float y, [FromQuery] float z)
        {
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.VirtualPlayer.ToPlayerId() == playerId && communicator.IsConnectionActive)
                {
                    if (communicator.ControlledAgent != null && communicator.ControlledAgent.IsActive())
                    {
                        Vec3 newPosition = new Vec3(x, y, z);
                        communicator.ControlledAgent.TeleportToPosition(newPosition);
                        return new ResultDTO
                        {
                            Status = true,
                            Reason = $"Player {playerId} teleported to ({x}, {y}, {z})"
                        };
                    }
                    return new ResultDTO
                    {
                        Status = false,
                        Reason = "Player has no active agent"
                    };
                }
            }
            return new ResultDTO
            {
                Status = false,
                Reason = "Player not found or not online"
            };
        }

        [HttpPost("changegold")]
        public ActionResult<ResultDTO> ChangeGold([FromBody] ChangeGoldDTO request)
        {
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.VirtualPlayer.ToPlayerId() == request.PlayerId && communicator.IsConnectionActive)
                {
                    PersistentEmpireRepresentative rep = communicator.GetComponent<PersistentEmpireRepresentative>();
                    if (rep != null)
                    {
                        if (request.Amount > 0)
                        {
                            rep.GoldGain(request.Amount);
                        }
                        else
                        {
                            rep.GoldLost(Math.Abs(request.Amount));
                        }
                        return new ResultDTO
                        {
                            Status = true,
                            Reason = $"Changed gold for player {request.PlayerId}"
                        };
                    }
                }
            }
            return new ResultDTO
            {
                Status = false,
                Reason = "Player not found"
            };
        }

        [HttpPost("setclass")]
        public ActionResult<ResultDTO> SetPlayerClass([FromBody] SetClassDTO request)
        {
            foreach (NetworkCommunicator communicator in GameNetwork.NetworkPeers.ToArray())
            {
                if (communicator.VirtualPlayer.ToPlayerId() == request.PlayerId && communicator.IsConnectionActive)
                {
                    PersistentEmpireRepresentative rep = communicator.GetComponent<PersistentEmpireRepresentative>();
                    if (rep != null)
                    {
                        rep.SetClass(request.ClassId);
                        
                        string query = "UPDATE Players SET Class = @Class WHERE PlayerId = @PlayerId";
                        DBConnection.ExecuteDapper(query, new
                        {
                            PlayerId = request.PlayerId,
                            Class = request.ClassId
                        });
                        
                        return new ResultDTO
                        {
                            Status = true,
                            Reason = $"Changed class for player {request.PlayerId} to {request.ClassId}"
                        };
                    }
                }
            }
            return new ResultDTO
            {
                Status = false,
                Reason = "Player not found"
            };
        }

        [HttpGet("hero/list")]
        public ActionResult<ResultDTO> GetHeroClasses()
        {
            var heroClasses = new List<string>
            {
                "pe_peasant",
                "pe_peasant_female",
                "pe_militia",
                "pe_militia_female",
                "pe_footman",
                "pe_footman_female",
                "pe_sergeant",
                "pe_sergeant_female",
                "pe_knight",
                "pe_knight_female",
                "pe_lord",
                "pe_lady",
                "pe_hero_warrior",
                "pe_hero_knight",
                "pe_hero_archer"
            };
            return new ResultDTO
            {
                Status = true,
                Reason = System.Text.Json.JsonSerializer.Serialize(heroClasses)
            };
        }

        private string GetIpAddress(NetworkCommunicator peer)
        {
            try
            {
                byte[] bytes = BitConverter.GetBytes(peer.GetHost());
                return new IPAddress(bytes).ToString();
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
