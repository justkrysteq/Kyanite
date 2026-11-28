namespace Kyanite.Database.Default.Files;

public record LookupTableModule
{
	public Guid Id { get; set; }
	public required string Name { get; set; }
	public required string Type { get; set; }
	public required string Path { get; set; }
}
