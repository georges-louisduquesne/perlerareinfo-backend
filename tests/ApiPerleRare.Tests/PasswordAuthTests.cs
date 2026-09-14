using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class PasswordAuthTests
{
	[Fact]
	public void Plaintext_match_returns_upgraded_hash()
	{
		Assert.True(PasswordAuth.TryAuthenticate("secret", "secret", out string upgraded));
		Assert.NotNull(upgraded);
		Assert.True(PasswordHasher.IsHashed(upgraded));
		Assert.True(PasswordHasher.Verify("secret", upgraded));
	}

	[Fact]
	public void Plaintext_mismatch_fails()
	{
		Assert.False(PasswordAuth.TryAuthenticate("wrong", "secret", out string upgraded));
		Assert.Null(upgraded);
	}

	[Fact]
	public void Hashed_match_does_not_request_rewrite()
	{
		string hash = PasswordHasher.Hash("secret");
		Assert.True(PasswordAuth.TryAuthenticate("secret", hash, out string upgraded));
		Assert.Null(upgraded);
	}

	[Fact]
	public void Hashed_mismatch_fails()
	{
		string hash = PasswordHasher.Hash("secret");
		Assert.False(PasswordAuth.TryAuthenticate("wrong", hash, out string upgraded));
		Assert.Null(upgraded);
	}
}
