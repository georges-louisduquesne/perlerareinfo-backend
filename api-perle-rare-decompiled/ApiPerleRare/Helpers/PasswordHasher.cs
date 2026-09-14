using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ApiPerleRare.Helpers;

/// <summary>
/// Soft-migration password storage for <c>CP_MotDePasse</c>.
/// Plaintext values (legacy) are rewritten on successful login to a prefixed PBKDF2 string.
/// Format: <c>$pbkdf2-sha256${iterations}${salt_b64url}${hash_b64url}</c> (~90 chars → needs varchar ≥ 128).
/// </summary>
public static class PasswordHasher
{
	public const string Prefix = "$pbkdf2-sha256$";
	public const int Iterations = 100_000;
	private const int SaltSize = 16;
	private const int HashSize = 32;

	public static bool IsHashed(string stored)
	{
		return !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);
	}

	public static string Hash(string password)
	{
		if (password == null)
		{
			throw new ArgumentNullException(nameof(password));
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(password));
		}

		byte[] salt = new byte[SaltSize];
		RandomNumberGenerator.Fill(salt);
		byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, HashSize);
		return Prefix + Iterations + "$" + ToBase64Url(salt) + "$" + ToBase64Url(hash);
	}

	public static bool Verify(string password, string stored)
	{
		if (password == null || string.IsNullOrEmpty(stored) || !IsHashed(stored))
		{
			return false;
		}

		// $pbkdf2-sha256$iterations$salt$hash
		string[] parts = stored.Split('$', StringSplitOptions.None);
		// "", "pbkdf2-sha256", iterations, salt, hash
		if (parts.Length != 5 || parts[1] != "pbkdf2-sha256")
		{
			return false;
		}
		if (!int.TryParse(parts[2], out int iterations) || iterations < 1)
		{
			return false;
		}

		byte[] salt;
		byte[] expected;
		try
		{
			salt = FromBase64Url(parts[3]);
			expected = FromBase64Url(parts[4]);
		}
		catch (FormatException)
		{
			return false;
		}

		if (salt.Length == 0 || expected.Length == 0)
		{
			return false;
		}

		byte[] actual = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, expected.Length);
		return CryptographicOperations.FixedTimeEquals(actual, expected);
	}

	private static string ToBase64Url(byte[] data)
	{
		return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	private static byte[] FromBase64Url(string value)
	{
		string padded = value.Replace('-', '+').Replace('_', '/');
		switch (padded.Length % 4)
		{
			case 2:
				padded += "==";
				break;
			case 3:
				padded += "=";
				break;
		}
		return Convert.FromBase64String(padded);
	}
}
