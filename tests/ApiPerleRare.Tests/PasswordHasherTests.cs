using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class PasswordHasherTests
{
	[Fact]
	public void Hash_is_prefixed_and_not_plaintext()
	{
		string hash = PasswordHasher.Hash("secret");
		Assert.StartsWith(PasswordHasher.Prefix, hash);
		Assert.NotEqual("secret", hash);
		Assert.True(hash.Length > 15);
		Assert.True(hash.Length <= 255);
	}

	[Fact]
	public void Verify_accepts_correct_password()
	{
		string hash = PasswordHasher.Hash("correct-horse");
		Assert.True(PasswordHasher.Verify("correct-horse", hash));
	}

	[Fact]
	public void Verify_rejects_wrong_password()
	{
		string hash = PasswordHasher.Hash("correct-horse");
		Assert.False(PasswordHasher.Verify("wrong", hash));
	}

	[Fact]
	public void IsHashed_detects_format()
	{
		Assert.False(PasswordHasher.IsHashed("plain15chars!!"));
		Assert.False(PasswordHasher.IsHashed(null));
		Assert.True(PasswordHasher.IsHashed(PasswordHasher.Hash("x")));
	}

	[Fact]
	public void Hash_uses_unique_salt()
	{
		string a = PasswordHasher.Hash("same");
		string b = PasswordHasher.Hash("same");
		Assert.NotEqual(a, b);
		Assert.True(PasswordHasher.Verify("same", a));
		Assert.True(PasswordHasher.Verify("same", b));
	}
}
