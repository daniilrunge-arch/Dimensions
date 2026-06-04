namespace CaxarokLink.Models
{
    public class ServerInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string SecretKey { get; set; }
    }

    public class PlayerData
    {
        public string PlayerName { get; set; }
        public byte[] Inventory { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public string SourceServer { get; set; }
    }

    public class OnlineResponse
    {
        public string ServerName { get; set; }
        public int OnlineCount { get; set; }
    }
}
