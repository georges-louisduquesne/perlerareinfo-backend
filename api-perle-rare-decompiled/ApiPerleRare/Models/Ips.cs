using System;
using System.Collections.Generic;

namespace ApiPerleRare.Models;

public class Ips
{
	public string IIp { get; set; }

	public DateTime IStart { get; set; }

	public DateTime IStop { get; set; }

	public virtual ICollection<IpsHost> IpsHost { get; set; } = new List<IpsHost>();
}
