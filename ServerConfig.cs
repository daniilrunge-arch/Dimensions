using System.Collections.Generic;

namespace CaxarokLink.Config
{
    public class ServerConfig
    {
        public string CurrentServerName { get; set; }
        public string SecretKey { get; set; }
        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
    }
}
