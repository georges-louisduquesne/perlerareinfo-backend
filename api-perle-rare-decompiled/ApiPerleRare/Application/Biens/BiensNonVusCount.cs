using System;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace ApiPerleRare.Application.BiensNonVus;

/// <summary>
/// Compte des biens non vus d'un conseiller.
/// Deux recherches indexées (conseiller, puis 2e conseiller) au lieu d'un
/// parcours de property_contact : le OR empêchait tout index.
/// </summary>
internal static class BiensNonVusCount
{
	internal static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);

	internal const string Sql = @"
SELECT COUNT(*) FROM (
  SELECT pc.PC_Id
  FROM contacts_recherche cr
  INNER JOIN property_contact pc
    ON pc.PC_RefContact = cr.C_RefContact AND pc.PC_Actif = 1 AND pc.PC_Vu <> 1
  INNER JOIN property p ON p.P_PropertyId = pc.PC_PropertyId
  WHERE cr.C_NomFamilleConseiller = @conseillerid
  UNION
  SELECT pc.PC_Id
  FROM contacts_recherche cr
  INNER JOIN property_contact pc
    ON pc.PC_RefContact = cr.C_RefContact AND pc.PC_Actif = 1 AND pc.PC_Vu <> 1
  INNER JOIN property p ON p.P_PropertyId = pc.PC_PropertyId
  WHERE cr.C_2emeConseiller = @conseillerid2
) non_vus";

	internal static string CacheKey(string login)
	{
		return "biens-non-vus:" + (login ?? "");
	}

	internal static long GetOrCompute(IMemoryCache cache, string login, Func<long> compute)
	{
		return GetOrCompute(cache, login, compute, CacheTtl);
	}

	internal static long GetOrCompute(IMemoryCache cache, string login, Func<long> compute, TimeSpan ttl)
	{
		string key = CacheKey(login);
		if (cache.TryGetValue(key, out long cached))
		{
			return cached;
		}
		long value = compute();
		cache.Set(key, value, ttl);
		return value;
	}

	internal static long ReadCount(object res)
	{
		if (res is long v)
		{
			return v;
		}
		if (res is int i)
		{
			return i;
		}
		if (res == null || res is DBNull)
		{
			Trace.WriteLine("Null récupéré pour Non vu");
			return -1L;
		}
		throw new Exception("Type '" + res.GetType().FullName + "' non géré");
	}
}
