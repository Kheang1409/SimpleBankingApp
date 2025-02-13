using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using SimpleBankingApp.Models;
using SimpleBankingApp.Services;
using StackExchange.Redis;

namespace SimpleBankingApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CacheService _cacheService;
        private readonly CustomerService _customerService;
        private readonly IConnectionMultiplexer _redis;

        public CustomerController(CacheService cacheService, CustomerService customerService, IConnectionMultiplexer redis)
        {
            _cacheService = cacheService;
            _customerService = customerService;
            _redis = redis;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            string cacheKey = "customers_list";
            TimeSpan cacheExpiry = TimeSpan.FromMinutes(5);

            // Try to get data from Redis cache
            var cachedCustomers = await _cacheService.GetDataAsync<List<Customer>>(cacheKey);
            if (cachedCustomers != null)
            {
                return View(cachedCustomers);
            }

            // If cache is empty, fetch data from DB
            var customers = await _customerService.GetAllCustomersAsync();

            // Store data in Redis
            await _cacheService.SetDataAsync(cacheKey, customers, cacheExpiry);

            return View(customers);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetCustomersByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,FullName,Email,PhoneNumber")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.AddCustomerAsync(customer);
                var sub = _redis.GetSubscriber();
                await sub.PublishAsync("cache-invalidation", "customers_list");
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetCustomersByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FullName,Email,PhoneNumber")] Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _cacheService.RemoveDataAsync("customers_list");
                await _customerService.UpdateCustomerAsync(customer);
                var sub = _redis.GetSubscriber();
                await sub.PublishAsync("cache-invalidation", "customers_list");
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customerService.GetCustomersByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _cacheService.RemoveDataAsync("customers_list");
            await _customerService.DeleteCustomerAsync(id);
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync("cache-invalidation", "customers_list");
            return RedirectToAction(nameof(Index));
        }
    }
}