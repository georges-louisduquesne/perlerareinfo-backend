using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ApiPerleRare.Application.Files;
using Xunit;

namespace ApiPerleRare.Tests;

public class OodriveMacPdfRepairTests
{
	[Fact]
	public void Repair_leaves_a_normal_pdf_unchanged()
	{
		byte[] pdf = ClassicPdf();
		byte[] repaired = OodriveMacPdfRepair.Repair(pdf);
		Assert.Equal(pdf, repaired);
	}

	[Fact]
	public void Repair_appends_a_readable_xref_without_touching_signed_bytes()
	{
		byte[] broken = OodriveSignedPdf(out int updatedCatalog);
		byte[] repaired = OodriveMacPdfRepair.Repair(broken);

		Assert.True(repaired.Length > broken.Length);
		Assert.Equal(broken, repaired.AsSpan(0, broken.Length).ToArray());
		byte[] rows = XrefRows(repaired, broken.Length, out int field2Width, out int field3Width);
		int width = 1 + field2Width + field3Width;
		Assert.Equal(0, rows[0]);
		Assert.Equal(65535, ReadBe(rows, 1 + field2Width, field3Width));
		Assert.Equal(updatedCatalog, ReadBe(rows, width + 1, field2Width));
		byte[] again = OodriveMacPdfRepair.Repair(repaired);
		Assert.Equal(repaired, again);
	}

	[Fact]
	public void Repair_replaces_the_uncompressed_trailer_preview_rejects()
	{
		byte[] broken = OodriveSignedPdf(out int updatedCatalog);
		byte[] old = UncompressedTrailer(broken, updatedCatalog);
		byte[] repaired = OodriveMacPdfRepair.Repair(old);
		Assert.True(repaired.Length > old.Length);
		Assert.Equal(old, repaired.AsSpan(0, old.Length).ToArray());
		byte[] rows = XrefRows(repaired, old.Length, out int field2Width, out int field3Width);
		Assert.Equal(65535, ReadBe(rows, 1 + field2Width, field3Width));
		Assert.Equal(updatedCatalog, ReadBe(rows, 1 + field2Width + field3Width + 1, field2Width));
		Assert.Equal(repaired, OodriveMacPdfRepair.Repair(repaired));
	}

	[Fact]
	public void Repair_keeps_objects_stored_inside_an_object_stream()
	{
		byte[] broken = PdfWithCompressedObject(out int streamObject, out int index);
		byte[] repaired = OodriveMacPdfRepair.Repair(broken);
		byte[] rows = XrefRows(repaired, broken.Length, out int field2Width, out int field3Width);
		int width = 1 + field2Width + field3Width;
		int at = 2 * width;
		Assert.Equal(2, rows[at]);
		Assert.Equal(streamObject, ReadBe(rows, at + 1, field2Width));
		Assert.Equal(index, ReadBe(rows, at + 1 + field2Width, field3Width));
	}

	[Fact]
	public void Upload_stores_the_repaired_pdf()
	{
		byte[] broken = OodriveSignedPdf(out _);
		var storage = new MemoryFileStorage();
		storage.AllowResolve("mandats/MD_LOVICHI.pdf", "/files/mandats/MD_LOVICHI.pdf");
		var useCase = new UploadFileUseCase(storage);

		FileResponse result = useCase.Execute(new FileUploadRequest
		{
			FileName = "mandats/MD_LOVICHI.pdf",
			FileContent = Convert.ToBase64String(broken)
		});

		Assert.True(result.IsSuccess);
		byte[] stored = storage.Files["/files/mandats/MD_LOVICHI.pdf"];
		Assert.True(stored.Length > broken.Length);
		Assert.Equal(broken, stored.AsSpan(0, broken.Length).ToArray());
		Assert.DoesNotContain("/Prev", Encoding.ASCII.GetString(stored, broken.Length, stored.Length - broken.Length));
	}

	[Fact]
	public void Repair_renames_only_the_unsigned_classic_trailer()
	{
		byte[] classic = ClassicPdf();
		int signedEnd = classic.Length;
		int classicXref = Encoding.ASCII.GetString(classic).IndexOf("xref\n", StringComparison.Ordinal);
		string unsigned = "trailer\n<< /Size 3 /Root 1 0 R >>\nstartxref\n0\n%%EOF\n";
		string broken = "5 0 obj\n<< /Type /XRef /Size 3 /Root 1 0 R /Prev " + classicXref + " /ByteRange [0 " + signedEnd + " " + signedEnd + " 0] /Index [0 1] /W [0 0 0] /Length 0 >>\nstream\n\nendstream\nendobj\nstartxref\n" + (signedEnd + unsigned.Length) + "\n%%EOF\n";
		byte[] input = Concat(classic, Encoding.ASCII.GetBytes(unsigned + broken));
		byte[] repaired = OodriveMacPdfRepair.Repair(input);

		Assert.Equal(classic, repaired.AsSpan(0, classic.Length).ToArray());
		string middle = Encoding.ASCII.GetString(repaired, signedEnd, unsigned.Length);
		Assert.Contains("trailex", middle);
		Assert.DoesNotContain("trailer", middle);
		Assert.Contains("trailer", Encoding.ASCII.GetString(repaired, 0, classic.Length));
	}

	private static byte[] XrefRows(byte[] pdf, int tailAt, out int field2Width, out int field3Width)
	{
		string tail = Encoding.Latin1.GetString(pdf, tailAt, pdf.Length - tailAt);
		int streamMarker = tail.IndexOf("stream\n", StringComparison.Ordinal);
		string header = tail.Substring(0, streamMarker);
		Assert.Contains("/Type /XRef", header);
		Assert.Contains("/Filter /FlateDecode", header);
		Assert.DoesNotContain("/Prev", header);
		Assert.DoesNotContain("/W [0", header);
		var widths = System.Text.RegularExpressions.Regex.Match(header, @"/W \[1 (\d+) (\d+)\]");
		Assert.True(widths.Success);
		field2Width = int.Parse(widths.Groups[1].Value);
		field3Width = int.Parse(widths.Groups[2].Value);
		int length = int.Parse(System.Text.RegularExpressions.Regex.Match(header, @"/Length (\d+)").Groups[1].Value);
		int stream = tailAt + streamMarker + "stream\n".Length;
		var packed = new byte[length];
		Buffer.BlockCopy(pdf, stream, packed, 0, length);
		return Inflate(packed);
	}

	private static int ReadBe(byte[] data, int index, int width)
	{
		int value = 0;
		for (int i = 0; i < width; i++)
		{
			value = (value << 8) | data[index + i];
		}
		return value;
	}

	private static byte[] Inflate(byte[] raw)
	{
		using (var source = new MemoryStream(raw))
		using (var zlib = new ZLibStream(source, CompressionMode.Decompress))
		using (var destination = new MemoryStream())
		{
			zlib.CopyTo(destination);
			return destination.ToArray();
		}
	}

	private static byte[] UncompressedTrailer(byte[] broken, int catalog)
	{
		bool newline = broken[broken.Length - 1] != (byte)'\n' && broken[broken.Length - 1] != (byte)'\r';
		int start = broken.Length + (newline ? 1 : 0);
		var rows = new byte[15];
		rows[5] = 1;
		rows[6] = (byte)((catalog >> 16) & 0xff);
		rows[7] = (byte)((catalog >> 8) & 0xff);
		rows[8] = (byte)(catalog & 0xff);
		rows[10] = 1;
		rows[11] = (byte)((start >> 16) & 0xff);
		rows[12] = (byte)((start >> 8) & 0xff);
		rows[13] = (byte)(start & 0xff);
		string dict = "2 0 obj\n<< /Type /XRef /Size 3 /Root 1 0 R /W [1 3 1] /Length 15 >>\nstream\n";
		string tail = "\nendstream\nendobj\nstartxref\n" + start + "\n%%EOF\n";
		return Concat(
			broken,
			newline ? new byte[] { 10 } : Array.Empty<byte>(),
			Encoding.ASCII.GetBytes(dict),
			rows,
			Encoding.ASCII.GetBytes(tail));
	}

	private static byte[] ClassicPdf()
	{
		string body = "%PDF-1.4\n"
			+ "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
			+ "2 0 obj\n<< /Type /Pages /Count 0 /Kids [] >>\nendobj\n";
		int xrefAt = body.Length;
		string xref = "xref\n0 3\n"
			+ "0000000000 65535 f \n"
			+ Offset(body.IndexOf("1 0 obj", StringComparison.Ordinal))
			+ Offset(body.IndexOf("2 0 obj", StringComparison.Ordinal))
			+ "trailer\n<< /Size 3 /Root 1 0 R >>\n"
			+ "startxref\n" + xrefAt + "\n%%EOF\n";
		return Encoding.ASCII.GetBytes(body + xref);
	}

	private static byte[] OodriveSignedPdf(out int updatedCatalog)
	{
		byte[] classic = ClassicPdf();
		string updateBody = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Lang (fr) >>\nendobj\n";
		updatedCatalog = classic.Length;
		int classicXref = IndexOf(classic, "xref\n");
		byte[] row0 = XrefRow(0, 0);
		byte[] row1 = XrefRow(1, updatedCatalog);
		byte[] rows = Concat(row0, row1);
		byte[] compressed = Zlib(rows);
		string dict = "6 0 obj\n<< /Type /XRef /Size 6 /Root 1 0 R /Info 2 0 R "
			+ "/ID [<AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA> <BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB>] "
			+ "/Prev " + classicXref + " /Index [0 1 1 1] /W [1 3 0] /Filter /FlateDecode /Length " + compressed.Length + " >>\nstream\n";
		int streamObject = classic.Length + updateBody.Length;
		string tail = "\nendstream\nendobj\nstartxref\n" + streamObject + "\n%%EOF\n";
		byte[] signed = Concat(classic, Encoding.ASCII.GetBytes(updateBody), Encoding.ASCII.GetBytes(dict), compressed, Encoding.ASCII.GetBytes(tail));
		string broken = "7 0 obj\n<< /Type /XRef /Size 6 /Root 1 0 R /Prev " + streamObject
			+ " /ByteRange [0 99999999 99999999 0] /Index [0 1] /W [0 0 0] /Length 0 >>\nstream\n\nendstream\nendobj\nstartxref\n" + signed.Length + "\n%%EOF\n";
		return Concat(signed, Encoding.ASCII.GetBytes(broken));
	}

	private static byte[] PdfWithCompressedObject(out int streamObject, out int index)
	{
		streamObject = 4;
		index = 3;
		string body = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
		int catalog = body.IndexOf("1 0 obj", StringComparison.Ordinal);
		var rows = new byte[4 * 5];
		WriteEntry(rows, 0, 0, 0, 65535);
		WriteEntry(rows, 1, 1, catalog, 0);
		WriteEntry(rows, 2, 2, streamObject, index);
		WriteEntry(rows, 3, 1, body.Length, 0);
		string dict = "3 0 obj\n<< /Type /XRef /Size 4 /Root 1 0 R /W [1 2 2] /Length " + rows.Length + " >>\nstream\n";
		string tail = "\nendstream\nendobj\nstartxref\n" + body.Length + "\n%%EOF\n";
		byte[] original = Concat(Encoding.ASCII.GetBytes(body + dict), rows, Encoding.ASCII.GetBytes(tail));
		string broken = "5 0 obj\n<< /Type /XRef /Size 4 /Root 1 0 R /Prev " + body.Length
			+ " /ByteRange [0 99999999 99999999 0] /Index [0 1] /W [0 0 0] /Length 0 >>\nstream\n\nendstream\nendobj\nstartxref\n" + original.Length + "\n%%EOF\n";
		return Concat(original, Encoding.ASCII.GetBytes(broken));
	}

	private static void WriteEntry(byte[] data, int number, int type, int field2, int field3)
	{
		int at = number * 5;
		data[at] = (byte)type;
		data[at + 1] = (byte)((field2 >> 8) & 0xff);
		data[at + 2] = (byte)(field2 & 0xff);
		data[at + 3] = (byte)((field3 >> 8) & 0xff);
		data[at + 4] = (byte)(field3 & 0xff);
	}

	private static byte[] XrefRow(int type, int offset)
	{
		return new[]
		{
			(byte)type,
			(byte)((offset >> 16) & 0xff),
			(byte)((offset >> 8) & 0xff),
			(byte)(offset & 0xff)
		};
	}

	private static byte[] Zlib(byte[] data)
	{
		using (var output = new MemoryStream())
		{
			using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, true))
			{
				zlib.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}
	}

	private static string Offset(int value)
	{
		return value.ToString("0000000000") + " 00000 n \n";
	}

	private static int IndexOf(byte[] data, string text)
	{
		byte[] needle = Encoding.ASCII.GetBytes(text);
		for (int i = 0; i <= data.Length - needle.Length; i++)
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

	private static byte[] Concat(params byte[][] parts)
	{
		int length = 0;
		foreach (byte[] part in parts)
		{
			length += part.Length;
		}
		var output = new byte[length];
		int at = 0;
		foreach (byte[] part in parts)
		{
			Buffer.BlockCopy(part, 0, output, at, part.Length);
			at += part.Length;
		}
		return output;
	}

	private sealed class MemoryFileStorage : IFileStorage
	{
		public System.Collections.Generic.Dictionary<string, string> Resolutions { get; } = new System.Collections.Generic.Dictionary<string, string>();

		public System.Collections.Generic.Dictionary<string, byte[]> Files { get; } = new System.Collections.Generic.Dictionary<string, byte[]>();

		public void AllowResolve(string relative, string fullPath) => Resolutions[relative] = fullPath;

		public bool TryResolve(string relativeName, out string fullPath, out string error)
		{
			if (relativeName != null && Resolutions.TryGetValue(relativeName, out fullPath))
			{
				error = null;
				return true;
			}
			fullPath = null;
			error = "Chemin interdit";
			return false;
		}

		public void WriteAllBytes(string fullPath, byte[] content) => Files[fullPath] = content;

		public bool Exists(string fullPath) => Files.ContainsKey(fullPath);

		public byte[] ReadAllBytes(string fullPath) => Files[fullPath];

		public void Delete(string fullPath) => Files.Remove(fullPath);
	}
}
