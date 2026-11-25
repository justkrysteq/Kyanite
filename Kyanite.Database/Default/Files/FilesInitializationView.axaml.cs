using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Kyanite.Database.Default.Files;

public partial class FilesInitializationView : UserControl
{
	public FilesInitializationView()
	{
		InitializeComponent();
	}

	void OverrideDefaultPath(object? sender, RoutedEventArgs e)
	{
		var isChecked = OverrideCheckbox.IsChecked!.Value;

		if (isChecked)
		{
			OverrideText.IsVisible = true;
		}
		else
		{
			OverrideText.IsVisible = false;
		}
	}
}
