using System;

namespace ApiPerleRare.Models;

public class Audit
{
	public int AId { get; set; }

	public DateTime ADate { get; set; }

	public string ALogin { get; set; }

	public string ATable { get; set; }

	public string AType { get; set; }

	public string AValue { get; set; }

	public string AWhere { get; set; }

	public string AIpsource { get; set; }
}
