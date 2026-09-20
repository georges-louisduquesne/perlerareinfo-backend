using System;
using Xunit;
using ApiPerleRare.Application.Events;

namespace ApiPerleRare.Tests;

public class PropertyContactBulkDeleteTests
{
	[Theory]
	[InlineData("PC_RefContact = 57081", 57081u)]
	[InlineData("PcRefContact eq 12", 12u)]
	[InlineData("pcRefContact=9", 9u)]
	public void Parses_contact_scoped_where(string where, uint expected)
	{
		Assert.True(PropertyContactBulkDelete.TryParseContactRef(where, out uint contactRef));
		Assert.Equal(expected, contactRef);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("PC_Actif = 1")]
	[InlineData("PC_RefContact = 12 OR 1=1")]
	[InlineData("PC_RefContact = 12; DROP TABLE property_contact")]
	[InlineData("PC_PropertyId = 'x'")]
	[InlineData("1=1")]
	[InlineData("PC_RefContact = 0")]
	public void Rejects_anything_but_one_contact_ref(string where)
	{
		Assert.False(PropertyContactBulkDelete.TryParseContactRef(where, out uint contactRef));
		Assert.Equal(0u, contactRef);
	}

	[Fact]
	public void Sql_deletes_only_property_contact_rows_for_one_bound_contact()
	{
		string sql = PropertyContactBulkDelete.DeleteByContactSql;
		Assert.Equal("DELETE FROM `property_contact` WHERE `PC_RefContact` = {0}", sql);
		Assert.DoesNotContain("DROP", sql, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("TRUNCATE", sql, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(" OR ", sql, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(";", sql);
	}

	[Fact]
	public void Detects_mysql_delete_privilege_denied()
	{
		Exception denied = new InvalidOperationException(
			"inner",
			new Exception("DELETE command denied to user 'cursor_client'@'%' for table 'property_contact'"));
		Assert.True(PropertyContactBulkDelete.IsPrivilegeDenied(denied));
		Assert.False(PropertyContactBulkDelete.IsPrivilegeDenied(new Exception("lock wait timeout")));
	}
}
