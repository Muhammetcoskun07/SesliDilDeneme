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
                entity.Property(e=>e.TargetLevel).IsRequired().HasMaxLength(50);
                entity.Property(e=>e.ProgressRate).IsRequired();
                entity.HasOne(e=>e.User).WithMany(u=>u.Progresses).HasForeignKey(e=>e.UserId);
            });
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Message");
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.Role).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e=>e.Conversation).WithMany(u=>u.Messages).HasForeignKey(e=>e.ThreadId);
            });
            modelBuilder.Entity<FileStorage>(entity =>
            {
                entity.ToTable("FileStorage");
                entity.HasKey(e => e.FileId);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FileURL).HasMaxLength(1000);
                entity.Property(e => e.UploadDate).IsRequired();
                entity.HasOne(e=>e.Conversation).WithMany(u=>u.Files).HasForeignKey(e=>e.ThreadId);
            });

        }
    }
}
