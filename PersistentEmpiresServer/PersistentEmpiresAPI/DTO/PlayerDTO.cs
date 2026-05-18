namespace PersistentEmpiresAPI.DTO
{
    public class PlayerInfoDTO
    {
        public int PlayerId { get; set; }
        public string Username { get; set; }
        public int FactionIndex { get; set; }
        public int Gold { get; set; }
        public string ClassId { get; set; }
        public string IpAddress { get; set; }
    }

    public class PlayerStatsDTO
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public bool Online { get; set; }
        public int Gold { get; set; }
        public int FactionIndex { get; set; }
        public string ClassId { get; set; }
        public int Health { get; set; }
        public int Hunger { get; set; }
        public string Position { get; set; }
    }

    public class ChangeGoldDTO
    {
        public int PlayerId { get; set; }
        public int Amount { get; set; }
    }

    public class SetClassDTO
    {
        public int PlayerId { get; set; }
        public string ClassId { get; set; }
    }

    public class AddSkillPointsDTO
    {
        public int PlayerId { get; set; }
        public string SkillName { get; set; }
        public int Points { get; set; }
    }
}
