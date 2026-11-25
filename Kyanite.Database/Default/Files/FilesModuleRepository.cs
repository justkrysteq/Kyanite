namespace Kyanite.Database.Default.Files;

public class FilesModuleRepository : IModuleRepository
{
	public void Initialize()
	{

	}

	public async Task<ModuleInformation> AddAsync(ModuleInformation module)
	{
		var moduleWithId = module with { Id = Guid.NewGuid() };

		return moduleWithId;
	}

	public async Task<IEnumerable<ModuleInformation>> GetAllAsync()
	{
		var module = new ModuleInformation {
			Id = Guid.NewGuid(),
			Name = "whejh",
			Type = "wejwkje",
		};

		return [module];
	}

	public async Task<ModuleInformation> GetSingleAsync(Guid id)
	{
		var module = new ModuleInformation {
			Id = Guid.NewGuid(),
			Name = "whejh",
			Type = "wejwkje",
		};

		return module;
	}

	public async Task<ModuleInformation> UpdateAsync(ModuleInformation module)
	{
		return module;
	}

	public async Task DeleteAsync(Guid id)
	{

	}
}
