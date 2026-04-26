using Microsoft.EntityFrameworkCore;

namespace ConnectHub.Web.Data
{
    public class WebsiteDbContext : DbContext
    {
        public WebsiteDbContext(DbContextOptions<WebsiteDbContext> options) : base(options) { }
        // DbSets for audit logs, analytics, etc. can be added here
    }
}
