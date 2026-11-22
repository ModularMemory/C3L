using GradebookAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI.Data
{
    public class GradebookDbContext : DbContext
    {
        public GradebookDbContext(DbContextOptions<GradebookDbContext> options)
            : base(options)
        {
        }

        public DbSet<Assessment> Assessments => Set<Assessment>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<CategoryWeight> CategoryWeights => Set<CategoryWeight>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Each student can have one grade per assessment
            modelBuilder.Entity<Grade>()
                .HasIndex(g => new { g.StudentId, g.AssessmentId })
                .IsUnique();

            // Each course can have one weight per category of assessment
            modelBuilder.Entity<CategoryWeight>()
                .HasIndex(w => new { w.CourseId, w.Category })
                .IsUnique();

            // Link grades to their assessments
            modelBuilder.Entity<Grade>()
                .HasOne<Assessment>()
                .WithMany()
                .HasForeignKey(g => g.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
