using System;

namespace ApiPerleRare.Models;

public class ProxyAnonyme
{
	public string Url { get; set; }

	public int Port { get; set; }

	public float CrawlingTime { get; set; }

	public int Tentatives { get; set; }

	public int Reussites { get; set; }

	public int Echecs { get; set; }

	public int Parserror { get; set; }

	public DateTime Age { get; set; }
}
