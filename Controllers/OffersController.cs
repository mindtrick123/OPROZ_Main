using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    [Route("Admin/[controller]")]
    public class OffersController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OffersController> _logger;

        public OffersController(ApplicationDbContext context, ILogger<OffersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Offers
        public async Task<IActionResult> Index()
        {
            var offers = await _context.Offers
                .Include(o => o.Service)
                .OrderByDescending(o => o.IsActive)
                .ThenByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(offers);
        }

        // GET: Admin/Offers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offer = await _context.Offers
                .Include(o => o.Service)
                .Include(o => o.PaymentHistories)
                .ThenInclude(ph => ph.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offer == null)
            {
                return NotFound();
            }

            return View(offer);
        }

        // GET: Admin/Offers/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new OfferFormViewModel
            {
                AvailableServices = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Admin/Offers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfferFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check if code already exists
                if (await _context.Offers.AnyAsync(o => o.Code.ToLower() == viewModel.Code.ToLower()))
                {
                    ModelState.AddModelError(nameof(viewModel.Code), "An offer with this code already exists.");
                }
                else
                {
                    var offer = new Offer
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        Code = viewModel.Code.ToUpper(),
                        Type = viewModel.Type,
                        Value = viewModel.Value,
                        ServiceId = viewModel.ServiceId,
                        StartDate = viewModel.StartDate,
                        EndDate = viewModel.EndDate,
                        MaxUsageCount = viewModel.MaxUsageCount,
                        MinOrderAmount = viewModel.MinOrderAmount,
                        IsActive = viewModel.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Offers.Add(offer);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Offer '{offer.Name}' has been created successfully.");
                    return RedirectToAction(nameof(Index));
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

        // GET: Admin/Offers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            var viewModel = new OfferFormViewModel
            {
                Id = offer.Id,
                Name = offer.Name,
                Description = offer.Description,
                Code = offer.Code,
                Type = offer.Type,
                Value = offer.Value,
                ServiceId = offer.ServiceId,
                StartDate = offer.StartDate,
                EndDate = offer.EndDate,
                MaxUsageCount = offer.MaxUsageCount,
                MinOrderAmount = offer.MinOrderAmount,
                IsActive = offer.IsActive,
                AvailableServices = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Admin/Offers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OfferFormViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var offer = await _context.Offers.FindAsync(id);
                    if (offer == null)
                    {
                        return NotFound();
                    }

                    // Check if code already exists (excluding current offer)
                    if (await _context.Offers.AnyAsync(o => o.Code.ToLower() == viewModel.Code.ToLower() && o.Id != id))
                    {
                        ModelState.AddModelError(nameof(viewModel.Code), "An offer with this code already exists.");
                    }
                    else
                    {
                        offer.Name = viewModel.Name;
                        offer.Description = viewModel.Description;
                        offer.Code = viewModel.Code.ToUpper();
                        offer.Type = viewModel.Type;
                        offer.Value = viewModel.Value;
                        offer.ServiceId = viewModel.ServiceId;
                        offer.StartDate = viewModel.StartDate;
                        offer.EndDate = viewModel.EndDate;
                        offer.MaxUsageCount = viewModel.MaxUsageCount;
                        offer.MinOrderAmount = viewModel.MinOrderAmount;
                        offer.IsActive = viewModel.IsActive;
                        offer.UpdatedAt = DateTime.UtcNow;

                        _context.Update(offer);
                        await _context.SaveChangesAsync();

                        SetSuccessMessage($"Offer '{offer.Name}' has been updated successfully.");
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OfferExists(viewModel.Id))
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

        // GET: Admin/Offers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offer = await _context.Offers
                .Include(o => o.Service)
                .Include(o => o.PaymentHistories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (offer == null)
            {
                return NotFound();
            }

            return View(offer);
        }

        // POST: Admin/Offers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offer = await _context.Offers
                .Include(o => o.PaymentHistories)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer != null)
            {
                // Check if offer is being used in any payments
                if (offer.PaymentHistories.Any())
                {
                    SetErrorMessage($"Cannot delete offer '{offer.Name}' because it has been used in payments. Please deactivate it instead.");
                    return RedirectToAction(nameof(Index));
                }

                _context.Offers.Remove(offer);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Offer '{offer.Name}' has been deleted successfully.");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OfferExists(int id)
        {
            return _context.Offers.Any(e => e.Id == id);
        }
    }
}