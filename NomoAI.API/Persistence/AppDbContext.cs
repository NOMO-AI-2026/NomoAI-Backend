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

        public DbSet<Doctor> Doctor { get; set; }

        public DbSet<ChildSpeechLevelHistory> ChildSpeechLevelHistories { get; set; }

        public DbSet<ChildProgressAlert> ChildProgressAlerts { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<AttemptEvaluation> AttemptEvaluations { get; set; }

        public DbSet<AttemptTranscribtion> AttemptTranscribtions { get; set; }

        public DbSet<SupportTicket> SupportTickets { get; set; }

        public DbSet<SupscriptionPlan> SupscriptionPlan { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SupportTicket>(entity =>
            {
                entity.Property(ticket => ticket.Subject)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(ticket => ticket.Message)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(ticket => ticket.AdminNote)
                    .HasMaxLength(1000);

                entity.Property(ticket => ticket.HandledByAdminUserId)
                    .HasMaxLength(450);

                entity.Property(ticket => ticket.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasIndex(ticket => ticket.Status);

                entity.HasOne(ticket => ticket.User)
                    .WithMany()
                    .HasForeignKey(ticket => ticket.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

            builder.Entity<Session>(entity =>
            {
                entity.Property(session => session.IsDoctorReviewed)
                    .HasDefaultValue(false);

                entity.Property(session => session.DoctorComment)
                    .HasMaxLength(1000);

                // Speeds up doctor-dashboard awaiting-review counts.
                entity.HasIndex(session => new
                {
                    session.Status,
                    session.IsDoctorReviewed,
                    session.IsDeleted
                });
            });

            builder.Entity<SessionAttempts>(entity =>
            {
                entity
                    .HasOne(attempt => attempt.Session)
                    .WithMany()
                    .HasForeignKey(attempt => attempt.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Filtered unique index (soft-deleted attempts do not block re-attempt numbering).
                entity
                    .HasIndex(attempt => new { attempt.SessionId, attempt.AttemptNumber })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<AttemptEvaluation>(entity =>
            {
                // One evaluation per attempt: AttemptId is the real FK (replaces the
                // legacy non-FK decimal AttemptId + shadow AttemptId1 columns).
                entity
                    .HasOne(evaluation => evaluation.Attempt)
                    .WithOne()
                    .HasForeignKey<AttemptEvaluation>(evaluation => evaluation.AttemptId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
