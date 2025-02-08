using System.ComponentModel.DataAnnotations;

namespace SimpleBankingApp.Models
{
    public class WithdrawViewModel
    {
        public int AccountId { get; set; }
        public decimal CurrentBalance { get; set; }

        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Please enter a valid withdrawal amount")]
        public decimal Amount { get; set; }
    }
}
