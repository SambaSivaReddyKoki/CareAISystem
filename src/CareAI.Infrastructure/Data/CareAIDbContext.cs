using CareAI.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CareAI.Infrastructure.Data
{
    public class CareAIDbContext : DbContext
    {
        public CareAIDbContext(DbContextOptions<CareAIDbContext> options) 
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ConversationId);

            modelBuilder.Entity<ServiceRequest>()
                .HasIndex(sr => sr.UserId);
        }
    }
}
