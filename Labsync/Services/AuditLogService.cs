using Labsync.Models.Data;
using LabSync.Models;

namespace LabSync.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(
            ApplicationDbContext context)
        {
            _context = context;
        }
        // Audit Log Create
        public async Task LogAsync(
            int userId,
            string action,
            string? entityName = null,
            int entityId = 0,
            string? details = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
        }
    }
}
