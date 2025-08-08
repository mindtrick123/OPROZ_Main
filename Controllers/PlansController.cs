using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    [Route("Admin/Plans")]
    public class PlansController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlansController> _logger;

        public PlansController(ApplicationDbContext context, ILogger<PlansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Plans
        public async Task<IActionResult> Index()
        {
            var plans = await _context.SubscriptionPlans
                .Include(p => p.PlanServices)
                .ThenInclude(ps => ps.Service)
                .OrderBy(p => p.Type)
                .ThenBy(p => p.Duration)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return View(plans);
        }

        // GET: Admin/Plans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.SubscriptionPlans
                .Include(p => p.PlanServices)
                .ThenInclude(ps => ps.Service)
                .Include(p => p.PaymentHistories)
                .ThenInclude(ph => ph.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // GET: Admin/Plans/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new SubscriptionPlanFormViewModel
            {
                AvailableServices = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Admin/Plans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubscriptionPlanFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var plan = new SubscriptionPlan
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    Duration = viewModel.Duration,
                    Type = viewModel.Type,
                    Features = viewModel.Features,
                    MaxUsers = viewModel.MaxUsers,
                    MaxStorage = viewModel.MaxStorage,
                    IsActive = viewModel.IsActive,
                    IsPopular = viewModel.IsPopular,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubscriptionPlans.Add(plan);
                await _context.SaveChangesAsync();

                // Add selected services to the plan
                if (viewModel.SelectedServiceIds?.Any() == true)
                {
                    var planServices = viewModel.SelectedServiceIds.Select(serviceId => new PlanService
                    {
                        SubscriptionPlanId = plan.Id,
                        ServiceId = serviceId,
                        CreatedAt = DateTime.UtcNow
                    });

                    _context.PlanServices.AddRange(planServices);
                    await _context.SaveChangesAsync();
                }

                SetSuccessMessage($"Subscription plan '{plan.Name}' has been created successfully.");
                return RedirectToAction(nameof(Index));
            }

            // Reload services if validation fails
            viewModel.AvailableServices = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Plans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.SubscriptionPlans
                .Include(p => p.PlanServices)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            var viewModel = new SubscriptionPlanFormViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                Duration = plan.Duration,
                Type = plan.Type,
                Features = plan.Features,
                MaxUsers = plan.MaxUsers,
                MaxStorage = plan.MaxStorage,
                IsActive = plan.IsActive,
                IsPopular = plan.IsPopular,
                SelectedServiceIds = plan.PlanServices.Select(ps => ps.ServiceId).ToList(),
                AvailableServices = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Admin/Plans/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubscriptionPlanFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var plan = await _context.SubscriptionPlans
                        .Include(p => p.PlanServices)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (plan == null)
                    {
                        return NotFound();
                    }

                    plan.Name = viewModel.Name;
                    plan.Description = viewModel.Description;
                    plan.Price = viewModel.Price;
                    plan.Duration = viewModel.Duration;
                    plan.Type = viewModel.Type;
                    plan.Features = viewModel.Features;
                    plan.MaxUsers = viewModel.MaxUsers;
                    plan.MaxStorage = viewModel.MaxStorage;
                    plan.IsActive = viewModel.IsActive;
                    plan.IsPopular = viewModel.IsPopular;
                    plan.UpdatedAt = DateTime.UtcNow;

                    // Update plan services
                    _context.PlanServices.RemoveRange(plan.PlanServices);

                    if (viewModel.SelectedServiceIds?.Any() == true)
                    {
                        var planServices = viewModel.SelectedServiceIds.Select(serviceId => new PlanService
                        {
                            SubscriptionPlanId = plan.Id,
                            ServiceId = serviceId,
                            CreatedAt = DateTime.UtcNow
                        });

                        _context.PlanServices.AddRange(planServices);
                    }

                    _context.Update(plan);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Subscription plan '{plan.Name}' has been updated successfully.");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlanExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Reload services if validation fails
            viewModel.AvailableServices = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Plans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var plan = await _context.SubscriptionPlans
                .Include(p => p.PlanServices)
                .ThenInclude(ps => ps.Service)
                .Include(p => p.PaymentHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: Admin/Plans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var plan = await _context.SubscriptionPlans
                .Include(p => p.PaymentHistories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan != null)
            {
                // Check if plan is being used in any payments
                if (plan.PaymentHistories.Any())
                {
                    SetErrorMessage($"Cannot delete subscription plan '{plan.Name}' because it has payment history. Please deactivate it instead.");
                    return RedirectToAction(nameof(Index));
                }

                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Subscription plan '{plan.Name}' has been deleted successfully.");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PlanExists(int id)
        {
            return _context.SubscriptionPlans.Any(e => e.Id == id);
        }
    }
}