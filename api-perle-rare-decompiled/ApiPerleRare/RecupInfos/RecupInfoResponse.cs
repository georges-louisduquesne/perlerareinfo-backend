using System.Collections.Generic;
using MySqlConnector;

namespace ApiPerleRare.RecupInfos;

public class RecupInfoResponse : AbstractRecupInfoResponse<RecupInfoCount>
{
	protected override void AddInfoFromDataReader(List<RecupInfoCount> list, MySqlDataReader reader, string field, string value)
	{
		list.Add(new RecupInfoCount
		{
			Field = field,
			Value = value,
			Nb = reader.GetInt32(1)
		});
	}

	protected override RecupInfoCount Clone(RecupInfoCount count)
	{
		return new RecupInfoCount
		{
			Field = count.Field,
			Nb = count.Nb,
			Value = count.Value
		};
	}

	protected override void Merge(RecupInfoCount master, RecupInfoCount slave)
	{
		master.Nb += slave.Nb;
	}
}
