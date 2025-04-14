// Repositories/IEmailUserRepository.cs
using MaiApi.Models;
using Microsoft.EntityFrameworkCore;
using MaiApi.Data;

namespace MaiApi.Repositories
{
    public interface IEmailUserRepository
    {
        Task<EmailUser> CreateAsync(EmailUser user);
        Task<EmailUser?> GetByIdAsync(string id);
        Task<EmailUser?> GetByApiKeyAsync(string apiKey);
        Task<EmailUser?> GetByUsernameAsync(string username);
        Task<EmailUser?> GetByEmailAsync(string email);
        Task<bool> UpdateAsync(EmailUser user);
        Task<bool> DeleteAsync(string id);
        Task<List<EmailUser>> GetAllAsync();
        Task<bool> SaveChangesAsync();
    }

    public class EmailUserRepository : IEmailUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailUserRepository> _logger;

        public EmailUserRepository(ApplicationDbContext context, ILogger<EmailUserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailUser> CreateAsync(EmailUser user)
        {
            await _context.EmailUsers.AddAsync(user);
            await SaveChangesAsync();
            return user;
        }

        public async Task<EmailUser?> GetByIdAsync(string id)
        {
            return await _context.EmailUsers.FindAsync(id);
        }

        public async Task<EmailUser?> GetByApiKeyAsync(string apiKey)
        {
            return await _context.EmailUsers.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        }

        public async Task<EmailUser?> GetByUsernameAsync(string username)
        {
            return await _context.EmailUsers.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<EmailUser?> GetByEmailAsync(string email)
        {
            return await _context.EmailUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateAsync(EmailUser user)
        {
            _context.EmailUsers.Update(user);
            return await SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user == null)
                return false;

            _context.EmailUsers.Remove(user);
            return await SaveChangesAsync();
        }

        public async Task<List<EmailUser>> GetAllAsync()
        {
            return await _context.EmailUsers.ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                return (await _context.SaveChangesAsync()) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                return false;
            }
        }
    }
}
