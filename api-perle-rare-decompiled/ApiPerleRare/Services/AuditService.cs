using System;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Services;

public class AuditService : IAuditService
{
	public void Add(ApplicationDbContext dbContext, string login, string table, string type, string value, string where, string remoteIpAddress)
	{
		int res = dbContext.Database.ExecuteSqlRaw("INSERT INTO audit VALUES (DEFAULT, NOW(), @p0, @p1, @p2, @p3, @p4, @p5)", login, table, type, value, where, remoteIpAddress);
		if (res != 1)
		{
			throw new Exception("Devrait avoir fait une modif!");
		}
	}
}
