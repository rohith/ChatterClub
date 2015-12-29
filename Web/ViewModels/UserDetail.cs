using Newtonsoft.Json;

namespace CreativeColon.ChatterClub.Web.ViewModels
{
    public class UserDetail
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
