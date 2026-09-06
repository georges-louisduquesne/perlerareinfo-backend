using System;

namespace ApiPerleRare.RecupInfos;

public static class TypeBienEx
{
	public static string GetStringValue(this TypeBien tb)
	{
		return tb switch
		{
			TypeBien.Appartement => "Appartement", 
			TypeBien.Maison => "Maison", 
			TypeBien.LocauxPro => "Locaux Pro", 
			_ => throw new NotImplementedException(), 
		};
	}
}
