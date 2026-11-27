namespace Kyanite.Core.Models;

public class AppSettings
{
	public bool KeepRunningInBackground { get; set; } = true;
	public bool EnableNotifications { get; set; } = true;
	public bool NotifyOnOverdue { get; set; } = true;
	public int DueSoonThresholdMinutes { get; set; } = 30;
	public string? ModulesDirectory { get; set; }

	public static string DefaultModulesDirectory => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Kyanite",
		"modules");
}
