namespace Kyanite.Database.Default.Files;

public class FilesDatabaseStack
	: DatabaseStack
{
	string databaseDirPath;

	const string databaseDirName = "modules";

	public FilesDatabaseStack() : base(new FilesModuleRepository(), new FilesDataRepository())
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		databaseDirPath = Path.Combine(localAppData, databaseDirName);
	}

	public override string FriendlyName => "Files";

	public override async Task<bool> Prepare(Dictionary<string, string> data)
	{
		((FilesModuleRepository)ModuleRepository).Initialize();
		((FilesDataRepository)DataRepository).Initialize();

		return true;
	}

	public override async void OnRemoved()
	{
		Directory.Delete(databaseDirPath);
	}

	public override DatabaseInitializationViewModelBase CreateInitializationViewModel()
		=> new FilesInitializationViewModel();
}
