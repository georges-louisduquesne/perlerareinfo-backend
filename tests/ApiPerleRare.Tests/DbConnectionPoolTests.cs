using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class DbConnectionPoolTests
{
	[Fact]
	public void Absent_limits_are_appended()
	{
		string cs = DbConnectionPool.Apply("Server=127.0.0.1;Uid=app;Pwd=secret", "15", "20");

		Assert.Equal("Server=127.0.0.1;Uid=app;Pwd=secret;Maximum Pool Size=15;Connection Idle Timeout=20", cs);
	}

	[Fact]
	public void Existing_limits_are_replaced_without_touching_the_password()
	{
		string cs = DbConnectionPool.Apply(
			"Server=127.0.0.1;Pwd=a=b;Maximum Pool Size=100;Connection Idle Timeout=180",
			"25",
			"20");

		Assert.Equal("Server=127.0.0.1;Pwd=a=b;Maximum Pool Size=25;Connection Idle Timeout=20", cs);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData(" ", "0")]
	[InlineData("abc", "-1")]
	public void Missing_or_invalid_limits_leave_the_string_unchanged(string maxPool, string idleSeconds)
	{
		const string cs = "Server=127.0.0.1;Uid=app";

		Assert.Equal(cs, DbConnectionPool.Apply(cs, maxPool, idleSeconds));
	}

	[Fact]
	public void Empty_connection_string_stays_empty()
	{
		Assert.Equal("", DbConnectionPool.Apply("", "15", "20"));
		Assert.Null(DbConnectionPool.Apply(null, "15", "20"));
	}
}
