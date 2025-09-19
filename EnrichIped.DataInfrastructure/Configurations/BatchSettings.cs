namespace EnrichIped.DataInfrastructure.Configurations;

public class BatchSettings
{
    public int MaxParallelBatches { get; set; } = 5;
    public int DevelopmentBatchSize { get; set; } = 1000;
    public int CompleteBatchSize { get; set; } = 25000;
    public int LogBatchSize { get; set; } = 1000;
}