namespace ApiPerleRare.Application.Events;

/// <summary>
/// Home « en attente d'encaissement »: no HON/PS invoice issued yet.
/// Live CRM stores empty invoice refs as 0 as well as NULL.
/// </summary>
public static class EncaissementsEnCoursFilter
{
	public static bool IsWaitingForPayment(int? factureHon, int? facturePs)
	{
		return IsUnissued(factureHon) && IsUnissued(facturePs);
	}

	public static bool IsUnissued(int? invoiceRef)
	{
		return !invoiceRef.HasValue || invoiceRef.Value == 0;
	}
}
