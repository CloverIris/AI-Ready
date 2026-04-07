using AIReady.Service.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIReady.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly AIReadyDbContext _context;

    public WorkflowsController(AIReadyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.WorkflowTemplates.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(w => w.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(w => 
                (w.Name != null && w.Name.Contains(search)) ||
                (w.Description != null && w.Description.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.Popularity)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var workflow = await _context.WorkflowTemplates.FindAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }
        return Ok(workflow);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.WorkflowTemplates
            .Where(w => w.Category != null)
            .Select(w => w.Category)
            .Distinct()
            .ToListAsync();

        return Ok(categories);
    }
}
