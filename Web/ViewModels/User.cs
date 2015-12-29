using System.Collections.Generic;

namespace CreativeColon.ChatterClub.Web.ViewModels
{
    public class User
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string AuthenticationCode { get; set; }
        public Dictionary<string, Connection> Connections { get; set; }

        public User()
        {
            Connections = new Dictionary<string, Connection>();
        }
    }
}
