using Microsoft.EntityFrameworkCore;
using LTUDAPI.Models; // Đảm bảo dòng này đúng

namespace LTUDAPI.Data // Namespace phải là LTUDAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<ReminderConfig> ReminderConfigs { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().ToTable("ACCOUNT").HasKey(a => a.IdAcc);
            modelBuilder.Entity<ReminderConfig>().ToTable("REMINDER_CONFIG").HasKey(r => r.IdConfig);
            modelBuilder.Entity<UserLog>().ToTable("USER_LOG").HasKey(l => l.IdLog);
            modelBuilder.Entity<User>().ToTable("USER").HasKey(u => u.IdAcc);
        }
    }
}