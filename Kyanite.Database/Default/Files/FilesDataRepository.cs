namespace Kyanite.Database.Default.Files;

public class FilesDataRepository(string databaseDirPath) : IDataRepository
{
	readonly string _databaseDirPath = databaseDirPath;

	public async Task<DataInformation> AddAsync(DataInformation data, Guid moduleId) // NOTE: Implementing a writer might be better in the future
	{
		// TODO: this works differently for different modules, cuz this adds "new attributes", like id = _text for the text in Note module, and _settings for specific settings in todo module
		// figure out how to handle that, Note modules should only contain the value of id = _text
		// in json modules the underscore should be stripped and the value of id is equal to the option name in json, example: _settings becomes "settings": null

		// File.AppendAllText(Path.Combine(_databaseDirPath, $"{moduleId}.md"), $"""
		// 			id: {data.Id} | type: {data.Type} | value: {data.Value}
		// 			""");

		return data;
	}

	public async Task<IEnumerable<DataInformation>> GetAllAsync(Guid moduleId)
	{
		// TODO: if type == Note returns an array with: id = _text, type = string, value = file contents
		// else checks the json for options

		// var lines = File.ReadAllLines(Path.Combine(_databaseDirPath, $"{moduleId}.md")).Skip(4).ToArray();
		//
		var data = new List<DataInformation>();
		//
		// foreach (var line in lines)
		// {
		// 	if (line.TrimStart().StartsWith("id:"))
		// 	{
		// 		var lineSplit = line.Split(": ");
		//
		// 		data.Add(new DataInformation
		// 		{
		// 			Id = lineSplit[1].Split(" | ")[0],
		// 			Type = lineSplit[2].Split(" | ")[0],
		// 			Value = lineSplit[3].Split(" | ")[0],
		// 		});
		// 	}
		// }

		return data;
	}

	public async Task<DataInformation> GetSingleAsync(string dataId, Guid moduleId)
	{
		// TODO: same as above, just for the specific dataId

		// var lines = File.ReadAllLines(Path.Combine(_databaseDirPath, $"{moduleId}.md")).Skip(4).ToArray();

		var module = new DataInformation();

		// foreach (var line in lines)
		// {
		// 	if (line.TrimStart().StartsWith($"id: {dataId}"))
		// 	{
		// 		var lineSplit = line.Split(": ");
		//
		// 		module.Id = lineSplit[1].Split(" | ")[0];
		// 		module.Type = lineSplit[2].Split(" | ")[0];
		// 		module.Value = lineSplit[3].Split(" | ")[0];
		// 	}
		// }

		return module;
	}

	public async Task<DataInformation> UpdateAsync(DataInformation data, Guid moduleId)
	{
		// TODO: for notes, override the file with data.Value, else check for id in json and update that

		var filePath = Path.Combine(_databaseDirPath, $"{moduleId}.md");
		var lines = File.ReadAllLines(filePath).ToList();

		var lineNumber = 0;

		foreach (var line in lines)
		{
			if (line.TrimStart().StartsWith($"id: {data.Id}"))
			{
				break;
			}

			lineNumber++;
		}

		lines[lineNumber] = $"id: {data.Id} | type: {data.Type} | value: {data.Value}";

		await File.WriteAllLinesAsync(filePath, lines);

		return data;
	}

	public async Task DeleteAsync(string dataId, Guid moduleId)
	{
		// TODO: Just like above but delete instead of replace

		var filePath = Path.Combine(_databaseDirPath, $"{moduleId}.md");
		var lines = File.ReadAllLines(filePath).ToList();

		var lineNumber = 0;

		foreach (var line in lines)
		{
			if (line.TrimStart().StartsWith($"id: {dataId}"))
			{
				break;
			}

			lineNumber++;
		}

		lines.RemoveAt(lineNumber);

		await File.WriteAllLinesAsync(filePath, lines);
	}
}
