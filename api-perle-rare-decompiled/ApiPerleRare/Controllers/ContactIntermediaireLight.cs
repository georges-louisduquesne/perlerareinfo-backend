using System.Runtime.Serialization;

namespace ApiPerleRare.Controllers;

[DataContract]
public class ContactIntermediaireLight
{
	[DataMember]
	public string CStatut { get; set; }

	[DataMember]
	public string CTel { get; set; }

	[DataMember]
	public string CNom { get; set; }

	[DataMember]
	public string CPrenom { get; set; }

	[DataMember]
	public string CQualiteRelation { get; set; }

	[DataMember]
	public uint CRefCtcInter { get; set; }

	[DataMember]
	public uint IRefIntermediaire { get; set; }

	[DataMember]
	public string INomIntermediaire { get; set; }

	[DataMember]
	public sbyte IActif { get; set; }

	[DataMember]
	public string ITelephone { get; set; }

	[DataMember]
	public string ITelephone2 { get; set; }

	[DataMember]
	public string CMel { get; set; }

	[DataMember]
	public string CCom { get; set; }
}
