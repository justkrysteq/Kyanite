using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kyanite.Modules;

public class ModuleManager()
{
	const string DefaultModuleNamespace = "Kyanite.Modules.Default";
	const string NoteType = "Note";
	const int MaxNameLength = 100;

	static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		IgnoreReadOnlyProperties = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"CON", "PRN", "AUX", "NUL",
		"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
		"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
	};

	readonly List<Module> modules = [];
	public Module[] Modules => [.. modules];

	public Dictionary<string, Type> ModuleTypes { get; private set; } = [];

	public List<string> LoadWarnings { get; } = [];

	public bool IsPrepared { get; private set; }

	IServiceProvider? _serviceProvider;
	ModuleStore? _store;

	public async Task Prepare(IServiceProvider serviceProvider, string modulesDirectory)
	{
		_serviceProvider = serviceProvider;
		_store = new ModuleStore(modulesDirectory);
		_store.EnsureCreated();

		IsPrepared = false;
		modules.Clear();
		ModuleTypes.Clear();
		LoadWarnings.Clear();

		LoadModuleTypesFromAssembly();
		await LoadSavedModules();

		IsPrepared = true;
	}

	void LoadModuleTypesFromAssembly()
	{
		var assembly = Assembly.GetExecutingAssembly();

		var foundModules = from t in assembly.GetTypes()
						   where typeof(Module).IsAssignableFrom(t) &&
								 t != typeof(Module) &&
								 t.Namespace is not null &&
								 t.Namespace.StartsWith(DefaultModuleNamespace)
						   select t;

		foreach (var moduleType in foundModules)
			ModuleTypes[moduleType.Name.Replace("ViewModel", string.Empty)] = moduleType;
	}

	async Task LoadSavedModules()
	{
		var store = RequireStore();

		foreach (var path in store.EnumerateModulePaths())
		{
			var moduleFile = await TryCreateModuleFile(path);

			if (moduleFile is null)
				continue;

			if (modules.Any(existing =>
					string.Equals(existing.Directory, moduleFile.Directory, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(existing.Name, moduleFile.Name, StringComparison.OrdinalIgnoreCase)))
			{
				LoadWarnings.Add($"Skipped \"{moduleFile.Name}\": duplicate module name in \"{moduleFile.Directory}\".");
				continue;
			}

			modules.Add(ConvertFileToModule(moduleFile));
		}
	}

	async Task<ModuleFile?> TryCreateModuleFile(string path)
	{
		var name = Path.GetFileNameWithoutExtension(path);
		var type = await ReadModuleType(path);

		if (type is null)
			return null;

		if (!ModuleTypes.ContainsKey(type))
		{
			LoadWarnings.Add($"Skipped \"{name}\": unknown module type \"{type}\".");
			return null;
		}

		if (!path.EndsWith(ExtensionFor(type), StringComparison.OrdinalIgnoreCase))
		{
			LoadWarnings.Add($"Skipped \"{name}\": module type \"{type}\" does not match the file extension.");
			return null;
		}

		var directory = Path.GetDirectoryName(path) ?? string.Empty;
		var relativeDirectory = Path.GetRelativePath(RequireStore().RootDirectory, directory);

		if (relativeDirectory == ".")
			relativeDirectory = string.Empty;

		return new ModuleFile(
			relativeDirectory.Replace('\\', '/'),
			name,
			type,
			path,
			File.GetLastWriteTimeUtc(path));
	}

	async Task<string?> ReadModuleType(string path)
	{
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			return NoteType;

		try
		{
			using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));

			if (document.RootElement.ValueKind != JsonValueKind.Object ||
				!document.RootElement.TryGetProperty("type", out var typeElement) ||
				typeElement.ValueKind != JsonValueKind.String)
			{
				LoadWarnings.Add($"Skipped \"{Path.GetFileName(path)}\": missing or invalid \"type\".");
				return null;
			}

			return typeElement.GetString();
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			LoadWarnings.Add($"Skipped \"{Path.GetFileName(path)}\": {ex.Message}");
			return null;
		}
	}

	public async Task<Module> CreateModule(string name, string type, string directory = "")
	{
		var store = RequireStore();

		type = type.Replace("ViewModel", string.Empty);

		if (!ModuleTypes.TryGetValue(type, out var moduleType))
			throw new Exception($"Wrong module name: {type}, module is not registered in ModuleManager");

		name = ValidateName(name);
		directory = NormalizeDirectory(directory);

		if (store.ExistsInDirectory(directory, name))
			throw new Exception($"A module named \"{name}\" already exists in this directory.");

		var path = Path.Combine(store.RootDirectory, directory, name + ExtensionFor(type));
		var module = ConvertFileToModule(new ModuleFile(directory, name, type, path, DateTime.MinValue));

		await store.WriteTextAsync(path, Serialize(module));
		module.File = module.File with { LastWriteTimeUtc = File.GetLastWriteTimeUtc(path) };

		modules.Add(module);
		return module;
	}

	public async Task LoadModule(Module module)
	{
		var store = RequireStore();
		var text = await store.ReadTextAsync(module.File.Path);
		var fields = GetSynchronizedFields(module.GetType());

		if (IsNote(module))
		{
			if (fields.Length != 1 || fields[0].FieldType != typeof(string))
				throw new InvalidOperationException($"Plain-text module \"{module.File.Type}\" must have exactly one string [Synchronize] field.");

			fields[0].SetValue(module, text);
		}
		else
		{
			var root = JsonNode.Parse(text) as JsonObject
				?? throw new InvalidOperationException($"Module file \"{module.File.Path}\" is not a JSON object.");

			foreach (var field in fields)
			{
				var node = root[JsonKey(field)];

				if (node is null)
					continue;

				field.SetValue(module, JsonSerializer.Deserialize(node.ToJsonString(), field.FieldType, JsonOptions));
			}
		}

		module.File = module.File with { LastWriteTimeUtc = File.GetLastWriteTimeUtc(module.File.Path) };
		module.Refresh();
	}

	public async Task SaveModule(Module module)
	{
		var store = RequireStore();
		var currentWriteTime = File.GetLastWriteTimeUtc(module.File.Path);

		if (module.File.LastWriteTimeUtc != DateTime.MinValue && currentWriteTime != module.File.LastWriteTimeUtc)
			throw new IOException($"\"{module.DisplayName}\" changed on disk. Reopen it before saving to avoid overwriting the external change.");

		await store.WriteTextAsync(module.File.Path, Serialize(module));
		module.File = module.File with { LastWriteTimeUtc = File.GetLastWriteTimeUtc(module.File.Path) };
	}

	public Task DeleteModule(Module module)
	{
		if (!modules.Contains(module))
			throw new Exception("Module not found");

		RequireStore().Delete(module.File.Path);
		modules.Remove(module);
		return Task.CompletedTask;
	}

	public void ClearData()
	{
		IsPrepared = false;
		modules.Clear();
		ModuleTypes.Clear();
		LoadWarnings.Clear();
		_store = null;
	}

	Module ConvertFileToModule(ModuleFile moduleFile)
	{
		if (_serviceProvider is null)
			throw new Exception("Service provider is not available");

		if (!ModuleTypes.TryGetValue(moduleFile.Type, out var moduleType))
			throw new Exception("Module type does not exist in ModuleManager registry");

		var instance = NativeActivator.CreateInstance(moduleType, _serviceProvider, moduleFile);
		return (Module)instance;
	}

	string Serialize(Module module)
	{
		if (IsNote(module))
		{
			var fields = GetSynchronizedFields(module.GetType());

			if (fields.Length != 1 || fields[0].FieldType != typeof(string))
				throw new InvalidOperationException($"Plain-text module \"{module.File.Type}\" must have exactly one string [Synchronize] field.");

			return (string?)fields[0].GetValue(module) ?? string.Empty;
		}

		var root = new JsonObject { ["type"] = module.File.Type };

		foreach (var field in GetSynchronizedFields(module.GetType()))
			root[JsonKey(field)] = JsonSerializer.SerializeToNode(field.GetValue(module), field.FieldType, JsonOptions);

		return root.ToJsonString(JsonOptions);
	}

	static FieldInfo[] GetSynchronizedFields(Type type)
		=> type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(field => field.GetCustomAttribute<SynchronizeAttribute>() is not null)
			.ToArray();

	static string JsonKey(FieldInfo field)
	{
		var key = field.Name.TrimStart('_');

		if (key == "type")
			throw new InvalidOperationException("Synchronized field name \"type\" is reserved.");

		return key;
	}

	static bool IsNote(Module module) => module.File.Type == NoteType;

	static string ExtensionFor(string type) => type == NoteType ? ".md" : ".json";

	static string ValidateName(string name)
	{
		name = name.Trim();

		if (name.Length == 0)
			throw new Exception("Module name cannot be empty.");

		if (name.Length > MaxNameLength)
			throw new Exception($"Module name cannot be longer than {MaxNameLength} characters.");

		if (name.StartsWith(".") || name.EndsWith("."))
			throw new Exception("Module name cannot start or end with a dot.");

		if (name.Any(character => character < 32 || "<>:\"/\\|?*".Contains(character)))
			throw new Exception("Module name contains a character that cannot be used in a file name.");

		if (ReservedNames.Contains(name.Split('.')[0]))
			throw new Exception($"\"{name}\" is a reserved file name.");

		return name;
	}

	static string NormalizeDirectory(string directory)
	{
		directory = directory.Replace('\\', '/').Trim('/');

		if (directory.Length == 0)
			return string.Empty;

		var segments = directory.Split('/');

		if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
			throw new Exception("Invalid module directory.");

		return string.Join('/', segments.Select(ValidateName));
	}

	ModuleStore RequireStore()
		=> _store ?? throw new Exception("Module manager needs to be prepared before usage");
}
