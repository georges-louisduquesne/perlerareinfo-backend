using System;

namespace ApiPerleRare.Helpers;

[AttributeUsage(AttributeTargets.Property)]
public class AliasAttribute : Attribute
{
	public string Alias { get; set; }

	public AliasAttribute(string aliasName)
	{
		Alias = aliasName;
	}
}
