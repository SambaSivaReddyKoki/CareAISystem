using System.ComponentModel.DataAnnotations;

namespace CareAI.Core.Configuration
{
    public class OpenAIOptions
    {
        [Required]
        public string ApiKey { get; set; } = string.Empty;
        
        [Required]
        public string Endpoint { get; set; } = string.Empty;
        
        [Required]
        public string ModelName { get; set; } = "gpt-4";
        
        public string? Organization { get; set; }
        
        public string? DeploymentName { get; set; }
        
        public bool Enabled { get; set; } = true;
    }
}
