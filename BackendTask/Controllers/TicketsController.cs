using System.Security.Claims;
using BackendTask.DTOs;
using BackendTask.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendTask.Controllers
{
    [Route("tickets")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly TicketManagementSystemContext _context;

        public TicketsController(TicketManagementSystemContext context)
        {
            _context = context;
        }
        
        [HttpPost]
        [Authorize(Roles = "USER,MANAGER")]
        public async Task<IActionResult> CreateTicket(CreateTicketDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (model.Priority != "LOW" && model.Priority != "MEDIUM" && model.Priority != "HIGH")
                    return BadRequest("Invalid priority");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var ticket = new Ticket
                {
                    Title = model.Title,
                    Description = model.Description,
                    Priority = model.Priority,
                    Status = "OPEN",
                    CreatedBy = Convert.ToInt32(userId),
                    CreatedAt = DateTime.Now
                };

                await _context.Tickets.AddAsync(ticket);
                await _context.SaveChangesAsync();

                return StatusCode(201, "Ticket created successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetTickets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userIdStr))
                    return Unauthorized();

                int userId = Convert.ToInt32(userIdStr);
                
                IQueryable<Ticket> query = _context.Tickets
                    .Include(t => t.CreatedByNavigation)
                    .Include(t => t.AssignedToNavigation);
                
                if (role == "MANAGER")
                {
                    query = query.Where(t => t.CreatedBy == userId);
                }
                else if (role == "SUPPORT")
                {
                    query = query.Where(t => t.AssignedTo == userId);
                }
                else
                {
                    query = query.Where(t => t.CreatedBy == userId);
                }
                
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var statusUpper = status.ToUpper();
                    var validStatuses = new List<string> { "OPEN", "IN_PROGRESS", "RESOLVED", "CLOSED" };

                    if (!validStatuses.Contains(statusUpper))
                        return BadRequest("Invalid status filter");

                    query = query.Where(t => t.Status == statusUpper);
                }
                
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    var priorityUpper = priority.ToUpper();
                    var validPriorities = new List<string> { "LOW", "MEDIUM", "HIGH" };

                    if (!validPriorities.Contains(priorityUpper))
                        return BadRequest("Invalid priority filter");

                    query = query.Where(t => t.Priority == priorityUpper);
                }
                
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));
                }
                
                var totalRecords = await query.CountAsync();
                var ticketsList = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        t.Priority,
                        CreatedBy = t.CreatedByNavigation.Name,
                        AssignedTo = t.AssignedToNavigation != null ? t.AssignedToNavigation.Name : null,
                        t.CreatedAt
                    })
                    .ToListAsync();
                
                return Ok(new
                {
                    page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    tickets = ticketsList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "MANAGER,SUPPORT")]
        public async Task<IActionResult> AssignTicket(int id, AssignTicketDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(role))
                    return Unauthorized();

                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound("Ticket not found");

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == model.UserId);

                if (user == null)
                    return NotFound("User not found");

                if (user.Role.Name == "USER")
                    return BadRequest("Cannot assign ticket to USER role");

                ticket.AssignedTo = user.Id;

                await _context.SaveChangesAsync();

                return Ok("Ticket assigned successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SUPPORT")]
        public async Task<IActionResult> UpdateTicketStatus(int id, UpdateStatusDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (model.Status != "OPEN" && 
                    model.Status != "IN_PROGRESS" && 
                    model.Status != "CLOSED")
                    return BadRequest("Invalid status");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var supportId = Convert.ToInt32(userId);

                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound("Ticket not found");

                if (ticket.AssignedTo != supportId)
                    return BadRequest("You can only update tickets assigned to you");

                ticket.Status = model.Status;

                await _context.SaveChangesAsync();

                return Ok("Ticket status updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.TicketComments)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound("Ticket not found");

                if (ticket.TicketComments.Any())
                    return BadRequest("Cannot delete ticket with comments");

                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();

                return Ok("Ticket deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}