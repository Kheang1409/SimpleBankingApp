using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SimpleBankingApp.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }
        [JsonIgnore]
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}
