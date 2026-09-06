namespace ApiPerleRare.Models;

public class IpsHost
{
	public string IhIp { get; set; }

	public string IhHost { get; set; }

	public bool IhDenied { get; set; }

	public virtual Ips IhIpNavigation { get; set; }
}
