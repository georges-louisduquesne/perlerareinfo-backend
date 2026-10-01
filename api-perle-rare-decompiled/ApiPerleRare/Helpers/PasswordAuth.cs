namespace ApiPerleRare.Helpers;

/// <summary>
/// Login check for <c>CP_MotDePasse</c>. Never returns a replacement value:
/// a successful login must not rewrite the stored password.
/// </summary>
public static class PasswordAuth
{
	/// <summary>
	/// Returns false if the password does not match.
	/// <paramref name="upgradedHash"/> is always null — callers must not persist a new hash.
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
		return password == stored;
	}
}
