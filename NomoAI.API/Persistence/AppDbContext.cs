using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {

        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Parent> Parents { get; set; }

        public DbSet<Children> Children { get; set; }

        public DbSet<DoctorNotes> DoctorNotes { get; set; }

        public DbSet<Session> Sessions { get; set; }

        public DbSet<SessionAttempts> SessionAttempts { get; set; }

        public DbSet<SessionSummary> SessionSummaries { get; set; }

        public DbSet<SpeechLevel> SpeechLevels { get; set; }

        public DbSet<ChildSpeechLevelHistory> ChildSpeechLevelHistories { get; set; }

        public DbSet<ChildProgressAlert> ChildProgressAlerts { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<AttemptEvaluation> AttemptEvaluations { get; set; }

        public DbSet<AttemptTranscribtion> AttemptTranscribtions { get; set; }


    }
}
