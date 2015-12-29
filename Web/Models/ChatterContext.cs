using System.Configuration;
using System.Data.Entity;
using System.IO;

namespace CreativeColon.ChatterClub.Web.Models
{
    public class ClubContext : DbContext
    {
        public ClubContext()
            : base("ClubContext")
        {
            var LogFilePath = ConfigurationManager.AppSettings["EFSQLLog"];

            if (!string.IsNullOrWhiteSpace(LogFilePath))
            {
                File.WriteAllText(LogFilePath, string.Empty);
                Database.Log = (line) => File.AppendAllText(LogFilePath, line);
            }

            Database.SetInitializer(new DropCreateDatabaseAlways<ClubContext>());
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
    }
}
