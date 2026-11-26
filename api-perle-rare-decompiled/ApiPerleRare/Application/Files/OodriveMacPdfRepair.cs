using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ApiPerleRare.Application.Files;

/// <summary>
/// Oodrive Sign ajoute, après une signature, une révision dont la table est
/// <c>/W [0 0 0]</c>. Aperçu macOS s'arrête sur cette table et n'affiche pas le PDF.
/// On ajoute une table complète compressée, sans renvoyer vers ces révisions.
/// L'objet 0 y est libre, génération 65535 : une table brute qui commence par
/// un octet nul, avec une génération 0, est refusée elle aussi. Les octets
/// déjà signés ne changent pas.
/// </summary>
internal static class OodriveMacPdfRepair
{
	public static byte[] Repair(byte[] data)
	{
		if (data == null || data.Length < 8)
		{
			return data ?? Array.Empty<byte>();
		}
		try
		{
			Resolved xref = Resolve(data);
			if (xref == null || xref.Root == null || (!xref.NeedsRepair && !MacPreviewStillRejects(data, xref)))
			{
				return data;
			}
			byte[] source = (byte[])data.Clone();
			NeutralizeUnsignedClassicTrailers(source);
			byte[] repaired = AppendCleanXref(source, xref);
			return repaired ?? data;
		}
		catch (Exception)
		{
			return data;
		}
	}

	/// <summary>
	/// La première réparation ajoutait une table complète non compressée.
	/// Aperçu la refuse : le flux commence par un octet nul. On n'y retouche
	/// que dans ce cas, pour ne pas réécrire un PDF déjà compressible.
	/// </summary>
	private static bool MacPreviewStillRejects(byte[] data, Resolved xref)
	{
		if (!xref.ActiveUncompressed)
		{
			return false;
		}
		return ContainsAscii(data, "/W [0 0 0]") || ContainsAscii(data, "/W[0 0 0]");
	}

	/// <summary>
	/// Le titre ajouté par l'application est un trailer classique hors signature.
	/// L'écran actuel le prend encore pour la dernière table et retombe sur la
	/// révision illisible. On renomme ce mot-clé seulement après les octets signés.
	/// </summary>
	private static void NeutralizeUnsignedClassicTrailers(byte[] data)
	{
		int from = SignedCoverageEnd(data);
		byte[] marker = Latin1("trailer");
		while (from >= 0 && from < data.Length)
		{
			int at = IndexOf(data, marker, from);
			if (at < 0)
			{
				return;
			}
			int next = SkipSpace(data, at + marker.Length);
			if (next + 1 < data.Length && data[next] == (byte)'<' && data[next + 1] == (byte)'<')
			{
				data[at + 6] = (byte)'x';
			}
			from = at + marker.Length;
		}
	}

	private static int SignedCoverageEnd(byte[] data)
	{
		byte[] marker = Latin1("/ByteRange");
		int from = 0;
		int covered = -1;
		while (from < data.Length)
		{
			int at = IndexOf(data, marker, from);
			if (at < 0)
			{
				break;
			}
			int open = IndexOfAscii(data, "[", at + marker.Length, Math.Min(data.Length, at + marker.Length + 80));
			if (open >= 0)
			{
				int index = open + 1;
				if (TryReadLong(data, ref index, out long start)
					&& TryReadLong(data, ref index, out long length)
					&& TryReadLong(data, ref index, out long start2)
					&& TryReadLong(data, ref index, out long length2))
				{
					long end = start2 + length2;
					if (start + length > end)
					{
						end = start + length;
					}
					if (end > covered)
					{
						covered = end > int.MaxValue ? int.MaxValue : (int)end;
					}
				}
			}
			from = at + marker.Length;
		}
		return covered < 0 ? 0 : covered;
	}

	private static byte[] AppendCleanXref(byte[] data, Resolved xref)
	{
		int highest = 0;
		foreach (int number in xref.Entries.Keys)
		{
			if (number > highest)
			{
				highest = number;
			}
		}
		int size = Math.Max(xref.Size, highest + 1);
		int newObject = size;
		int newSize = newObject + 1;
		bool needNewline = data[data.Length - 1] != (byte)'\n' && data[data.Length - 1] != (byte)'\r';
		int objectStart = data.Length + (needNewline ? 1 : 0);
		var entries = new XEntry[newSize];
		for (int number = 0; number < newSize; number++)
		{
			if (xref.Entries.TryGetValue(number, out XEntry known))
			{
				entries[number] = known;
			}
			else
			{
				entries[number] = new XEntry
				{
					Type = 0,
					Field2 = 0,
					Field3 = number == 0 ? 65535 : 0
				};
			}
		}
		entries[newObject] = new XEntry
		{
			Type = 1,
			Field2 = objectStart,
			Field3 = 0
		};
		LinkFree(entries);
		int field2Width = 1;
		int field3Width = 2;
		for (int number = 0; number < newSize; number++)
		{
			field2Width = Math.Max(field2Width, ByteWidth(entries[number].Field2));
			field3Width = Math.Max(field3Width, ByteWidth(entries[number].Field3));
		}
		int entryWidth = 1 + field2Width + field3Width;
		int streamLength = newSize * entryWidth;
		var stream = new byte[streamLength];
		int cursor = 0;
		for (int number = 0; number < newSize; number++)
		{
			stream[cursor++] = entries[number].Type;
			WriteBigEndian(stream, ref cursor, entries[number].Field2, field2Width);
			WriteBigEndian(stream, ref cursor, entries[number].Field3, field3Width);
		}
		byte[] compressed = Deflate(stream);
		var header = new StringBuilder();
		header.Append(newObject).Append(" 0 obj\n<< /Type /XRef /Size ").Append(newSize);
		header.Append(" /W [1 ").Append(field2Width).Append(' ').Append(field3Width).Append("] /Filter /FlateDecode ");
		header.Append(xref.Root);
		if (!string.IsNullOrEmpty(xref.Info))
		{
			header.Append(' ').Append(xref.Info);
		}
		if (!string.IsNullOrEmpty(xref.Encrypt))
		{
			header.Append(' ').Append(xref.Encrypt);
		}
		if (!string.IsNullOrEmpty(xref.Id))
		{
			header.Append(' ').Append(xref.Id);
		}
		header.Append(" /Length ").Append(compressed.Length).Append(" >>\nstream\n");
		string tail = "\nendstream\nendobj\nstartxref\n" + objectStart + "\n%%EOF\n";
		byte[] headBytes = Latin1(header.ToString());
		byte[] tailBytes = Latin1(tail);
		int total = data.Length + (needNewline ? 1 : 0) + headBytes.Length + compressed.Length + tailBytes.Length;
		var output = new byte[total];
		Buffer.BlockCopy(data, 0, output, 0, data.Length);
		int at = data.Length;
		if (needNewline)
		{
			output[at++] = (byte)'\n';
		}
		Buffer.BlockCopy(headBytes, 0, output, at, headBytes.Length);
		at += headBytes.Length;
		Buffer.BlockCopy(compressed, 0, output, at, compressed.Length);
		at += compressed.Length;
		Buffer.BlockCopy(tailBytes, 0, output, at, tailBytes.Length);
		return output;
	}

	/// <summary>
	/// Liste libre exigée par Aperçu : l'objet 0 est libre, génération 65535,
	/// et chaque objet libre pointe vers le suivant.
	/// </summary>
	private static void LinkFree(XEntry[] entries)
	{
		XEntry head = entries[0];
		head.Type = 0;
		head.Field3 = 65535;
		entries[0] = head;
		var free = new List<int>();
		for (int number = 0; number < entries.Length; number++)
		{
			if (entries[number].Type == 0)
			{
				free.Add(number);
			}
		}
		for (int i = 0; i < free.Count; i++)
		{
			int number = free[i];
			XEntry entry = entries[number];
			entry.Type = 0;
			entry.Field2 = i + 1 < free.Count ? free[i + 1] : 0;
			if (number == 0)
			{
				entry.Field3 = 65535;
			}
			entries[number] = entry;
		}
	}

	private static Resolved Resolve(byte[] data)
	{
		if (!StartsWithPdf(data))
		{
			return null;
		}
		if (!TryLastStartXref(data, out int offset))
		{
			return null;
		}
		var resolved = new Resolved();
		var seen = new HashSet<int>();
		bool first = true;
		while (offset >= 0)
		{
			if (!seen.Add(offset))
			{
				return null;
			}
			if (!TryReadSection(data, offset, out Section section))
			{
				return null;
			}
			if (first)
			{
				resolved.ActiveUncompressed = section.Uncompressed;
				first = false;
			}
			if (section.BrokenWidth)
			{
				resolved.NeedsRepair = true;
			}
			else
			{
				foreach (KeyValuePair<int, XEntry> entry in section.Entries)
				{
					if (!resolved.Entries.ContainsKey(entry.Key))
					{
						resolved.Entries[entry.Key] = entry.Value;
					}
				}
			}
			if (resolved.Root == null && section.Root != null)
			{
				resolved.Root = section.Root;
				resolved.Info = section.Info;
				resolved.Encrypt = section.Encrypt;
				resolved.Id = section.Id;
				resolved.Size = section.Size;
			}
			offset = section.Prev;
		}
		return resolved;
	}

	private static bool TryReadSection(byte[] data, int offset, out Section section)
	{
		section = null;
		if (offset < 0 || offset >= data.Length)
		{
			return false;
		}
		if (MatchAscii(data, offset, "xref"))
		{
			return TryReadClassic(data, offset, out section);
		}
		return TryReadXrefStream(data, offset, out section);
	}

	private static bool TryReadClassic(byte[] data, int offset, out Section section)
	{
		section = null;
		int index = offset + 4;
		index = SkipLineEnd(data, index);
		var entries = new Dictionary<int, XEntry>();
		while (index < data.Length && IsDigit(data[index]))
		{
			if (!TryReadInt(data, ref index, out int start) || !SkipSpace(data, ref index) || !TryReadInt(data, ref index, out int count))
			{
				return false;
			}
			index = SkipLineEnd(data, index);
			for (int n = 0; n < count; n++)
			{
				if (index + 20 > data.Length)
				{
					return false;
				}
				if (!TryParseDecimal(data, index, 10, out long field2) || !TryParseDecimal(data, index + 11, 5, out long generation))
				{
					return false;
				}
				byte kind = data[index + 17];
				entries[start + n] = new XEntry
				{
					Type = kind == (byte)'n' ? (byte)1 : (byte)0,
					Field2 = field2,
					Field3 = generation
				};
				index += 20;
			}
		}
		index = SkipSpace(data, index);
		if (!MatchAscii(data, index, "trailer"))
		{
			return false;
		}
		index = SkipSpace(data, index + 7);
		if (!TryReadDict(data, index, out string dict, out _))
		{
			return false;
		}
		if (!TryDictInt(dict, "/Size", out int size) || size < 1 || !TryDictRef(dict, "/Root", out string root))
		{
			return false;
		}
		section = new Section
		{
			Entries = entries,
			Size = size,
			Root = root,
			Info = TryDictRef(dict, "/Info", out string info) ? info : null,
			Encrypt = TryDictRef(dict, "/Encrypt", out string encrypt) ? encrypt : null,
			Id = DictId(dict),
			Prev = TryDictLong(dict, "/Prev", out long prev) ? (int)prev : -1
		};
		return true;
	}

	private static bool TryReadXrefStream(byte[] data, int offset, out Section section)
	{
		section = null;
		int dictAt = IndexOfAscii(data, "<<", offset, Math.Min(data.Length, offset + 64));
		if (dictAt < 0 || !TryReadDict(data, dictAt, out string dict, out int afterDict))
		{
			return false;
		}
		if (dict.IndexOf("/Type", StringComparison.Ordinal) < 0 || dict.IndexOf("/XRef", StringComparison.Ordinal) < 0)
		{
			return false;
		}
		if (!TryDictInt(dict, "/Size", out int size) || size < 1 || !TryDictRef(dict, "/Root", out string root))
		{
			return false;
		}
		int[] widths = DictArray(dict, "/W");
		if (widths == null || widths.Length != 3)
		{
			widths = new[] { 1, 2, 1 };
		}
		bool broken = widths[0] == 0 || widths[1] == 0;
		int[] index = DictArray(dict, "/Index");
		if (index == null || index.Length == 0 || index.Length % 2 != 0)
		{
			index = new[] { 0, size };
		}
		var entries = new Dictionary<int, XEntry>();
		if (!broken)
		{
			if (!TryDictInt(dict, "/Length", out int length) || length < 0 || afterDict + length > data.Length + 32)
			{
				return false;
			}
			int streamAt = IndexOfAscii(data, "stream", afterDict - 2, Math.Min(data.Length, afterDict + 32));
			if (streamAt < 0)
			{
				return false;
			}
			streamAt += 6;
			streamAt = SkipLineEnd(data, streamAt);
			if (streamAt + length > data.Length)
			{
				return false;
			}
			byte[] raw = new byte[length];
			Buffer.BlockCopy(data, streamAt, raw, 0, length);
			if (dict.IndexOf("/FlateDecode", StringComparison.Ordinal) >= 0)
			{
				if (dict.IndexOf("/DecodeParms", StringComparison.Ordinal) >= 0)
				{
					return false;
				}
				raw = Inflate(raw);
				if (raw == null)
				{
					return false;
				}
			}
			else if (dict.IndexOf("/Filter", StringComparison.Ordinal) >= 0)
			{
				return false;
			}
			int width = widths[0] + widths[1] + widths[2];
			int expected = 0;
			for (int pair = 0; pair < index.Length; pair += 2)
			{
				expected += index[pair + 1];
			}
			if (width == 0 || raw.Length < expected * width)
			{
				return false;
			}
			int cursor = 0;
			for (int pair = 0; pair < index.Length; pair += 2)
			{
				int start = index[pair];
				int count = index[pair + 1];
				for (int n = 0; n < count; n++)
				{
					int type = widths[0] == 0 ? 1 : ReadBigEndian(raw, ref cursor, widths[0]);
					long field2 = widths[1] == 0 ? 0 : ReadBigEndianLong(raw, ref cursor, widths[1]);
					long field3 = widths[2] == 0 ? 0 : ReadBigEndianLong(raw, ref cursor, widths[2]);
					entries[start + n] = new XEntry
					{
						Type = (byte)type,
						Field2 = field2,
						Field3 = field3
					};
				}
			}
		}
		section = new Section
		{
			Entries = entries,
			BrokenWidth = broken,
			Uncompressed = dict.IndexOf("/Filter", StringComparison.Ordinal) < 0,
			Size = size,
			Root = root,
			Info = TryDictRef(dict, "/Info", out string info) ? info : null,
			Encrypt = TryDictRef(dict, "/Encrypt", out string encrypt) ? encrypt : null,
			Id = DictId(dict),
			Prev = TryDictLong(dict, "/Prev", out long prev) ? (int)prev : -1
		};
		return true;
	}

	private static byte[] Deflate(byte[] raw)
	{
		using (var destination = new MemoryStream())
		{
			using (var zlib = new ZLibStream(destination, CompressionLevel.SmallestSize, true))
			{
				zlib.Write(raw, 0, raw.Length);
			}
			return destination.ToArray();
		}
	}

	private static bool ContainsAscii(byte[] data, string text)
	{
		return IndexOf(data, Latin1(text), 0) >= 0;
	}

	private static byte[] Inflate(byte[] raw)
	{
		try
		{
			using (var source = new MemoryStream(raw))
			using (var zlib = new ZLibStream(source, CompressionMode.Decompress))
			using (var destination = new MemoryStream())
			{
				zlib.CopyTo(destination);
				return destination.ToArray();
			}
		}
		catch (Exception)
		{
			try
			{
				using (var source = new MemoryStream(raw))
				using (var deflate = new DeflateStream(source, CompressionMode.Decompress))
				using (var destination = new MemoryStream())
				{
					deflate.CopyTo(destination);
					return destination.ToArray();
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	private static bool TryLastStartXref(byte[] data, out int offset)
	{
		offset = -1;
		byte[] marker = Latin1("startxref");
		int from = data.Length;
		while (from > 0)
		{
			int at = LastIndexOf(data, marker, from);
			if (at < 0)
			{
				return false;
			}
			int index = SkipSpace(data, at + marker.Length);
			if (TryReadInt(data, ref index, out int value) && value >= 0 && value < data.Length)
			{
				offset = value;
				return true;
			}
			from = at;
		}
		return false;
	}

	private static bool TryReadDict(byte[] data, int open, out string dict, out int end)
	{
		dict = null;
		end = -1;
		if (open < 0 || open + 1 >= data.Length || data[open] != (byte)'<' || data[open + 1] != (byte)'<')
		{
			return false;
		}
		int depth = 0;
		int index = open;
		while (index < data.Length)
		{
			byte current = data[index];
			if (current == (byte)'<' && index + 1 < data.Length && data[index + 1] == (byte)'<')
			{
				depth++;
				index += 2;
				continue;
			}
			if (current == (byte)'>' && index + 1 < data.Length && data[index + 1] == (byte)'>')
			{
				depth--;
				index += 2;
				if (depth == 0)
				{
					end = index;
					dict = Latin1String(data, open, end - open);
					return true;
				}
				continue;
			}
			if (current == (byte)'(')
			{
				index = SkipLiteral(data, index);
				continue;
			}
			if (current == (byte)'<')
			{
				index = SkipHex(data, index);
				continue;
			}
			if (current == (byte)'%')
			{
				while (index < data.Length && data[index] != (byte)'\n' && data[index] != (byte)'\r')
				{
					index++;
				}
				continue;
			}
			index++;
		}
		return false;
	}

	private static int SkipLiteral(byte[] data, int index)
	{
		int depth = 1;
		index++;
		while (index < data.Length && depth > 0)
		{
			if (data[index] == (byte)'\\')
			{
				index += 2;
				continue;
			}
			if (data[index] == (byte)'(')
			{
				depth++;
			}
			else if (data[index] == (byte)')')
			{
				depth--;
			}
			index++;
		}
		return index;
	}

	private static int SkipHex(byte[] data, int index)
	{
		index++;
		while (index < data.Length && data[index] != (byte)'>')
		{
			index++;
		}
		return Math.Min(data.Length, index + 1);
	}

	private static bool TryDictRef(string dict, string key, out string reference)
	{
		reference = null;
		int at = dict.IndexOf(key, StringComparison.Ordinal);
		if (at < 0)
		{
			return false;
		}
		int index = at + key.Length;
		while (index < dict.Length && IsSpace(dict[index]))
		{
			index++;
		}
		int start = index;
		if (!ReadDigits(dict, ref index, out _))
		{
			return false;
		}
		while (index < dict.Length && IsSpace(dict[index]))
		{
			index++;
		}
		if (!ReadDigits(dict, ref index, out _))
		{
			return false;
		}
		while (index < dict.Length && IsSpace(dict[index]))
		{
			index++;
		}
		if (index >= dict.Length || dict[index] != 'R')
		{
			return false;
		}
		reference = key + " " + dict.Substring(start, index - start).Replace("\r", " ").Replace("\n", " ") + " R";
		reference = CollapseSpaces(reference);
		return true;
	}

	private static bool TryDictInt(string dict, string key, out int value)
	{
		long number;
		bool ok = TryDictLong(dict, key, out number);
		value = (int)number;
		return ok && number >= int.MinValue && number <= int.MaxValue;
	}

	private static bool TryDictLong(string dict, string key, out long value)
	{
		value = 0;
		int at = dict.IndexOf(key, StringComparison.Ordinal);
		if (at < 0)
		{
			return false;
		}
		int index = at + key.Length;
		while (index < dict.Length && IsSpace(dict[index]))
		{
			index++;
		}
		return ReadDigits(dict, ref index, out value);
	}

	private static int[] DictArray(string dict, string key)
	{
		int at = dict.IndexOf(key, StringComparison.Ordinal);
		if (at < 0)
		{
			return null;
		}
		int open = dict.IndexOf('[', at + key.Length);
		int close = open < 0 ? -1 : dict.IndexOf(']', open + 1);
		if (open < 0 || close < 0)
		{
			return null;
		}
		string body = dict.Substring(open + 1, close - open - 1);
		var numbers = new List<int>();
		int index = 0;
		while (index < body.Length)
		{
			while (index < body.Length && !IsDigit(body[index]))
			{
				index++;
			}
			if (index >= body.Length)
			{
				break;
			}
			if (!ReadDigits(body, ref index, out long number))
			{
				break;
			}
			numbers.Add((int)number);
		}
		return numbers.ToArray();
	}

	private static string DictId(string dict)
	{
		int at = dict.IndexOf("/ID", StringComparison.Ordinal);
		if (at < 0)
		{
			return null;
		}
		int open = dict.IndexOf('[', at + 3);
		int close = open < 0 ? -1 : dict.IndexOf(']', open + 1);
		if (open < 0 || close < 0)
		{
			return null;
		}
		return "/ID [" + CollapseSpaces(dict.Substring(open + 1, close - open - 1)) + "]";
	}

	private static string CollapseSpaces(string value)
	{
		var builder = new StringBuilder(value.Length);
		bool space = false;
		for (int i = 0; i < value.Length; i++)
		{
			char current = value[i];
			if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
			{
				space = builder.Length > 0;
				continue;
			}
			if (space)
			{
				builder.Append(' ');
				space = false;
			}
			builder.Append(current);
		}
		return builder.ToString();
	}

	private static bool ReadDigits(string text, ref int index, out long value)
	{
		value = 0;
		if (index >= text.Length || !IsDigit(text[index]))
		{
			return false;
		}
		while (index < text.Length && IsDigit(text[index]))
		{
			value = value * 10 + (text[index] - '0');
			index++;
		}
		return true;
	}

	private static bool TryReadInt(byte[] data, ref int index, out int value)
	{
		value = 0;
		if (index >= data.Length || !IsDigit(data[index]))
		{
			return false;
		}
		while (index < data.Length && IsDigit(data[index]))
		{
			value = value * 10 + (data[index] - (byte)'0');
			index++;
		}
		return true;
	}

	private static bool TryParseDecimal(byte[] data, int index, int length, out long value)
	{
		value = 0;
		for (int i = 0; i < length; i++)
		{
			byte current = data[index + i];
			if (!IsDigit(current))
			{
				return false;
			}
			value = value * 10 + (current - (byte)'0');
		}
		return true;
	}

	private static int ReadBigEndian(byte[] data, ref int index, int width)
	{
		return (int)ReadBigEndianLong(data, ref index, width);
	}

	private static long ReadBigEndianLong(byte[] data, ref int index, int width)
	{
		long value = 0;
		for (int i = 0; i < width; i++)
		{
			value = (value << 8) | data[index++];
		}
		return value;
	}

	private static void WriteBigEndian(byte[] data, ref int index, long value, int width)
	{
		for (int shift = (width - 1) * 8; shift >= 0; shift -= 8)
		{
			data[index++] = (byte)((value >> shift) & 0xff);
		}
	}

	private static int ByteWidth(long value)
	{
		int width = 1;
		ulong rest = value < 0 ? 0 : (ulong)value;
		while (rest > 255 && width < 8)
		{
			rest >>= 8;
			width++;
		}
		return width;
	}

	private static bool StartsWithPdf(byte[] data)
	{
		return data.Length >= 5
			&& data[0] == (byte)'%'
			&& data[1] == (byte)'P'
			&& data[2] == (byte)'D'
			&& data[3] == (byte)'F'
			&& data[4] == (byte)'-';
	}

	private static bool MatchAscii(byte[] data, int index, string text)
	{
		if (index < 0 || index + text.Length > data.Length)
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (data[index + i] != (byte)text[i])
			{
				return false;
			}
		}
		return true;
	}

	private static int IndexOf(byte[] data, byte[] needle, int from)
	{
		return IndexOfAscii(data, needle, from, data.Length);
	}

	private static int IndexOfAscii(byte[] data, byte[] needle, int from, int until)
	{
		int last = Math.Min(data.Length, until) - needle.Length;
		for (int i = Math.Max(0, from); i <= last; i++)
		{
			bool found = true;
			for (int j = 0; j < needle.Length; j++)
			{
				if (data[i + j] != needle[j])
				{
					found = false;
					break;
				}
			}
			if (found)
			{
				return i;
			}
		}
		return -1;
	}

	private static bool TryReadLong(byte[] data, ref int index, out long value)
	{
		value = 0;
		while (index < data.Length && IsSpace((char)data[index]))
		{
			index++;
		}
		if (index >= data.Length || !IsDigit(data[index]))
		{
			return false;
		}
		while (index < data.Length && IsDigit(data[index]))
		{
			value = value * 10 + (data[index] - (byte)'0');
			index++;
		}
		return true;
	}

	private static int IndexOfAscii(byte[] data, string text, int from, int until)
	{
		byte[] needle = Latin1(text);
		int last = Math.Min(data.Length, until) - needle.Length;
		for (int i = Math.Max(0, from); i <= last; i++)
		{
			bool match = true;
			for (int j = 0; j < needle.Length; j++)
			{
				if (data[i + j] != needle[j])
				{
					match = false;
					break;
				}
			}
			if (match)
			{
				return i;
			}
		}
		return -1;
	}

	private static int LastIndexOf(byte[] data, byte[] needle, int before)
	{
		int last = Math.Min(before, data.Length) - needle.Length;
		for (int i = last; i >= 0; i--)
		{
			bool match = true;
			for (int j = 0; j < needle.Length; j++)
			{
				if (data[i + j] != needle[j])
				{
					match = false;
					break;
				}
			}
			if (match)
			{
				return i;
			}
		}
		return -1;
	}

	private static int SkipSpace(byte[] data, int index)
	{
		while (index < data.Length && IsSpace((char)data[index]))
		{
			index++;
		}
		return index;
	}

	private static bool SkipSpace(byte[] data, ref int index)
	{
		if (index >= data.Length || !IsSpace((char)data[index]))
		{
			return false;
		}
		while (index < data.Length && IsSpace((char)data[index]))
		{
			index++;
		}
		return true;
	}

	private static int SkipLineEnd(byte[] data, int index)
	{
		if (index < data.Length && data[index] == (byte)'\r')
		{
			index++;
		}
		if (index < data.Length && data[index] == (byte)'\n')
		{
			index++;
		}
		return index;
	}

	private static bool IsDigit(byte value)
	{
		return value >= (byte)'0' && value <= (byte)'9';
	}

	private static bool IsDigit(char value)
	{
		return value >= '0' && value <= '9';
	}

	private static bool IsSpace(char value)
	{
		return value == ' ' || value == '\t' || value == '\r' || value == '\n';
	}

	private static byte[] Latin1(string text)
	{
		var bytes = new byte[text.Length];
		for (int i = 0; i < text.Length; i++)
		{
			bytes[i] = (byte)text[i];
		}
		return bytes;
	}

	private static string Latin1String(byte[] data, int index, int length)
	{
		var chars = new char[length];
		for (int i = 0; i < length; i++)
		{
			chars[i] = (char)data[index + i];
		}
		return new string(chars);
	}

	private sealed class Resolved
	{
		public Dictionary<int, XEntry> Entries { get; } = new Dictionary<int, XEntry>();

		public bool NeedsRepair { get; set; }

		public bool ActiveUncompressed { get; set; }

		public string Root { get; set; }

		public string Info { get; set; }

		public string Encrypt { get; set; }

		public string Id { get; set; }

		public int Size { get; set; }
	}

	private sealed class Section
	{
		public Dictionary<int, XEntry> Entries { get; set; }

		public bool BrokenWidth { get; set; }

		public bool Uncompressed { get; set; }

		public int Size { get; set; }

		public string Root { get; set; }

		public string Info { get; set; }

		public string Encrypt { get; set; }

		public string Id { get; set; }

		public int Prev { get; set; } = -1;
	}

	private struct XEntry
	{
		public byte Type;

		public long Field2;

		public long Field3;
	}
}
