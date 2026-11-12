using System;
using ApiPerleRare.Application.Search;
using Xunit;

namespace ApiPerleRare.Tests;

public class SearchUseCaseErrorTests
{
	[Fact]
	public void User_facing_message_hides_mysql_stack_trace()
	{
		var ex = new Exception("wrapper", new InvalidOperationException("Too many connections"));
		string msg = SearchUseCase.UserFacingSearchError(ex);
		Assert.DoesNotContain("MySqlConnector", msg);
		Assert.DoesNotContain("InvalidOperationException", msg);
		Assert.Contains("saturé", msg);
	}

	[Fact]
	public void Unknown_failure_gets_a_short_generic_message()
	{
		string msg = SearchUseCase.UserFacingSearchError(new InvalidOperationException("column xyzzy"));
		Assert.DoesNotContain("xyzzy", msg);
		Assert.Contains("indisponible", msg);
	}
}
