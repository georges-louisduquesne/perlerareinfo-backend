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
	[InlineData("PC_RefContact = 0")]
	public void Rejects_anything_but_one_contact_ref(string where)
	{
		Assert.False(PropertyContactBulkDelete.TryParseContactRef(where, out uint contactRef));
		Assert.Equal(0u, contactRef);
	}
}
