namespace Kyanite.Database.Default.Files;

public class FilesModuleRepository(string databaseDirPath) : IModuleRepository
{
	readonly string _databaseDirPath = CreateDirectory(databaseDirPath);

	public async Task<ModuleInformation> AddAsync(ModuleInformation module)
	{
		var moduleWithId = module with { Id = Guid.NewGuid() };

		var filename = Sanitize(moduleWithId.Name);
		if (module.Type == "Note")
			filename += ".md";
		else
			filename += ".json";

		var path = Path.Combine(_databaseDirPath, filename);

		File.Create(path).Dispose();

		// TODO:
		// add to lookup-table.json, example looks like this: { "modules": [ { "id": "<guid>", "name": "whatever", "path": "whatever.json", "type": "ToDoList" } ] }
		// for todo, if settings, override is not checked keep as null
		// adding to lookup table should be in a helper function

		return moduleWithId;
	}

	public async Task<IEnumerable<ModuleInformation>> GetAllAsync()
	{
		var modulePaths = Directory.GetFiles(_databaseDirPath, "*.md"); // TODO: markdown or json

		var modules = new List<ModuleInformation>();

		foreach (var modulePath in modulePaths)
		{
			// TODO: handle md files and json files separately

			// modules.Add(new ModuleInformation
			// {
			// 	Id = Guid.Parse(Path.GetFileNameWithoutExtension(modulePath)),
			// 	Name = name ?? "Error reading file",
			// 	Type = type ?? "Note",
			// });
		}

		return modules;
	}

	public async Task<ModuleInformation> GetSingleAsync(Guid id)
	{
		// NOTE: What if a file does not exist?

		// TODO: check lookup-table and return module info from it
		// if file exists and is not in lookup table, add to lookup table

		// return new ModuleInformation
		// {
		// 	Id = id,
		// 	Name = name ?? "Error reading file",
		// 	Type = type ?? "Note",
		// };
	}

	public async Task<ModuleInformation> UpdateAsync(ModuleInformation module)
	{
		// TODO: change in lookup-table, that should also be a helper function

		return module;
	}

	public async Task DeleteAsync(Guid id)
	{
		// TODO: remove from lookup-table and remove file

		// File.Delete(Path.Combine(_databaseDirPath, $"{id}.md"));
	}

	static string CreateDirectory(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}
}
