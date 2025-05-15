using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechBlogAPI.Data;

namespace TechBlogAPI.Services
{
    public class BruteForceDefenseService(DatabaseContext context, IHttpContextAccessor httpContextAccessor)
    {
        // IHttpContextAccessor used to retrieve information about the HTTP request. IP

        private readonly int _maxAttempts = 5;
        private readonly int _blockTime = 15;


        public async Task<int> GetDelayForUserAsync(string username)
        {
            var now = DateTime.UtcNow;
            var lockOutTime = now.AddMinutes(-_blockTime);

            var failedLoginAttempts = await context.LoginAttempts.Where(e => e.Username == username
                                                                             && !e.IsSuccessful &&
                                                                             e.AttemptTime >= lockOutTime).CountAsync();
            return failedLoginAttempts * 1000; // * 1000 because it's milliseconds
        }

        public async Task<bool> IsLockedOutAsync(string username)
        {
            var now = DateTime.UtcNow;
            var lockOutTime = now.AddMinutes(-_blockTime);

            var failedLoginAttempts = await context.LoginAttempts.Where(e => e.Username == username
                                                                        && !e.IsSuccessful &&
                                                                        e.AttemptTime >= lockOutTime).CountAsync();

            return failedLoginAttempts >= _maxAttempts;

        }

        public async Task RecordAttemptAsync(string username, bool isSuccessful)
        {
            var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            context.LoginAttempts.Add(new()
            {
                Username = username,
                IpAddress = ip,
                IsSuccessful = isSuccessful,
                AttemptTime = DateTime.UtcNow
            });

            if (isSuccessful)
            {
                var failedAttempts = await context.LoginAttempts.Where(e => e.Username == username && !e.IsSuccessful).ToListAsync();
                context.LoginAttempts.RemoveRange(failedAttempts);
            }
            await context.SaveChangesAsync();
        }
    }
}