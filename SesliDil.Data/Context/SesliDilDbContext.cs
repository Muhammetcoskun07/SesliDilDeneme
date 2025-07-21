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
                entity.Property(e => e.UserId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.SocialProvider).IsRequired().HasMaxLength(10); // google, apple
                entity.Property(e => e.SocialId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NativeLanguage).IsRequired().HasMaxLength(10);
                entity.Property(e => e.TargetLanguage).IsRequired().HasMaxLength(10); 
                entity.Property(e => e.ProficiencyLevel).IsRequired().HasMaxLength(2); // A1, A2, vb.
                entity.Property(e => e.AgeRange).IsRequired().HasMaxLength(5); // 13-17, vb.
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastLoginAt).IsRequired();

            });
            modelBuilder.Entity<Progress>(entity =>
            {
                entity.ToTable("Progress");
                entity.HasKey(e => e.ProgressId);
                entity.Property(e => e.ProgressId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36); // UUID
                entity.Property(e => e.DailyConversationCount).IsRequired();
                entity.Property(e => e.TotalConversationTimeMinutes).IsRequired();
                entity.Property(e => e.CurrentStreakDays).IsRequired();
                entity.Property(e => e.LongestStreakDays).IsRequired();
                entity.Property(e => e.CurrentLevel).IsRequired().HasMaxLength(2); // A1, A2, vb.
                entity.Property(e => e.LastConversationDate).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasOne(e => e.User).WithMany(u => u.Progresses).HasForeignKey(e => e.UserId);
            });
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Message");
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.MessageId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(36); // UUID
                entity.Property(e => e.Role).IsRequired().HasMaxLength(10); // user, ai
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.AudioUrl).HasMaxLength(1000);
                entity.Property(e => e.SpeakerType).IsRequired().HasMaxLength(10); // user, ai
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.Conversation).WithMany(c => c.Messages).HasForeignKey(e => e.ConversationId);
            });
            modelBuilder.Entity<FileStorage>(entity =>
            {
                entity.ToTable("FileStorage");
                entity.HasKey(e => e.FileId);
                entity.Property(e => e.FileId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36); // UUID
                entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(36); // UUID
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
                entity.Property(e => e.ConversationId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36); // UUID
                entity.Property(e => e.AgentId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Message).HasMaxLength(4000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20); // active, completed, vb.
                entity.Property(e => e.Language).IsRequired().HasMaxLength(10); // ISO 639-1
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
                entity.Property(e => e.AgentId).ValueGeneratedOnAdd(); // UUID için otomatik generation
                entity.Property(e => e.AgentName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AgentPrompt).HasMaxLength(1000);
                entity.Property(e => e.AgentDescription).HasMaxLength(1000);
                entity.Property(e => e.AgentType).IsRequired().HasMaxLength(20); // conversation, travel, vb.
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });
        }
    }
}
