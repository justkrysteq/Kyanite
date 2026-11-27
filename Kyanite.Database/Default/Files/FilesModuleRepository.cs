namespace Kyanite.Database.Default.Files;

public class FilesModuleRepository(string databaseDirPath) : IModuleRepository
{
	readonly string _databaseDirPath = CreateDirectory(databaseDirPath);

	public async Task<ModuleInformation> AddAsync(ModuleInformation module)
	{
		var moduleWithId = module with { Id = Guid.NewGuid() };
		var frontMatter = $"""
			---
			type: {module.Type}
			name: {module.Name}
			---
			""" + Environment.NewLine;

		var filePath = Path.Combine(_databaseDirPath, $"{moduleWithId.Id}.md");

		await File.WriteAllTextAsync(filePath, frontMatter);

		return moduleWithId;
	}

	public async Task<IEnumerable<ModuleInformation>> GetAllAsync()
	{
		var modulePaths = Directory.GetFiles(_databaseDirPath, "*.md");

		var modules = new List<ModuleInformation>();

		foreach (var modulePath in modulePaths)
		{
			var content = File.ReadLines(modulePath).Take(4).ToArray();

			var frontMatter = GetFrontMatter(content);
			frontMatter.TryGetValue("name", out var name);
			frontMatter.TryGetValue("type", out var type);

			modules.Add(new ModuleInformation
			{
				Id = Guid.Parse(Path.GetFileNameWithoutExtension(modulePath)),
				Name = name ?? "Error reading file",
				Type = type ?? "Note",
			});
		}

		return modules;
	}

	public async Task<ModuleInformation> GetSingleAsync(Guid id)
	{
		// NOTE: What if a file does not exist?

		var content = File.ReadLines(Path.Combine(_databaseDirPath, $"{id}.md")).Take(4).ToArray();

		var frontMatter = GetFrontMatter(content);
		frontMatter.TryGetValue("name", out var name);
		frontMatter.TryGetValue("type", out var type);

		return new ModuleInformation
		{
			Id = id,
			Name = name ?? "Error reading file",
			Type = type ?? "Note",
		};
	}

	public async Task<ModuleInformation> UpdateAsync(ModuleInformation module)
	{
		var content = File.ReadLines(Path.Combine(_databaseDirPath, $"{module.Id}.md")).Take(4).ToArray();

		SetFrontMatterParam(content, "name", module.Name);
		SetFrontMatterParam(content, "type", module.Type);

		return module;
	}

	public async Task DeleteAsync(Guid id)
	{
		File.Delete(Path.Combine(_databaseDirPath, $"{id}.md"));
	}

	static string CreateDirectory(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}

	Dictionary<string, string> GetFrontMatter(string[] markdownLines)
	{
		var result = new Dictionary<string, string>();

		if (markdownLines[0].Trim() != "---")
			return result;

		for (int i = 1; i < markdownLines.Length; i++)
		{
			var line = markdownLines[i].Trim();

			if (line == "---")
				break;

			if (line.Length == 0)
				continue;

			int colon = line.IndexOf(':');
			if (colon <= 0)
				continue;

			string key = line[..colon].Trim();
			string value = line[(colon + 1)..].Trim();

			result[key] = value;
		}

		return result;
	}

	void SetFrontMatterParam(string[] markdownLines, string key, string value)
	{
		if (markdownLines == null || markdownLines.Length == 0 ||
			markdownLines[0].Trim() != "---")
			return;

		for (int i = 1; i < markdownLines.Length; i++)
		{
			var line = markdownLines[i].Trim();

			if (line == "---")
				return;

			if (line.Length == 0)
				continue;

			int colon = line.IndexOf(':');
			if (colon <= 0)
				continue;

			string existingKey = line[..colon].Trim();

			if (string.Equals(existingKey, key))
			{
				markdownLines[i] = line[..(colon + 1)] + " " + value;
				return;
			}
		}
	}
}
