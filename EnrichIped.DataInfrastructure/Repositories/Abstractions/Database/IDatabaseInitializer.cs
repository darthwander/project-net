namespace EnrichIped.DataInfrastructure.Repositories.Abstractions.Database;

public interface IDatabaseInitializer
{
	Task InitializeAsync();
}