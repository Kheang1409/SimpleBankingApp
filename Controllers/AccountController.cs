using Microsoft.AspNetCore.Mvc;
using SimpleBankingApp.Models;
using SimpleBankingApp.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SimpleBankingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly CacheService _cacheService; // Using CacheService for Redis caching
        private readonly AccountService _accountService; // AccountService to manage account-related operations
        private readonly ILogger<AccountController> _logger;

        public AccountController(CacheService cacheService, AccountService accountService, ILogger<AccountController> logger)
        {
            _cacheService = cacheService;
            _accountService = accountService;
            _logger = logger;
        }

        // GET: Account
        public async Task<IActionResult> Index()
        {
            string cacheKey = "accounts_list";
            TimeSpan cacheExpiry = TimeSpan.FromMinutes(5);

            // Try to get data from Redis cache
            var cachedAccounts = await _cacheService.GetDataAsync<List<Account>>(cacheKey);
            if (cachedAccounts != null)
            {
                return View(cachedAccounts);
            }

            // If cache is empty, fetch data from DB
            var accounts = await _accountService.GetAllAccountsAsync();

            // Store data in Redis
            await _cacheService.SetDataAsync(cacheKey, accounts, cacheExpiry);

            return View(accounts);
        }

        // GET: Account/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // GET: Account/Create
        public IActionResult Create()
        {
            var customers = _accountService.GetCustomers();

            if (customers == null || !customers.Any())
            {
                _logger.LogWarning("No customers found to populate the dropdown.");
            }

            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "FullName");
            return View();
        }


        // POST: Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountId, AccountType, Balance, CustomerId")] Account account)
        {
            // Log the CustomerId to see if it is being passed correctly
            _logger.LogInformation("CustomerId: " + account.CustomerId);

            // Log the ModelState to see why it's invalid
            _logger.LogInformation("Model State Is Valid: " + ModelState.IsValid);
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogError("Error: {ErrorMessage}", error.ErrorMessage);
            }

            if (account.CustomerId == 0)
            {
                _logger.LogWarning("CustomerId was not selected.");
                ModelState.AddModelError("CustomerId", "Please select a customer.");
            }

            // Check for invalid model state
            if (!ModelState.IsValid)
            {
                // Re-populate the customer dropdown for the view
                var customers = _accountService.GetCustomers();
                ViewData["CustomerId"] = new SelectList(customers, "CustomerId", "FullName", account.CustomerId);

                // Return the model with validation errors back to the view
                return View(account);
            }

            // Proceed with adding the account if valid
            await _accountService.AddAccountAsync(account);

            // Clear the cache after adding a new account
            await _cacheService.RemoveDataAsync("accounts_list");

            return RedirectToAction(nameof(Index));
        }



        // GET: Account/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            ViewData["CustomerId"] = new SelectList(_accountService.GetCustomers(), "CustomerId", "FullName", account.CustomerId);
            return View(account);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AccountId,AccountType,Balance,CustomerId")] Account account)
        {
            if (id != account.AccountId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _accountService.UpdateAccountAsync(account);

                // Clear the cache after updating an account
                await _cacheService.RemoveDataAsync("accounts_list");

                return RedirectToAction(nameof(Index));
            }

            ViewData["CustomerId"] = new SelectList(_accountService.GetCustomers(), "CustomerId", "FullName", account.CustomerId);
            return View(account);
        }

        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _accountService.DeleteAccountAsync(id);

            // Clear the cache after deleting an account
            await _cacheService.RemoveDataAsync("accounts_list");

            return RedirectToAction(nameof(Index));
        }

        // GET: Account/Deposit/5
        [HttpGet]
        public async Task<IActionResult> Deposit(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            var model = new DepositViewModel
            {
                AccountId = account.AccountId,
                CurrentBalance = account.Balance
            };

            return View(model);
        }

        // POST: Account/Deposit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            if (ModelState.IsValid)
            {
                var account = await _accountService.GetAccountByIdAsync(model.AccountId);
                if (account == null)
                {
                    return NotFound();
                }

                account.Balance += model.Amount;
                await _accountService.UpdateAccountAsync(account);

                // Clear the cache after deposit
                await _cacheService.RemoveDataAsync("accounts_list");
                await _cacheService.SetDataAsync($"account_{account.AccountId}", account, TimeSpan.FromMinutes(5)); // Cache updated account

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Account/Withdraw/5
        [HttpGet]
        public async Task<IActionResult> Withdraw(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            var model = new WithdrawViewModel
            {
                AccountId = account.AccountId,
                CurrentBalance = account.Balance
            };

            return View(model);
        }

        // POST: Account/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawViewModel model)
        {
            if (ModelState.IsValid)
            {
                var account = await _accountService.GetAccountByIdAsync(model.AccountId);
                if (account == null)
                {
                    return NotFound();
                }

                if (model.Amount > account.Balance)
                {
                    ModelState.AddModelError("Amount", "Insufficient funds.");
                    return View(model);
                }

                account.Balance -= model.Amount;
                await _accountService.UpdateAccountAsync(account);

                // Clear the cache after withdrawal
                await _cacheService.RemoveDataAsync("accounts_list");
                await _cacheService.SetDataAsync($"account_{account.AccountId}", account, TimeSpan.FromMinutes(5)); // Cache updated account

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}