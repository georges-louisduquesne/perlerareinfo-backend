using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ApiPerleRare.Application.Events;
using ApiPerleRare.Application.PropertyContacts;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyContactsController : ControllerBase
{
	public class PropertyContactEx : PropertyContact
	{
		public Annonceur[] Annonceurs { get; set; }
	}

	public class Annonceur
	{
		public long Id { get; set; }

		public string Name { get; set; }

		public string Email { get; set; }

		[JsonPropertyName("PhoneNumber")]
		public string Phone { get; set; }

		public IntermediairesDirects IntDirect { get; set; }

		public IntermediairesIndirects IntIndirect { get; set; }
	}

	private readonly ApplicationDbContext _context;

	private readonly ILogger<PropertyContactsController> _logger;

	public PropertyContactsController(ApplicationDbContext context, ILogger<PropertyContactsController> logger)
	{
		_context = context;
		_logger = logger;
	}

	[HttpGet]
	[Route("/api/contact/{contactRef}/properties")]
	public async Task<ActionResult<SelectResult<PropertyContactEx>>> GetProperties(long contactRef, [FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] bool enrichAgences = true)
	{
		try
		{
			IQueryable<PropertyContact> query = _context.PropertyContact.Where((PropertyContact pc) => (long)pc.PcRefContact == contactRef);
			SelectResult<PropertyContact> res = await EFHelper<PropertyContact>.Select(query, where, orderby, take, skip, select, (IQueryable<PropertyContact> q) => q.Include((PropertyContact p) => p.PcProperty));
			SelectResult<PropertyContactEx> res2 = new SelectResult<PropertyContactEx>
			{
				Total = res.Total,
				Items = res.Items.Select((PropertyContact source) => EFHelper<PropertyContact>.Copy<PropertyContactEx>(source)).ToArray()
			};
			PropertyContactEx[] items = res2.Items;
			foreach (PropertyContactEx i in items)
			{
				if (!string.IsNullOrEmpty(i.PcProperty?.PAnnonceurs))
				{
					i.Annonceurs = JsonSerializer.Deserialize<Annonceur[]>(i.PcProperty.PAnnonceurs);
				}
			}
			if (!enrichAgences)
			{
				return res2;
			}
			long[] idIds = (from annonceur in res2.Items.SelectMany((PropertyContactEx propertyContactEx) => propertyContactEx.Annonceurs)
				where annonceur.Id != 0
				select annonceur.Id).Distinct().ToArray();
			if (idIds.Length != 0)
			{
				IntermediairesDirects[] intDirects = _context.IntermediairesDirects.Where((IntermediairesDirects intermediairesDirects) => idIds.Contains(intermediairesDirects.IIdYanport)).Include((IntermediairesDirects v) => v.ContactIntermediaire).AsNoTracking()
					.ToArray();
				foreach (Annonceur a in res2.Items.SelectMany((PropertyContactEx propertyContactEx) => propertyContactEx.Annonceurs))
				{
					if (a.Id != 0)
					{
						a.IntDirect = intDirects.FirstOrDefault((IntermediairesDirects d) => d.IIdYanport == a.Id);
					}
				}
				string[] groupEventuels = (from d in intDirects
					select d.IGroupeEventuel into g
					where !string.IsNullOrEmpty(g)
					select g).Distinct().ToArray();
				if (groupEventuels.Length != 0)
				{
					IntermediairesIndirects[] intIndirects = _context.IntermediairesIndirects.Where((IntermediairesIndirects ii) => groupEventuels.Contains(ii.I2NomIntermIndirect)).AsNoTracking().ToArray();
					foreach (Annonceur a2 in res2.Items.SelectMany((PropertyContactEx propertyContactEx) => propertyContactEx.Annonceurs))
					{
						if (a2.IntDirect != null && !string.IsNullOrEmpty(a2.IntDirect.IGroupeEventuel))
						{
							a2.IntIndirect = intIndirects.FirstOrDefault((IntermediairesIndirects d) => d.I2NomIntermIndirect == a2.IntDirect.IGroupeEventuel);
						}
					}
				}
			}
			return res2;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<PropertyContact> ApplyDefaultFilter(IQueryable<PropertyContact> query)
	{
		return query;
	}

	[HttpPost]
	[Route("/api/contact/{contactRef}/properties/shared-indicators")]
	public async Task<ActionResult<ContactSharedIndicatorsResponse>> PostSharedIndicators(
		long contactRef,
		[FromBody] ContactSharedIndicatorsRequest body)
	{
		if (contactRef <= 0)
		{
			return BadRequest();
		}
		try
		{
			ContactSharedIndicatorsResponse res = await ContactSharedIndicatorsQuery.LoadAsync(
				_context,
				(uint)contactRef,
				body?.PropertyIds);
			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "shared-indicators failed for contact {ContactRef}", contactRef);
			return BadRequest(ex.Message);
		}
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<PropertyContact>>> GetPropertyContact()
	{
		return (await _context.PropertyContact.ToListAsync());
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<PropertyContact>> GetPropertyContact(uint id)
	{
		PropertyContact propertyContact = await _context.PropertyContact.FindAsync(id);
		if (propertyContact == null)
		{
			return NotFound();
		}
		return propertyContact;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutPropertyContact(uint id, PropertyContact pc)
	{
		if (id != pc.PcId)
		{
			return BadRequest();
		}
		_context.Entry(pc).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
			if (pc.PcRefContact != 0)
			{
				DbConnection c = _context.Database.GetDbConnection();
				if (c.State != ConnectionState.Open)
				{
					await c.OpenAsync();
				}
				string tableName = "annonces_refcontact_" + pc.PcRefContact;
				SqlSafety.Identifier(tableName);
				if (c.DoesTableExist(tableName))
				{
					using (MySqlCommand cmd = (MySqlCommand)c.CreateCommand())
					{
						cmd.CommandText = $"UPDATE {tableName} SET A_Actif=@actif, A_Rate=@rate, A_Com=@com WHERE A_RefAnnGlob IN (SELECT AG_Ref FROM annonces_globales WHERE AG_IdPropertyYanport=@pid)";
						cmd.Parameters.AddWithValue("@actif", pc.PcActif ? 1 : 0);
						cmd.Parameters.AddWithValue("@rate", pc.PcRate);
						cmd.Parameters.AddWithValue("@com", pc.PcCom ?? "");
						cmd.Parameters.AddWithValue("@pid", pc.PcPropertyId ?? "");
						await cmd.ExecuteNonQueryAsync();
					}
					if (pc.PcVu)
					{
						using MySqlCommand cmd2 = (MySqlCommand)c.CreateCommand();
						cmd2.CommandText = $"UPDATE {tableName} SET A_Date_Aff=CURDATE() WHERE (A_Date_Aff IS NULL OR A_Date_Aff='0000-00-00') AND A_RefAnnGlob IN (SELECT AG_Ref FROM annonces_globales WHERE AG_IdPropertyYanport=@pid)";
						cmd2.Parameters.AddWithValue("@pid", pc.PcPropertyId ?? "");
						await cmd2.ExecuteNonQueryAsync();
					}
				}
			}
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!PropertyContactExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpDelete]
	public async Task<IActionResult> DeletePropertyContactsByContact([FromQuery] string where = null)
	{
		if (!PropertyContactBulkDelete.TryParseContactRef(where, out uint contactRef))
		{
			return BadRequest();
		}
		try
		{
			await _context.Database.ExecuteSqlRawAsync(PropertyContactBulkDelete.DeleteByContactSql, contactRef);
			return NoContent();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Bulk delete property_contact failed for contact {ContactRef}", contactRef);
			if (PropertyContactBulkDelete.IsPrivilegeDenied(ex))
			{
				return StatusCode(403);
			}
			return StatusCode(500);
		}
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeletePropertyContact(uint id)
	{
		PropertyContact propertyContact = await _context.PropertyContact.FindAsync(id);
		if (propertyContact == null)
		{
			return NotFound();
		}
		_context.PropertyContact.Remove(propertyContact);
		await _context.SaveChangesAsync();
		return NoContent();
	}

	private bool PropertyContactExists(uint id)
	{
		return _context.PropertyContact.Any((PropertyContact e) => e.PcId == id);
	}
}
