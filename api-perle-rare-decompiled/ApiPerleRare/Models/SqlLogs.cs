namespace ApiPerleRare.Models;

public class SqlLogs
{
	public int Logid { get; set; }

	public uint? Contactref { get; set; }

	public string Sql { get; set; }

	public float? Duration { get; set; }
}
