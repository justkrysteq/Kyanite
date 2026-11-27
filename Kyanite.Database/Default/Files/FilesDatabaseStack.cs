namespace Kyanite.Database.Default.Files;

public class FilesDatabaseStack : DatabaseStack
{
	const string databaseDirName = "modules";
	string DatabaseDirPath { get; }

	public FilesDatabaseStack() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kyanite", databaseDirName)) { }

	private FilesDatabaseStack(string databaseDirPath) : base(new FilesModuleRepository(databaseDirPath), new FilesDataRepository(databaseDirPath))
	{
		DatabaseDirPath = databaseDirPath;
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
}
