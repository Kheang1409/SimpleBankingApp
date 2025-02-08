using System.ComponentModel.DataAnnotations;

namespace SimpleBankingApp.Models
{
    public class DepositViewModel
    {
        public int AccountId { get; set; }
        public decimal CurrentBalance { get; set; }

        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Please enter a valid deposit amount")]
        public decimal Amount { get; set; }
    }
}
