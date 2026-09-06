namespace ApiPerleRare.Tasks;

public abstract class AbstractTask
{
	public abstract string Description { get; }

	public abstract void Run();
}
