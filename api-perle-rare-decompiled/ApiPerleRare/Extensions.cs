using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using ApiPerleRare.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare;

public static class Extensions
{
	public static long ToUnixTimeSeconds(this DateTime value)
	{
		return value.Ticks / 10000000 - 62135596800L;
	}

	public static DateTime ToDateTime(this long value)
	{
		return new DateTime((value + 62135596800L) * 10000000);
	}

	public static DateTime? ToLocalTime(this DateTime? date)
	{
		if (!date.HasValue)
		{
			return null;
		}
		return date.Value.ToLocalTime();
	}

	public static string GetLogin(this ClaimsPrincipal user)
	{
		return user.Claims.First((Claim c) => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
	}

	public static int GetId(this ClaimsPrincipal user)
	{
		return int.Parse(user.Identity.Name);
	}

	public static string GetUserLogin(this ControllerBase controller)
	{
		return controller.User.GetLogin();
	}

	public static int GetUserId(this ControllerBase controller)
	{
		return controller.User.GetId();
	}

	public static bool DoesTableExist(this IDbConnection connection, string tableName)
	{
		using IDbCommand cmd = connection.CreateCommand();
		cmd.CommandText = $"SELECT COUNT(*) AS NB\r\n                    FROM information_schema.tables\r\n                    WHERE table_schema = '{connection.Database}'\r\n                    AND table_name = '{tableName}';";
		return (long)cmd.ExecuteScalar() > 0;
	}

	public static TModel[] ToModels<TModel>(this IDataReader dataReader) where TModel : class, new()
	{
		DataReaderHelper<TModel> drh = new DataReaderHelper<TModel>(dataReader);
		return drh.ReadAll();
	}

	public static IEnumerable<T> FixEncoding<T>(this IEnumerable<T> items)
	{
		EncodingHelper.FixEncodingInEnumerable(items);
		return items;
	}

	public static string Truncate(this string text, int maxLength)
	{
		if (text == null || text.Length < maxLength)
		{
			return text;
		}
		return text.Substring(0, maxLength);
	}
}
