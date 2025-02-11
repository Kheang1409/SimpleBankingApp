using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SimpleBankingApp.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }
        [Required]
        public string AccountType { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        public int CustomerId { get; set; }

        // Use nullable int for the foreign key field if it could be optional
        [ForeignKey("CustomerId")]
        [JsonIgnore]
        public Customer? Customer { get; set; }
    }

}
