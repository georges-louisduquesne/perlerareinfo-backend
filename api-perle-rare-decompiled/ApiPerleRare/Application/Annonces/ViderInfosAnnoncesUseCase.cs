using System.Data;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Annonces;

public sealed class ViderInfosAnnoncesUseCase : IViderInfosAnnoncesUseCase
{
	private readonly IApplicationDbContext _context;

	public ViderInfosAnnoncesUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public void Execute(uint contactRef)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		string tableName = $"annonces_refcontact_{contactRef}";
		if (c.DoesTableExist(tableName))
		{
			using MySqlCommand cmd = c.CreateCommand();
			cmd.CommandText = "TRUNCATE TABLE " + tableName;
			cmd.ExecuteNonQuery();
		}
	}
}
