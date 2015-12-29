using System;

namespace CreativeColon.ChatterClub.Web.Models
{
    public class Conversation
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public DateTime HappenedAt { get; set; }

        public virtual int FromPersonId { get; set; }
        public virtual Person FromPerson { get; set; }

        public virtual int? ToPersonId { get; set; }
        public virtual Person ToPerson { get; set; }

        public virtual int? ToRoomId { get; set; }
        public virtual Room ToRoom { get; set; }
    }
}