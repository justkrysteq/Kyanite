using CommunityToolkit.Mvvm.ComponentModel;

namespace Kyanite.Modules.Default.Note;

public partial class NoteViewModel(ModuleFile moduleFile) : Module(moduleFile)
{
	[Synchronize]
	[ObservableProperty]
	string _text = string.Empty;
}
