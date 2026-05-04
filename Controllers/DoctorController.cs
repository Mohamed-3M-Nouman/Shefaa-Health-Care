using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Data;
using ShefaaHealthCare.Models;
using ShefaaHealthCare.Repositories.Interfaces;

namespace ShefaaHealthCare.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public DoctorController(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        // ══════════════════════════════════════════
        //  SEARCH / LIST DOCTORS
        // ══════════════════════════════════════════

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search = null, int? specializationId = null)
        {
            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Schedules)
                .AsQueryable();

            // Search by doctor name or specialization
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.FullName.Contains(search) ||
                    d.Specialization != null && d.Specialization.Name.Contains(search) ||
                    d.User != null && d.User.PhoneNumber != null && d.User.PhoneNumber.Contains(search)
                );
            }

            // Filter by specialization
            if (specializationId.HasValue && specializationId > 0)
            {
                query = query.Where(d => d.SpecializationId == specializationId);
            }

            // Only show verified doctors
            query = query.Where(d => d.IsVerified);

            var doctors = await query
                .OrderBy(d => d.FullName)
                .ToListAsync();

            // Get all specializations for filter dropdown
            var specializations = await _unitOfWork.Repository<Specialization>()
                .GetAllAsync();

            ViewBag.Search = search;
            ViewBag.SelectedSpecialization = specializationId;
            ViewBag.Specializations = specializations;

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
