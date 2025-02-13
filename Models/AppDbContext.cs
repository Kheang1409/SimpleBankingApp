using Microsoft.EntityFrameworkCore;

namespace SimpleBankingApp.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add CHECK constraint for Balance column to ensure it's greater than or equal to 0
            modelBuilder.Entity<Account>()
                .HasCheckConstraint("CK_Account_Balance", "Balance >= 0");

            // Add CHECK constraint for AccountType to ensure it is one of the valid types
            modelBuilder.Entity<Account>()
                .HasCheckConstraint("CK_Account_AccountType",
                    "AccountType IN ('Savings', 'Checking', 'Business')");
        }
    }
}
