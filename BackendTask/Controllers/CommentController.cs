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
    public class CommentsController : ControllerBase
    {
        private readonly TicketManagementSystemContext _context;

        public CommentsController(TicketManagementSystemContext context)
        {
            _context = context;
        }
        
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, CommentDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound("Ticket not found");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Unauthorized();

                int uid = Convert.ToInt32(userId);
                
                if (role == "SUPPORT" && ticket.AssignedTo != uid)
                    return Forbid("You can comment only on assigned tickets");
                if (role == "USER" && ticket.CreatedBy != uid)
                    return Forbid("You can comment only on your own tickets");

                var comment = new TicketComment
                {
                    TicketId = id,
                    UserId = uid,
                    Comment = model.Comment,
                    CreatedAt = DateTime.Now
                };

                await _context.TicketComments.AddAsync(comment);
                await _context.SaveChangesAsync();

                return StatusCode(201, "Comment added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            try
            {
                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound("Ticket not found");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Unauthorized();

                int uid = Convert.ToInt32(userId);
                
                if (role == "SUPPORT" && ticket.AssignedTo != uid)
                    return Forbid("You can view comments only for assigned tickets");
                if (role == "USER" && ticket.CreatedBy != uid)
                    return Forbid("You can view comments only for your own tickets");

                var comments = await _context.TicketComments
                    .Where(c => c.TicketId == id)
                    .Include(c => c.User)
                    .Select(c => new
                    {
                        c.Id,
                        c.Comment,
                        User = c.User.Name,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpPatch("/comments/{id}")]
        public async Task<IActionResult> EditComment(int id, CommentDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var comment = await _context.TicketComments
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                    return NotFound("Comment not found");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Unauthorized();

                int uid = Convert.ToInt32(userId);

                if (role != "MANAGER" && comment.UserId != uid)
                    return Forbid("You can edit only your own comments");

                comment.Comment = model.Comment;

                await _context.SaveChangesAsync();

                return Ok("Comment updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpDelete("/comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var comment = await _context.TicketComments
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                    return NotFound("Comment not found");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Unauthorized();

                int uid = Convert.ToInt32(userId);

                if (role != "MANAGER" && comment.UserId != uid)
                    return Forbid("You can delete only your own comments");

                _context.TicketComments.Remove(comment);
                await _context.SaveChangesAsync();

                return Ok("Comment deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}