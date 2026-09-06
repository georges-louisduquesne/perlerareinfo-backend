namespace ApiPerleRare.YanportModels;

public class Features
{
	public Descriptive Descriptive { get; set; }

	public AdditionalFeature[] AdditionalFeatures { get; set; }

	public Geometry Geometry { get; set; }

	public Visual Visual { get; set; }

	public Construction Construction { get; set; }

	public Energy Energy { get; set; }
}
