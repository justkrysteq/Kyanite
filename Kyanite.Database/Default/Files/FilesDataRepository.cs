namespace Kyanite.Database.Default.Files;

public class FilesDataRepository : IDataRepository
{
	public void Initialize()
	{

	}

	public async Task<DataInformation> AddAsync(DataInformation data, Guid moduleId)
	{

		return data;
	}

	public async Task<IEnumerable<DataInformation>> GetAllAsync(Guid moduleId)
	{
		var module = new DataInformation
		{
			Id = "ewheiu",
			Value = "whejh",
			Type = "wejwkje",
		};

		return [module];
	}

	public async Task<DataInformation> GetSingleAsync(string dataId, Guid moduleId)
	{
		var module = new DataInformation
		{
			Id = "ewheiu",
			Value = "whejh",
			Type = "wejwkje",
		};

		return module;
	}

	public async Task<DataInformation> UpdateAsync(DataInformation data, Guid moduleId)
	{
		return data;
	}

	public Task DeleteAsync(string dataId, Guid moduleId)
	{
		return Task.CompletedTask;
	}
}
