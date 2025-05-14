using System.Threading.Tasks;

namespace keyraces.Core.Interfaces
{
    public interface ITextGenerationService
    {
        Task<string> GenerateTextAsync(string topic = "", string difficulty = "medium", int length = 300, string language = "ru");
    }
}
