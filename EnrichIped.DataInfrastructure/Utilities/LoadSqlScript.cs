namespace EnrichIped.DataInfrastructure.Utilities;

public static class SqlScriptLoader
{
	public static string LoadSqlScript(string fileName)
	{
		var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		var scriptPath = Path.Combine(baseDirectory, "Scripts", fileName);

		if (!File.Exists(scriptPath))
			scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", fileName);

		if (!File.Exists(scriptPath))
			throw new FileNotFoundException($"O arquivo SQL '{fileName}' não foi encontrado.", scriptPath);

		return File.ReadAllText(scriptPath);
	}
}