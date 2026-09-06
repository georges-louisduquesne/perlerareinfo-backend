using System.Runtime.Serialization;

namespace ApiPerleRare.Helpers;

[DataContract]
public class SelectResult<T>
{
	[DataMember]
	public int Total { get; set; }

	[DataMember]
	public T[] Items { get; set; }
}
