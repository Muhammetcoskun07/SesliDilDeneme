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
        public SesliDilDbContext(DbContextOptions<SesliDilDbContext> options) : base(options)
        {
            // Dependency Injection için constructor
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<AIAgent> AIAgents { get; set; }
        public DbSet<Session> Sessions { get; set; } // ✅ Session eklendi

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).IsRequired().ValueGeneratedOnAdd();
                entity.Property(e => e.SocialProvider).HasMaxLength(10).IsRequired();
                entity.Property(e => e.SocialId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NativeLanguage).HasMaxLength(10); // Zorunlu
                entity.Property(e => e.TargetLanguage).HasMaxLength(10);
                entity.Property(e => e.ProficiencyLevel).HasMaxLength(2);
                entity.Property(e => e.AgeRange).HasMaxLength(5);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastLoginAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LearningGoals).HasColumnType("jsonb"); // PostgreSQL için
                entity.Property(e => e.Hobbies).HasColumnType("jsonb"); // PostgreSQL için
                entity.Property(e => e.HasCompletedOnboarding).IsRequired().HasDefaultValue(false);
                entity.HasIndex(e => new { e.SocialProvider, e.SocialId }).IsUnique();
                entity.HasMany(e => e.Progresses).WithOne(p => p.User).HasForeignKey(p => p.UserId);
            });

            modelBuilder.Entity<Progress>(entity =>
            {
                entity.ToTable("Progress");
                entity.HasKey(e => e.ProgressId);
                entity.Property(e => e.ProgressId).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.DailyConversationCount).IsRequired();
                entity.Property(e => e.TotalConversationTimeMinutes).IsRequired();
                entity.Property(e => e.CurrentStreakDays).IsRequired();
                entity.Property(e => e.LongestStreakDays).IsRequired();
                entity.Property(e => e.CurrentLevel).IsRequired().HasMaxLength(2);
                entity.Property(e => e.LastConversationDate).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasOne(e => e.User).WithMany(u => u.Progresses).HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Message");
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.MessageId).ValueGeneratedOnAdd();
                entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.AudioUrl).HasMaxLength(1000);
                entity.Property(e => e.SpeakerType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.Conversation).WithMany(c => c.Messages).HasForeignKey(e => e.ConversationId);
            });

            modelBuilder.Entity<FileStorage>(entity =>
            {
                entity.ToTable("FileStorage");
                entity.HasKey(e => e.FileId);
                entity.Property(e => e.FileId).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileURL).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.UploadDate).IsRequired();
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Conversation).WithMany(c => c.Files).HasForeignKey(e => e.ConversationId);
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversation");
                entity.HasKey(e => e.ConversationId);
                entity.Property(e => e.ConversationId).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.AgentId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Message).HasMaxLength(4000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
                entity.Property(e => e.StartedAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();
                entity.HasOne(e => e.Agent).WithMany().HasForeignKey(e => e.AgentId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<AIAgent>(entity =>
            {
                entity.ToTable("AIAgent");
                entity.HasKey(e => e.AgentId);
                entity.Property(e => e.AgentId).ValueGeneratedOnAdd();
                entity.Property(e => e.AgentName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AgentPrompt).HasMaxLength(1000);
                entity.Property(e => e.AgentDescription).HasMaxLength(1000);
                entity.Property(e => e.AgentType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("Session");
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.SessionId).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId);
            });
        }
    }
}
