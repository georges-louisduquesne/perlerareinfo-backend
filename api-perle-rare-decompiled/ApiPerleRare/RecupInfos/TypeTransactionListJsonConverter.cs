using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPerleRare.RecupInfos;

/// <summary>
/// Front Recherche sends typeTransaction as "A" or ["A"]. ASP.NET would otherwise 400 the counts call.
/// </summary>
public sealed class TypeTransactionListJsonConverter : JsonConverter<TypeTransaction[]>
{
	public override TypeTransaction[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return Array.Empty<TypeTransaction>();
		}
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			List<TypeTransaction> list = new List<TypeTransaction>();
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
				{
					break;
				}
				TypeTransaction one = ReadOne(ref reader);
				if (one != TypeTransaction.None)
				{
					list.Add(one);
				}
			}
			return list.ToArray();
		}
		TypeTransaction single = ReadOne(ref reader);
		return single == TypeTransaction.None ? Array.Empty<TypeTransaction>() : new[] { single };
	}

	public override void Write(Utf8JsonWriter writer, TypeTransaction[] value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		if (value != null)
		{
			foreach (TypeTransaction item in value)
			{
				if (item != TypeTransaction.None)
				{
					writer.WriteStringValue(item.ToString());
				}
			}
		}
		writer.WriteEndArray();
	}

	private static TypeTransaction ReadOne(ref Utf8JsonReader reader)
	{
		if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int n))
		{
			return Enum.IsDefined(typeof(TypeTransaction), n) ? (TypeTransaction)n : TypeTransaction.None;
		}
		string text = reader.TokenType == JsonTokenType.String ? reader.GetString() : Convert.ToString(reader.GetInt32());
		if (string.IsNullOrWhiteSpace(text))
		{
			return TypeTransaction.None;
		}
		return Enum.TryParse(text, ignoreCase: true, out TypeTransaction parsed) ? parsed : TypeTransaction.None;
	}
}
