namespace LabSync.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            int userId,
            string action,
            string? entityName = null,
            int entityId = 0,
            string? details = null);
    }
}