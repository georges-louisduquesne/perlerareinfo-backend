using System;
using System.Linq;

namespace ApiPerleRare.YanportModels;

public class Ad
{
	public static readonly string[] BadCrawls = new string[5] { "EXPLORIMMO_NEUF", "LE_BON_COIN_NEUF", "LOGICIMMO_NEUF", "SELOGER_NEUF", "IMMONEUF" };

	public Guid Id { get; set; }

	public string CrawlSource { get; set; }

	public string Url { get; set; }

	public bool MustBeIgnored => BadCrawls.Contains(CrawlSource ?? "");
}
