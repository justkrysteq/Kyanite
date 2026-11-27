namespace Kyanite.Database.Default.Files;

public class FilesDataRepository(string databaseDirPath) : IDataRepository
{
	readonly string _databaseDirPath = databaseDirPath;

	public async Task<DataInformation> AddAsync(DataInformation data, Guid moduleId) // NOTE: Implementing a writer might be better in the future
	{
		File.AppendAllText(Path.Combine(_databaseDirPath, $"{moduleId}.md"), $"""
					id: {data.Id} | type: {data.Type} | value: {data.Value}
					""");

		return data;
	}

	public async Task<IEnumerable<DataInformation>> GetAllAsync(Guid moduleId)
	{
		var lines = File.ReadAllLines(Path.Combine(_databaseDirPath, $"{moduleId}.md")).Skip(4).ToArray();

		var data = new List<DataInformation>();

		foreach (var line in lines)
		{
			if (line.TrimStart().StartsWith("id:"))
			{
				var lineSplit = line.Split(": ");

				data.Add(new DataInformation
				{
					Id = lineSplit[1].Split(" | ")[0],
					Type = lineSplit[2].Split(" | ")[0],
					Value = lineSplit[3].Split(" | ")[0],
				});
			}
		}

		return data;
	}

	public async Task<DataInformation> GetSingleAsync(string dataId, Guid moduleId)
	{
		var lines = File.ReadAllLines(Path.Combine(_databaseDirPath, $"{moduleId}.md")).Skip(4).ToArray();

		var module = new DataInformation();

		foreach (var line in lines)
		{
			if (line.TrimStart().StartsWith($"id: {dataId}"))
			{
				var lineSplit = line.Split(": ");

				module.Id = lineSplit[1].Split(" | ")[0];
				module.Type = lineSplit[2].Split(" | ")[0];
				module.Value = lineSplit[3].Split(" | ")[0];
			}
		}

		return module;
	}

	public async Task<DataInformation> UpdateAsync(DataInformation data, Guid moduleId)
	{
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
