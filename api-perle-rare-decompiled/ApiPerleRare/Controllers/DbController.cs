using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize(Roles = "Admin")]
public class DbController : ControllerBase
{
	private readonly ApplicationDbContext _dbContext;

	public DbController(ApplicationDbContext context)
	{
		_dbContext = context;
	}

	[HttpGet("{model}/{id}")]
	public async Task<ActionResult> Select(string model, string id)
	{
		try
		{
			IEntityType type = _dbContext.Model.GetEntityTypes().FirstOrDefault((IEntityType e) => string.Compare(e.ShortName(), model, ignoreCase: true) == 0);
			if (type == null)
			{
				return BadRequest("type '" + model + "' inconnu");
			}
			IKey primaryKey = type.FindPrimaryKey();
			if (primaryKey == null)
			{
				return BadRequest("Aucune clé primaire pour '" + type.Name + "'");
			}
			object keyValue = CastTo(id, primaryKey.GetKeyType());
			object rec = await _dbContext.FindAsync(type.ClrType, keyValue);
			if (rec == null)
			{
				return NotFound();
			}
			return Ok(rec);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ClientError.Generic);
		}
	}

	[HttpPut("{model}/{id}")]
	public ActionResult<string> Update(string model, string id, [FromBody] IDictionary<string, object> requestParam)
	{
		try
		{
			if (requestParam.Count == 0)
			{
				return "OK";
			}
			IEntityType type = _dbContext.Model.GetEntityTypes().FirstOrDefault((IEntityType e) => string.Compare(e.ShortName(), model, ignoreCase: true) == 0);
			if (type == null)
			{
				return "type '" + model + "' inconnu";
			}
			IKey primaryKey = type.FindPrimaryKey();
			if (primaryKey == null)
			{
				return "Aucune clé primaire pour '" + type.Name + "'";
			}
			object keyValue = CastTo(id, primaryKey.GetKeyType());
			object record = Activator.CreateInstance(type.ConstructorBinding.RuntimeType);
			primaryKey.Properties.Single().FieldInfo.SetValue(record, keyValue);
			EntityEntry entry = _dbContext.Entry(record);
			IEnumerable<IProperty> props = type.GetProperties();
			foreach (KeyValuePair<string, object> v in requestParam)
			{
				IProperty prop = props.FirstOrDefault((IProperty p) => string.Compare(p.Name, v.Key, ignoreCase: true) == 0);
				prop.FieldInfo.SetValue(record, CastTo(v.Value, prop.FieldInfo.FieldType));
				entry.Property(prop).IsModified = true;
			}
			_dbContext.SaveChanges();
			return Ok();
		}
		catch (Exception ex)
		{
			return BadRequest(ClientError.Generic);
		}
	}

	[HttpPost("{model}")]
	public ActionResult<string> Insert(string model, [FromBody] IDictionary<string, object> requestParam)
	{
		try
		{
			IEntityType type = _dbContext.Model.GetEntityTypes().FirstOrDefault((IEntityType e) => string.Compare(e.ShortName(), model, ignoreCase: true) == 0);
			if (type == null)
			{
				return "type '" + model + "' inconnu";
			}
			IKey primaryKey = type.FindPrimaryKey();
			if (primaryKey == null)
			{
				return "Aucune clé primaire pour '" + type.Name + "'";
			}
			object record = Activator.CreateInstance(type.ConstructorBinding.RuntimeType);
			IEnumerable<IProperty> props = type.GetProperties();
			foreach (KeyValuePair<string, object> v in requestParam)
			{
				IProperty prop = props.FirstOrDefault((IProperty p) => string.Compare(p.Name, v.Key, ignoreCase: true) == 0);
				prop.FieldInfo.SetValue(record, CastTo(v.Value, prop.FieldInfo.FieldType));
			}
			_dbContext.Add(record);
			_dbContext.SaveChanges();
			return Ok(record);
		}
		catch (Exception ex)
		{
			return BadRequest(ClientError.Generic);
		}
	}

	[HttpDelete("{model}/{id}")]
	public ActionResult<string> Delete(string model, string id)
	{
		try
		{
			IEntityType type = _dbContext.Model.GetEntityTypes().FirstOrDefault((IEntityType e) => string.Compare(e.ShortName(), model, ignoreCase: true) == 0);
			if (type == null)
			{
				return "type '" + model + "' inconnu";
			}
			IKey primaryKey = type.FindPrimaryKey();
			if (primaryKey == null)
			{
				return "Aucune clé primaire pour '" + type.Name + "'";
			}
			object keyValue = CastTo(id, primaryKey.GetKeyType());
			object record = Activator.CreateInstance(type.ConstructorBinding.RuntimeType);
			primaryKey.Properties.Single().FieldInfo.SetValue(record, keyValue);
			_dbContext.Remove(record);
			_dbContext.SaveChanges();
			return Ok();
		}
		catch (Exception ex)
		{
			return BadRequest(ClientError.Generic);
		}
	}

	private object CastTo(object val, Type type)
	{
		if (val == null)
		{
			return null;
		}
		if (val is JsonElement je)
		{
			return JsonSerializer.Deserialize(je, type, (JsonSerializerOptions)null);
		}
		return Convert.ChangeType(val, type, CultureInfo.InvariantCulture);
	}

	private string GetVal(object v)
	{
		if (v is JsonElement je)
		{
			return je.GetRawText();
		}
		return v.ToString();
	}
}
