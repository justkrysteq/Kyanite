namespace Kyanite.Modules;

/// <summary>
/// reads and writes module files below one directory
/// </summary>
public class ModuleStore(string rootDirectory)
{
	public string RootDirectory { get; } = rootDirectory;

	public void EnsureCreated() => Directory.CreateDirectory(RootDirectory);

	public IEnumerable<string> EnumerateModulePaths()
	{
		if (!Directory.Exists(RootDirectory))
			return [];

		var options = new EnumerationOptions
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,
			AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint
		};

		return Directory.EnumerateFiles(RootDirectory, "*", options).Where(IsModuleFile);
	}

	public bool ExistsInDirectory(string directory, string name)
	{
		var path = Path.Combine(RootDirectory, directory);

		if (!Directory.Exists(path))
			return false;

		return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
			.Where(IsModuleFile)
			.Any(file => string.Equals(Path.GetFileNameWithoutExtension(file), name, StringComparison.OrdinalIgnoreCase));
	}

	public Task<string> ReadTextAsync(string path) => File.ReadAllTextAsync(path);

	public async Task WriteTextAsync(string path, string text)
	{
		var directory = Path.GetDirectoryName(path);

		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var temporaryPath = path + ".tmp";
		await File.WriteAllTextAsync(temporaryPath, text);
		File.Move(temporaryPath, path, overwrite: true);
	}

	public void Delete(string path) => File.Delete(path);

	public static bool IsModuleFile(string path)
		=> path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
		|| path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
}
