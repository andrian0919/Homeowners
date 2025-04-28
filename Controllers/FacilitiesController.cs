using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Authorization;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class FacilitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacilitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Facilities
        public async Task<IActionResult> Index()
        {
            var facilities = await _context.Facilities.ToListAsync();
            var viewModel = new FacilityListViewModel
            {
                Facilities = facilities
            };
            return View(viewModel);
        }

        // GET: Facilities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (facility == null)
            {
                return NotFound();
            }

            // Get upcoming reservations for this facility
            var upcomingReservations = await _context.FacilityReservations
                .Include(r => r.User)
                .Where(r => r.FacilityId == id && 
                    r.ReservationDate >= DateTime.Today &&
                    r.Status != ReservationStatus.Cancelled && 
                    r.Status != ReservationStatus.Rejected)
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            var viewModel = new FacilityDetailsViewModel
            {
                Facility = facility,
                UpcomingReservations = upcomingReservations
            };

            return View(viewModel);
        }

        // GET: Facilities/Create
        [Authorize(Roles = "Administrator,Staff")]
        public IActionResult Create()
        {
            return View(new CreateFacilityViewModel());
        }

        // POST: Facilities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Create([Bind("Name,Type,Description,MaxCapacity,HourlyRate,OpeningTime,ClosingTime")] CreateFacilityViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var facility = new Facility
                {
                    Name = viewModel.Name,
                    Type = viewModel.Type,
                    Description = viewModel.Description,
                    MaxCapacity = viewModel.MaxCapacity,
                    HourlyRate = viewModel.HourlyRate,
                    OpeningTime = viewModel.OpeningTime,
                    ClosingTime = viewModel.ClosingTime,
                    IsActive = true
                };
                
                _context.Add(facility);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Facilities/Edit/5
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
            {
                return NotFound();
            }
            
            var viewModel = new EditFacilityViewModel
            {
                Id = facility.Id,
                Name = facility.Name,
                Type = facility.Type,
                Description = facility.Description,
                MaxCapacity = facility.MaxCapacity,
                HourlyRate = facility.HourlyRate,
                OpeningTime = facility.OpeningTime,
                ClosingTime = facility.ClosingTime,
                IsActive = facility.IsActive
            };
            
            return View(viewModel);
        }

        // POST: Facilities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,Description,MaxCapacity,HourlyRate,OpeningTime,ClosingTime,IsActive")] EditFacilityViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var facility = await _context.Facilities.FindAsync(id);
                    if (facility == null)
                    {
                        return NotFound();
                    }
                    
                    facility.Name = viewModel.Name;
                    facility.Type = viewModel.Type;
                    facility.Description = viewModel.Description;
                    facility.MaxCapacity = viewModel.MaxCapacity;
                    facility.HourlyRate = viewModel.HourlyRate;
                    facility.OpeningTime = viewModel.OpeningTime;
                    facility.ClosingTime = viewModel.ClosingTime;
                    facility.IsActive = viewModel.IsActive;
                    
                    _context.Update(facility);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacilityExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Facilities/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities
                .FirstOrDefaultAsync(m => m.Id == id);
            if (facility == null)
            {
                return NotFound();
            }

            return View(facility);
        }

        // POST: Facilities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility != null)
            {
                // Check if there are any reservations for this facility
                var hasReservations = await _context.FacilityReservations
                    .AnyAsync(r => r.FacilityId == id);
                
                if (hasReservations)
                {
                    // If there are reservations, just mark it as inactive
                    facility.IsActive = false;
                    _context.Update(facility);
                }
                else
                {
                    // Otherwise, delete it
                    _context.Facilities.Remove(facility);
                }
                
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool FacilityExists(int id)
        {
            return _context.Facilities.Any(e => e.Id == id);
        }
    }
} 