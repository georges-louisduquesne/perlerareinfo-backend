namespace ApiPerleRare.Helpers;

public class ExchangeConnectionString : BaseConnectionString
{
	public string Server { get; set; }

	public string Username { get; set; }

	public string Password { get; set; }

	public string Domain { get; set; }

	public string Sender { get; set; }

	public int Port { get; set; }

	public bool Ssl { get; set; } = false;

	public ExchangeConnectionString(string connectionString)
		: base(connectionString)
	{
	}
}
