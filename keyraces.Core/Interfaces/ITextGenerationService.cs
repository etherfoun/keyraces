namespace keyraces.Core.Interfaces
{
    public interface ITextGenerationService
    {
        Task<string> GenerateTextAsync(string topic = "", string difficulty = "medium", int length = 300);
    }
}
