using System.Collections.Generic;

namespace CreativeColon.ChatterClub.Web.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DeivceCode { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }

        public Person()
        {
            Rooms = new HashSet<Room>();
        }
    }
}
