namespace ApiPerleRare.YanportModels;

public class Geometry
{
	public float? Surface { get; set; }

	public byte? RoomCount { get; set; }

	public AreaCount AreaCount { get; set; }

	public Floor[] Floors { get; set; }

	public byte? FloorCount { get; set; }
}
