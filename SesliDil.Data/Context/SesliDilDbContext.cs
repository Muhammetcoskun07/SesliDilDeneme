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
        //model builderlar eksik eklenecektir.
    }
}
