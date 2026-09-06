using System;

namespace ApiPerleRare.Models;

public class Stats
{
	public DateOnly SDate { get; set; }

	public int SClientsActifs { get; set; }

	public int SClientsRechActiv { get; set; }

	public int SClientsRechSusp { get; set; }

	public int SClientsAttenteSig { get; set; }

	public sbyte SFluxClientsActifs { get; set; }

	public sbyte SFluxClientsMorts { get; set; }

	public int SProspectsActifs { get; set; }

	public sbyte SFluxProspectsActifs { get; set; }

	public sbyte SFluxProspectsMorts { get; set; }

	public int SFluxEventNewRvSeul { get; set; }

	public int SFluxEventNewRvClient { get; set; }

	public int SFluxEventNewCrVisite { get; set; }

	public int SFluxEventRvSeul { get; set; }

	public int SFluxEventRvClient { get; set; }

	public int SFluxEventCrVisite { get; set; }

	public short SFluxAnnonces { get; set; }
}
