using System.Collections.Generic;

namespace CreativeColon.ChatterClub.Web.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Person> People { get; set; }

        public Room()
        {
            People = new HashSet<Person>();
        }
    }
}