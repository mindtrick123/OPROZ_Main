using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    [Route("Admin/[controller]")]
    public class ServicesController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(ApplicationDbContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(services);
        }

        // GET: Admin/Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.PlanServices)
                .ThenInclude(ps => ps.SubscriptionPlan)
                .Include(s => s.Offers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // GET: Admin/Services/Create
        public IActionResult Create()
        {
            var viewModel = new ServiceFormViewModel();
            return View(viewModel);
        }

        // POST: Admin/Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var service = new Service
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    ShortDescription = viewModel.ShortDescription,
                    IconClass = viewModel.IconClass,
                    ImageUrl = viewModel.ImageUrl,
                    BasePrice = viewModel.BasePrice,
                    IsActive = viewModel.IsActive,
                    IsFeatured = viewModel.IsFeatured,
                    DisplayOrder = viewModel.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Service '{service.Name}' has been created successfully.");
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Admin/Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var viewModel = new ServiceFormViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                ShortDescription = service.ShortDescription,
                IconClass = service.IconClass,
                ImageUrl = service.ImageUrl,
                BasePrice = service.BasePrice,
                IsActive = service.IsActive,
                IsFeatured = service.IsFeatured,
                DisplayOrder = service.DisplayOrder
            };

            return View(viewModel);
        }

        // POST: Admin/Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var service = await _context.Services.FindAsync(id);
                    if (service == null)
                    {
                        return NotFound();
                    }

                    service.Name = viewModel.Name;
                    service.Description = viewModel.Description;
                    service.ShortDescription = viewModel.ShortDescription;
                    service.IconClass = viewModel.IconClass;
                    service.ImageUrl = viewModel.ImageUrl;
                    service.BasePrice = viewModel.BasePrice;
                    service.IsActive = viewModel.IsActive;
                    service.IsFeatured = viewModel.IsFeatured;
                    service.DisplayOrder = viewModel.DisplayOrder;
                    service.UpdatedAt = DateTime.UtcNow;

                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Service '{service.Name}' has been updated successfully.");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(viewModel);
        }

        // GET: Admin/Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.PlanServices)
                .ThenInclude(ps => ps.SubscriptionPlan)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: Admin/Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services
                .Include(s => s.PlanServices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service != null)
            {
                // Check if service is being used in any plans
                if (service.PlanServices.Any())
                {
                    SetErrorMessage($"Cannot delete service '{service.Name}' because it's being used in subscription plans. Please remove it from all plans first.");
                    return RedirectToAction(nameof(Index));
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Service '{service.Name}' has been deleted successfully.");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}