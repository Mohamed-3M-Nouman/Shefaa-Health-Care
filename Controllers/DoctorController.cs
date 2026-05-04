using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Data;
using ShefaaHealthCare.Models;

namespace ShefaaHealthCare.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════
        //  SEARCH / LIST DOCTORS (With Pagination)
        // ══════════════════════════════════════════
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchQuery, int? specializationId, int page = 1)
        {
            int pageSize = 9; // 9 doctors per page

            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Schedules)
                .AsQueryable();

            // Apply Search Filter
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(d =>
                    d.FullName.Contains(searchQuery) ||
                    (d.Specialization != null && d.Specialization.Name.Contains(searchQuery)) ||
                    (d.User != null && d.User.PhoneNumber != null && d.User.PhoneNumber.Contains(searchQuery))
                );
            }

            // Apply Specialization Filter
            if (specializationId.HasValue && specializationId > 0)
            {
                query = query.Where(d => d.SpecializationId == specializationId);
            }

            // Show Verified Doctors Only
            query = query.Where(d => d.IsVerified);

            // Calculate Pagination
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var doctors = await query
                .OrderByDescending(d => d.Rating) // Order by top rated first
                .ThenBy(d => d.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate ViewBags for UI binding
            ViewBag.SearchQuery = searchQuery;
            ViewBag.SelectedSpecialization = specializationId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Specializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();

            return View(doctors);
        }

        // ══════════════════════════════════════════
        //  DOCTOR PROFILE
        // ══════════════════════════════════════════
        [AllowAnonymous]
        public async Task<IActionResult> Profile(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsVerified);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // ══════════════════════════════════════════
        //  DOCTOR DASHBOARD
        // ══════════════════════════════════════════
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
