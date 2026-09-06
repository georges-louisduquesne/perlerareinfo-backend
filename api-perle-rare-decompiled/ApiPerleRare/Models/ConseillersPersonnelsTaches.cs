using System;

namespace ApiPerleRare.Models;

public class ConseillersPersonnelsTaches
{
	public uint CptRef { get; set; }

	public DateOnly CptDateCreation { get; set; }

	public uint CptRefConseiller { get; set; }

	public string CptType { get; set; }

	public string CptQui { get; set; }

	public string CptCom { get; set; }

	public string CptEtat { get; set; }

	public DateOnly CptDateRealisation { get; set; }

	public string CptLien { get; set; }

	public virtual ConseillersPersonnels CptRefConseillerNavigation { get; set; }
}
