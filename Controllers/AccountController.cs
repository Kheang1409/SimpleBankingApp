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
            if (ModelState.IsValid)
            {
                await _accountService.AddAccountAsync(account);
                await _cacheService.RemoveDataAsync("accounts_list");

                return RedirectToAction(nameof(Index));
            }
            return View(account);
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

                if (model.Amount <= 0)
                {
                    ModelState.AddModelError("Amount", "Deposit amount must be greater than zero.");
                }

                if (ModelState.IsValid)
                {
                    account.Balance += model.Amount;
                    await _accountService.UpdateAccountAsync(account);

                    await _cacheService.RemoveDataAsync("accounts_list");
                    await _cacheService.SetDataAsync($"account_{account.AccountId}", account, TimeSpan.FromMinutes(5));

                    return RedirectToAction(nameof(Index));
                }
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