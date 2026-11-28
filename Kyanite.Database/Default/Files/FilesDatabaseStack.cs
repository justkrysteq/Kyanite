using static Kyanite.Core.KyanitePaths;
using System.Text.Json;

namespace Kyanite.Database.Default.Files;

public class FilesDatabaseStack : DatabaseStack
{
	string DatabaseDirPath { get; }

	public FilesDatabaseStack() : this(Path.Combine(KyaniteDataDir, "modules")) { }

	private FilesDatabaseStack(string databaseDirPath) : base(new FilesModuleRepository(databaseDirPath), new FilesDataRepository(databaseDirPath))
	{
		DatabaseDirPath = databaseDirPath;

		var lookupTablePath = Path.Combine(KyaniteDataDir, "lookup-table.json");

		if (!File.Exists(lookupTablePath))
		{
			CreateLookupTable(lookupTablePath, databaseDirPath);
		}
		// TODO: should handle a case when a new file is added
		// check if every module is in the lookup-table, if not add
		else { }
	}

	public override string FriendlyName => "Files";

	public override async Task<bool> Prepare(Dictionary<string, string> data)
	{
		return true;
	}

	public override async void OnRemoved()
	{
		Directory.Delete(DatabaseDirPath);
	}

	public override DatabaseInitializationViewModelBase CreateInitializationViewModel()
		=> new FilesInitializationViewModel();

	// TODO: public static function to sanitize path

	void CreateLookupTable(string lookupTablePath, string databaseDirPath)
	{
		var modules = GetAllModulesNoLookup(databaseDirPath);

		var lookupTable = new { modules };

		File.WriteAllText(lookupTablePath, JsonSerializer.Serialize(lookupTable));
	}

	List<LookupTableModule> GetAllModulesNoLookup(string databaseDirPath)
	{
		var files = Directory.GetFiles(databaseDirPath);

		var modules = new List<LookupTableModule>();

		foreach (var file in files)
		{
			string? type = null;

			if (Path.GetFileName(file).EndsWith(".md"))
			{
				type = "Note";
			}
			else if (Path.GetFileName(file).EndsWith(".json"))
			{
				string json = File.ReadAllText(file);

				using JsonDocument doc = JsonDocument.Parse(json);
				type = doc.RootElement.GetProperty("type").GetString();
			}

			if (type == null)
				continue;

			modules.Add(new LookupTableModule
			{
				Id = new Guid(),
				Name = Path.GetFileNameWithoutExtension(file),
				Type = type,
				Path = file
			});
		}

		return modules;
	}
}
