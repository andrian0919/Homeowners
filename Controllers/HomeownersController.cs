using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeownersSubdivision.Controllers
{
    public class HomeownersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeownersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Homeowners
        public async Task<IActionResult> Index()
        {
            return View(await _context.Homeowners.ToListAsync());
        }

        // GET: Homeowners/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var homeowner = await _context.Homeowners
                .FirstOrDefaultAsync(m => m.Id == id);
            if (homeowner == null)
            {
                return NotFound();
            }

            return View(homeowner);
        }

        // GET: Homeowners/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Homeowners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Address,Email,Phone,JoinDate")] Homeowner homeowner)
        {
            if (ModelState.IsValid)
            {
                _context.Add(homeowner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(homeowner);
        }

        // GET: Homeowners/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var homeowner = await _context.Homeowners.FindAsync(id);
            if (homeowner == null)
            {
                return NotFound();
            }
            return View(homeowner);
        }

        // POST: Homeowners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Address,Email,Phone,JoinDate")] Homeowner homeowner)
        {
            if (id != homeowner.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(homeowner);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HomeownerExists(homeowner.Id))
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
            return View(homeowner);
        }

        // GET: Homeowners/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var homeowner = await _context.Homeowners
                .FirstOrDefaultAsync(m => m.Id == id);
            if (homeowner == null)
            {
                return NotFound();
            }

            return View(homeowner);
        }

        // POST: Homeowners/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var homeowner = await _context.Homeowners.FindAsync(id);
            if (homeowner != null)
            {
                _context.Homeowners.Remove(homeowner);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HomeownerExists(int id)
        {
            return _context.Homeowners.Any(e => e.Id == id);
        }
    }
} 