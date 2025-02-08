
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleBankingApp.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        public string AccountType { get; set; } // Savings, Checking, etc.

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        // Foreign Key for Customer
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } // Navigation property
    }
}
