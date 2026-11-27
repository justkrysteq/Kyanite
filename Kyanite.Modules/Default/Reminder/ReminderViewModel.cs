using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Core.Services;

namespace Kyanite.Modules.Default.Reminder;

public partial class ReminderViewModel(ModuleFile moduleFile, NotificationServiceProvider notificationServiceProvider) : Module(moduleFile)
{
	[ObservableProperty] string _notificationTitle = string.Empty;
	[ObservableProperty] string _notificationDescription = string.Empty;

	[RelayCommand]
	void SendNotification()
	{
		var notificationService = notificationServiceProvider.ActiveService;
		notificationService.Show(NotificationTitle, NotificationDescription);
	}
}
