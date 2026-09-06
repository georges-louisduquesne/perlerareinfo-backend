using ApiPerleRare.Models;

namespace ApiPerleRare;

public interface IAuditService
{
	void Add(ApplicationDbContext dbContext, string login, string table, string type, string value, string where, string remoteIpAddress);
}
