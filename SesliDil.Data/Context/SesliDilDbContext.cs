using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SesliDil.Core.Entities;


namespace SesliDil.Data.Context
{
    public class SesliDilDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<AIAgent> AIAgents { get; set; }
        public SesliDilDbContext(DbContextOptions<SesliDilDbContext> options) : base(options) 
        {
            //Dependency Injections yapabilelim diye yazıldı
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.UserId);
                entity.Property(e=>e.Language).IsRequired().HasMaxLength(50);
                entity.Property(e=>e.Interests).IsRequired().HasMaxLength(500);
                entity.Property(e=>e.Gender).HasMaxLength(50);
                entity.Property(e => e.Age).IsRequired();
                entity.Property(e => e.Streak);
                entity.Property(e => e.RegistrationDate).IsRequired();

            });
            modelBuilder.Entity<Progress>(entity =>
            {
                entity.ToTable("Progress");
                entity.HasKey(e => e.ProgressId);
                entity.Property(e => e.CurrentLevel).IsRequired().HasMaxLength(50);
                entity.Property(e=>e.ProgressRate).IsRequired();
            });
        }
    }
}
