using System;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ApiPerleRare.Helpers;

/// <summary>
/// HS256 key that signs and verifies with the raw secret bytes, whatever their length.
/// Microsoft.IdentityModel 7.x refuses HMAC keys under 256 bits and ignores the relax switch;
/// the production secret is 17 bytes and the .NET 7 API next to this one uses it raw.
/// </summary>
public static class LegacyHmacKey
{
	public static SymmetricSecurityKey Create(byte[] keyBytes)
	{
		return new SymmetricSecurityKey(keyBytes)
		{
			CryptoProviderFactory = RawHmacCryptoProviderFactory.Instance
		};
	}

	internal static bool IsHmacSha256(string algorithm)
	{
		return algorithm == SecurityAlgorithms.HmacSha256 || algorithm == SecurityAlgorithms.HmacSha256Signature;
	}

	private sealed class RawHmacCryptoProviderFactory : CryptoProviderFactory
	{
		public static readonly RawHmacCryptoProviderFactory Instance = new RawHmacCryptoProviderFactory();

		public override bool IsSupportedAlgorithm(string algorithm, SecurityKey key)
		{
			return IsHmacSha256(algorithm) && key is SymmetricSecurityKey;
		}

		public override SignatureProvider CreateForSigning(SecurityKey key, string algorithm)
		{
			return Create(key, algorithm, willCreateSignatures: true);
		}

		public override SignatureProvider CreateForVerifying(SecurityKey key, string algorithm)
		{
			return Create(key, algorithm, willCreateSignatures: false);
		}

		public override void ReleaseSignatureProvider(SignatureProvider signatureProvider)
		{
			signatureProvider?.Dispose();
		}

		private static SignatureProvider Create(SecurityKey key, string algorithm, bool willCreateSignatures)
		{
			if (key is not SymmetricSecurityKey symmetric || !IsHmacSha256(algorithm))
			{
				throw new NotSupportedException($"Only HS256 with a symmetric key is supported (got '{algorithm}').");
			}
			return new RawHmacSha256SignatureProvider(symmetric, algorithm, willCreateSignatures);
		}
	}

	private sealed class RawHmacSha256SignatureProvider : SignatureProvider
	{
		private readonly byte[] _key;

		public RawHmacSha256SignatureProvider(SymmetricSecurityKey key, string algorithm, bool willCreateSignatures)
			: base(key, algorithm)
		{
			_key = key.Key;
			WillCreateSignatures = willCreateSignatures;
		}

		public override byte[] Sign(byte[] input)
		{
			return HMACSHA256.HashData(_key, input);
		}

		public override bool Verify(byte[] input, byte[] signature)
		{
			return CryptographicOperations.FixedTimeEquals(Sign(input), signature);
		}

		public override bool Verify(byte[] input, int inputOffset, int inputLength, byte[] signature, int signatureOffset, int signatureLength)
		{
			byte[] expected = HMACSHA256.HashData(_key, input.AsSpan(inputOffset, inputLength));
			return CryptographicOperations.FixedTimeEquals(expected, signature.AsSpan(signatureOffset, signatureLength));
		}

		protected override void Dispose(bool disposing)
		{
		}
	}
}
