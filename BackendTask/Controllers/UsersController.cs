using BackendTask.DTOs;
using BackendTask.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendTask.Controllers
{
    [Route("users")]
    [ApiController]
    [Authorize(Roles = "MANAGER")]
    public class UsersController : ControllerBase
    {
        private readonly TicketManagementSystemContext _context;

        public UsersController(TicketManagementSystemContext context)
        {
            _context = context;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (model.Role != "MANAGER" && model.Role != "SUPPORT" && model.Role != "USER")
                    return BadRequest("Invalid role");

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == model.Role);

                if (role == null)
                    return BadRequest("Role not found");

                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email);

                if (emailExists)
                    return BadRequest("Email already exists");

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = hashedPassword,
                    RoleId = role.Id,
                    CreatedAt = DateTime.Now
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                return StatusCode(201, "User created successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        Role = u.Role.Name,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}