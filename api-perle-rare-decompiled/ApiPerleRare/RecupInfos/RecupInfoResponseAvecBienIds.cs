using System;
using System.Collections.Generic;
using MySqlConnector;

namespace ApiPerleRare.RecupInfos;

public class RecupInfoResponseAvecBienIds : AbstractRecupInfoResponse<RecupInfoCountAvecBienIds>
{
	protected override string AdjustSql(string sql)
	{
		if (!sql.Contains("COUNT(DISTINCT AG_Ref_AGC) AS NbAnnonces"))
		{
			throw new Exception("'" + sql + "' doit contenir 'COUNT(DISTINCT AG_Ref_AGC) AS NbAnnonces'");
		}
		sql = sql.Replace("COUNT(DISTINCT AG_Ref_AGC) AS NbAnnonces", "COUNT(*) AS NbAnnonces, AG_Ref_AGC as BienId");
		sql = ((!sql.Contains("GROUP BY")) ? (sql + " GROUP BY AG_Ref_AGC") : (sql + ", AG_Ref_AGC"));
		return sql;
	}

	protected override void AddInfoFromDataReader(List<RecupInfoCountAvecBienIds> list, MySqlDataReader reader, string field, string value)
	{
		int nb = reader.GetInt32(1);
		uint bienId = reader.GetUInt32(2);
		RecupInfoCountAvecBienIds count = list.Find((RecupInfoCountAvecBienIds li) => li.Field == field && li.Value == value);
		if (count == null)
		{
			count = new RecupInfoCountAvecBienIds
			{
				Field = field,
				Value = value
			};
			list.Add(count);
		}
		count.Nb += nb;
		count.BienIds.Add(bienId);
	}

	protected override RecupInfoCountAvecBienIds Clone(RecupInfoCountAvecBienIds count)
	{
		return new RecupInfoCountAvecBienIds
		{
			BienIds = new List<long>(count.BienIds),
			Field = count.Field,
			Nb = count.Nb,
			Value = count.Value
		};
	}

	protected override void Merge(RecupInfoCountAvecBienIds master, RecupInfoCountAvecBienIds slave)
	{
		foreach (long id in slave.BienIds)
		{
			if (!master.BienIds.Contains(id))
			{
				master.BienIds.Add(id);
			}
		}
		master.Nb += slave.Nb;
	}
}
