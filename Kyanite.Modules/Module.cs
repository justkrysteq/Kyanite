using CommunityToolkit.Mvvm.ComponentModel;

namespace Kyanite.Modules;

public abstract class Module : ObservableObject
{
	public ModuleFile File { get; internal set; }
	public string Name => File.Name;
	public string Directory => File.Directory;
	public string DisplayName => Directory.Length == 0 ? Name : $"{Directory} / {Name}";

	protected Module(ModuleFile file)
	{
		File = file;
	}

	public void Refresh()
	{
		OnPropertyChanged(string.Empty);
	}
}
