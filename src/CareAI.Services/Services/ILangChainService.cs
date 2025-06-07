using System.Collections.Generic;
using System.Threading.Tasks;
using CareAI.Core.Configuration;
using CareAI.Core.Models;

namespace CareAI.Services.Services
{
    public interface ILangChainService
    {
        Task<string> ProcessMessageAsync(string conversationId, string userId, string message);
        Task<string> GetServiceRecommendationAsync(string userId, string userMessage);
        Task<string> ProcessServiceRequestAsync(string userId, string serviceType, Dictionary<string, object> parameters);
    }
}
