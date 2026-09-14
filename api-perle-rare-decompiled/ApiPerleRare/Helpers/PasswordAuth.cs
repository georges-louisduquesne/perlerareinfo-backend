namespace ApiPerleRare.Helpers;

/// <summary>
/// Soft-migration gate used by <c>UserService.Authenticate</c>:
/// plaintext match → rewrite hash; already hashed → verify only.
/// </summary>
public static class PasswordAuth
{
	/// <summary>
	/// Returns false if the password does not match.
	/// When true and <paramref name="upgradedHash"/> is non-null, caller must persist it to replace plaintext.
	/// </summary>
	public static bool TryAuthenticate(string password, string stored, out string upgradedHash)
	{
		upgradedHash = null;
		if (string.IsNullOrEmpty(password) || stored == null)
		{
			return false;
		}
		if (PasswordHasher.IsHashed(stored))
		{
			return PasswordHasher.Verify(password, stored);
		}
		if (password != stored)
		{
			return false;
		}
		upgradedHash = PasswordHasher.Hash(password);
		return true;
	}
}
