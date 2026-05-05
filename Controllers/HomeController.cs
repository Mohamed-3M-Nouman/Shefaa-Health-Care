using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Data;
using ShefaaHealthCare.Models;

namespace ShefaaHealthCare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var specializations = await _context.Specializations
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        var doctorNames = await _context.Doctors
            .AsNoTracking()
            .Select(d => d.FullName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        ViewBag.PopularSpecializations = specializations;
        ViewBag.SearchSuggestions = specializations
            .Select(s => s.Name)
            .Concat(doctorNames)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Services()
    {
        return View();
    }

    public IActionResult PopularQuestions()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    // Legal / policy pages
    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Hipaa()
    {
        return View();
    }

    public IActionResult Cookies()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
