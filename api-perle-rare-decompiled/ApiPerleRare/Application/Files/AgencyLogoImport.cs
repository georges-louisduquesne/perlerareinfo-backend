using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Files;

public sealed class AgencyLogoImportRequest
{
	public string Url { get; set; }
}

public sealed class AgencyLogoImportResult
{
	public string ILogo { get; set; }
}

/// <summary>
/// Télécharge une image publique et l’enregistre sous logos/agences/{id}.ext.
/// </summary>
public sealed class ImportAgencyLogoUseCase
{
	public const int MaxBytes = 2_000_000;

	private readonly ApplicationDbContext _db;

	private readonly IFileStorage _storage;

	public ImportAgencyLogoUseCase(ApplicationDbContext db, IFileStorage storage)
	{
		_db = db;
		_storage = storage;
	}

	public async Task<AgencyLogoImportResult> Execute(uint agencyId, string rawUrl, CancellationToken cancellationToken)
	{
		if (!AgencyLogoAddress.TryCreate(rawUrl, out Uri uri, out string urlError))
		{
			throw new InvalidOperationException(urlError);
		}
		byte[] bytes = await AgencyLogoAddress.DownloadAsync(uri, cancellationToken);
		if (!AgencyLogoAddress.TryDetect(bytes, out string extension, out string detectError))
		{
			throw new InvalidOperationException(detectError);
		}
		string relative = "logos/agences/" + agencyId + "." + extension;
		if (!_storage.TryResolve(relative, out string fullPath, out string pathError))
		{
			throw new InvalidOperationException(pathError ?? "Chemin de logo invalide");
		}
		IntermediairesDirects row = await _db.IntermediairesDirects
			.FirstOrDefaultAsync((IntermediairesDirects item) => item.IRefIntermediaire == agencyId, cancellationToken);
		if (row == null)
		{
			throw new InvalidOperationException("Agence introuvable");
		}
		_storage.WriteAllBytes(fullPath, bytes);
		row.ILogo = relative;
		await _db.SaveChangesAsync(cancellationToken);
		return new AgencyLogoImportResult { ILogo = relative };
	}
}

internal static class AgencyLogoAddress
{
	private static readonly Regex SvgDanger = new Regex(
		@"<script|</script|javascript:|data:\s*text/html|foreignObject|\son[a-z]+\s*=",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	public static bool TryCreate(string raw, out Uri uri, out string error)
	{
		uri = null;
		error = null;
		string value = (raw ?? "").Trim();
		if (value.Length == 0 || value.Length > 2000 || !Uri.TryCreate(value, UriKind.Absolute, out uri))
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
		{
			error = "Collez une URL http ou https.";
			return false;
		}
		if (uri.Port != 80 && uri.Port != 443 && uri.Port != -1)
		{
			error = "Adresse d’image refusée.";
			return false;
		}
		if (IsBlockedHost(uri.Host))
		{
			error = "Adresse d’image refusée.";
			return false;
		}
		return true;
	}

	public static async Task<byte[]> DownloadAsync(Uri start, CancellationToken cancellationToken)
	{
		using HttpClientHandler handler = new HttpClientHandler
		{
			AllowAutoRedirect = false,
			AutomaticDecompression = DecompressionMethods.All
		};
		using HttpClient client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(20)
		};
		client.DefaultRequestHeaders.UserAgent.ParseAdd("PerleRareLogoImport/1.0");
		Uri current = start;
		for (int hop = 0; hop < 4; hop++)
		{
			await EnsurePublicHostAsync(current, cancellationToken);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, current);
			using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
			{
				Uri next = response.Headers.Location;
				if (next == null)
				{
					throw new InvalidOperationException("Impossible de télécharger l’image.");
				}
				if (!next.IsAbsoluteUri)
				{
					next = new Uri(current, next);
				}
				if (!TryCreate(next.AbsoluteUri, out Uri safe, out string error))
				{
					throw new InvalidOperationException(error);
				}
				current = safe;
				continue;
			}
			if (!response.IsSuccessStatusCode)
			{
				throw new InvalidOperationException("Impossible de télécharger l’image.");
			}
			byte[] bytes = await ReadLimitedAsync(response, cancellationToken);
			if (bytes.Length == 0)
			{
				throw new InvalidOperationException("Le fichier n’est pas une image.");
			}
			return bytes;
		}
		throw new InvalidOperationException("Impossible de télécharger l’image.");
	}

	public static bool TryDetect(byte[] bytes, out string extension, out string error)
	{
		extension = null;
		error = null;
		if (bytes == null || bytes.Length < 8)
		{
			error = "Le fichier n’est pas une image.";
			return false;
		}
		if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
		{
			extension = "png";
			return true;
		}
		if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
		{
			extension = "jpg";
			return true;
		}
		if (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F')
		{
			extension = "gif";
			return true;
		}
		if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
			&& bytes.Length > 12
			&& bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
		{
			extension = "webp";
			return true;
		}
		string text = Encoding.UTF8.GetString(bytes);
		string trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
		if (trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
			|| (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && text.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0))
		{
			if (SvgDanger.IsMatch(text))
			{
				error = "Ce fichier SVG n’est pas accepté.";
				return false;
			}
			extension = "svg";
			return true;
		}
		error = "Le fichier n’est pas une image.";
		return false;
	}

	public static bool TryPublicFileName(string fileName, out string relative, out string contentType)
	{
		relative = null;
		contentType = null;
		string name = (fileName ?? "").Trim();
		if (!Regex.IsMatch(name, @"^\d+\.(png|jpg|jpeg|gif|webp|svg)$", RegexOptions.IgnoreCase))
		{
			return false;
		}
		string ext = name.Substring(name.LastIndexOf('.') + 1).ToLowerInvariant();
		if (ext == "jpeg")
		{
			ext = "jpg";
		}
		relative = "logos/agences/" + PathId(name) + "." + ext;
		contentType = ext switch
		{
			"png" => "image/png",
			"jpg" => "image/jpeg",
			"gif" => "image/gif",
			"webp" => "image/webp",
			"svg" => "image/svg+xml",
			_ => null
		};
		return contentType != null;
	}

	private static string PathId(string fileName)
	{
		int dot = fileName.LastIndexOf('.');
		return fileName.Substring(0, dot);
	}

	private static async Task EnsurePublicHostAsync(Uri uri, CancellationToken cancellationToken)
	{
		IPAddress[] addresses;
		try
		{
			addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, cancellationToken);
		}
		catch (SocketException)
		{
			throw new InvalidOperationException("Impossible de télécharger l’image.");
		}
		if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
		{
			throw new InvalidOperationException("Adresse d’image refusée.");
		}
	}

	private static async Task<byte[]> ReadLimitedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		long? length = response.Content.Headers.ContentLength;
		if (length.HasValue && length.Value > MaxDownload)
		{
			throw new InvalidOperationException("Image trop volumineuse (2 Mo maximum).");
		}
		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		byte[] buffer = new byte[81920];
		using var output = new System.IO.MemoryStream();
		int read;
		while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
		{
			if (output.Length + read > MaxDownload)
			{
				throw new InvalidOperationException("Image trop volumineuse (2 Mo maximum).");
			}
			output.Write(buffer, 0, read);
		}
		return output.ToArray();
	}

	private const int MaxDownload = ImportAgencyLogoUseCase.MaxBytes;

	private static bool IsBlockedHost(string host)
	{
		string name = (host ?? "").Trim().TrimEnd('.').ToLowerInvariant();
		if (name.Length == 0 || name == "localhost" || name.EndsWith(".localhost") || name.EndsWith(".local"))
		{
			return true;
		}
		if (name == "metadata.google.internal" || name == "metadata.google")
		{
			return true;
		}
		if (IPAddress.TryParse(name, out IPAddress ip))
		{
			return IsPrivateAddress(ip);
		}
		return false;
	}

	private static bool IsPrivateAddress(IPAddress ip)
	{
		if (ip == null || IPAddress.IsLoopback(ip))
		{
			return true;
		}
		if (ip.IsIPv4MappedToIPv6)
		{
			ip = ip.MapToIPv4();
		}
		if (ip.AddressFamily == AddressFamily.InterNetworkV6)
		{
			return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
		}
		byte[] b = ip.GetAddressBytes();
		if (b.Length != 4)
		{
			return true;
		}
		if (b[0] == 0 || b[0] == 10 || b[0] == 127)
		{
			return true;
		}
		if (b[0] == 169 && b[1] == 254)
		{
			return true;
		}
		if (b[0] == 192 && b[1] == 168)
		{
			return true;
		}
		if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
		{
			return true;
		}
		if (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
		{
			return true;
		}
		return false;
	}
}
