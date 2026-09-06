using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class TypeTransactionPropertyFilter : AbstractPropertyFilter
{
	private readonly bool? _isAchat;

	public TypeTransactionPropertyFilter(bool? isAchat)
	{
		_isAchat = isAchat;
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_isAchat.HasValue)
		{
			search.TypeTransaction = (_isAchat.Value ? "A" : "L");
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (!_isAchat.HasValue)
		{
			return "Type de transaction obligatoire !";
		}
		return (property.PTypeTransaction == (_isAchat.Value ? "A" : "L")) ? null : $"Type de transaction différente : {_isAchat.Value} <> {property.PTypeTransaction}";
	}
}
