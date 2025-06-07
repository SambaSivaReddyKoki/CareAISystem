using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using CareAI.Core.Models;
using MessageRole = CareAI.Core.Models.MessageRole;
using RequestStatus = CareAI.Core.Enums.RequestStatus;
using CareAI.Core.Configuration;
using CareAI.Infrastructure.Data;

namespace CareAI.Services.Services
{
    public class LangChainService : ILangChainService
    {
        private readonly OpenAIClient? _openAIClient;
        private readonly CareAIDbContext _dbContext;
        private readonly string? _deploymentName;
        private readonly ChatCompletionsOptions? _chatOptions;
        private readonly ILogger<LangChainService> _logger;
        private readonly string? _modelName;
        private readonly bool _isEnabled;

        public LangChainService(
            IOptions<Core.Configuration.OpenAIOptions> openAIOptions, 
            CareAIDbContext dbContext, 
            ILogger<LangChainService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var options = openAIOptions?.Value ?? new Core.Configuration.OpenAIOptions();
            _isEnabled = options.Enabled && 
                       !string.IsNullOrEmpty(options.ApiKey) && 
                       !string.IsNullOrEmpty(options.Endpoint);
            
            if (!_isEnabled)
            {
                _logger.LogWarning("OpenAI is disabled or not properly configured");
                return;
            }

            try
            {
                _deploymentName = options.DeploymentName ?? options.ModelName ?? "gpt-4";
                _modelName = options.ModelName ?? "gpt-4";
                
                _openAIClient = new OpenAIClient(
                    new Uri(options.Endpoint!), 
                    new AzureKeyCredential(options.ApiKey!));
                    
                _chatOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    MaxTokens = 1000,
                    Temperature = 0.7f,
                    NucleusSamplingFactor = 0.95f,
                    Messages = { new ChatRequestSystemMessage("You are a helpful AI assistant.") }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
                _isEnabled = false;
            }
        }

        public async Task<string> ProcessMessageAsync(string conversationId, string userId, string message)
        {
            if (!_isEnabled || _openAIClient == null || _chatOptions == null)
            {
                _logger.LogWarning("OpenAI service is disabled or not properly configured");
                return "I'm sorry, but the AI service is currently unavailable. Please try again later or contact support if the issue persists.";
            }

            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Conversation ID is required", nameof(conversationId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is required", nameof(message));

            try
            {
                var conversation = await GetOrCreateConversationAsync(conversationId, userId);
                var userMessage = await CreateMessageAsync(conversationId, userId, message, MessageRole.User);

                // Clone chat options to avoid modifying the original
                var chatOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _chatOptions.DeploymentName,
                    MaxTokens = _chatOptions.MaxTokens,
                    Temperature = _chatOptions.Temperature,
                    NucleusSamplingFactor = _chatOptions.NucleusSamplingFactor
                };
                
                // Clear existing messages and add new ones
                chatOptions.Messages.Clear();

                // Add system message
                chatOptions.Messages.Add(new ChatRequestSystemMessage("You are a helpful and empathetic AI assistant for social services."));

                // Add conversation history (last 10 messages)
                var recentMessages = await _dbContext.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(10)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                foreach (var msg in recentMessages)
                {
                    if (msg.Role == MessageRole.User)
                    {
                        chatOptions.Messages.Add(new ChatRequestUserMessage(msg.Content));
                    }
                    else if (msg.Role == MessageRole.Assistant)
                    {
                        chatOptions.Messages.Add(new ChatRequestAssistantMessage(msg.Content));
                    }
                    else if (msg.Role == MessageRole.System)
                    {
                        chatOptions.Messages.Add(new ChatRequestSystemMessage(msg.Content));
                    }
                }


                // Add the new user message
                chatOptions.Messages.Add(new ChatRequestUserMessage(message));


                // Get AI response
                var response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
                var aiResponse = response.Value.Choices[0].Message.Content;

                // Save AI response and update conversation timestamp
                await CreateMessageAsync(conversationId, userId, aiResponse, MessageRole.Assistant);
                
                // Update conversation timestamp
                conversation.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully processed message for conversation {ConversationId}", conversationId);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for conversation {ConversationId}", conversationId);
                return "I'm sorry, but I encountered an error while processing your message. Please try again later.";
            }
        }

        private async Task<Conversation> GetOrCreateConversationAsync(string conversationId, string userId)
        {
            var conversation = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                var newConversation = new Conversation
                {
                    Id = conversationId,
                    UserId = userId,
                    Title = "New Conversation",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    State = new Dictionary<string, object>(),
                    Messages = new List<Message>()
                };
                _dbContext.Conversations.Add(newConversation);
                await _dbContext.SaveChangesAsync();
                return newConversation;
            }

            return conversation;
        }

        private async Task<Message> CreateMessageAsync(string conversationId, string userId, string content, MessageRole role)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Content = content,
                Role = role,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            return message;
        }

        public async Task<string> GetServiceRecommendationAsync(string userId, string userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
                throw new ArgumentException("User message cannot be empty", nameof(userMessage));

            if (!_isEnabled || _openAIClient == null)
            {
                _logger.LogWarning("OpenAI service is disabled or not properly configured");
                return "GeneralInquiry";
            }

            try
            {
                _logger.LogInformation("Getting service recommendation for user {UserId}", userId);
                
                var chatOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    MaxTokens = 150,
                    Temperature = 0.3f,
                    Messages =
                    {
                        new ChatRequestSystemMessage(
                            "You are an AI that helps identify social services based on user needs. " +
                            "Analyze the following message and recommend up to 3 relevant social service categories. " +
                            "Return only the service names as a comma-separated list. " +
                            "Example: 'food assistance, housing support, utility bill help'")
                    }
                };

                chatOptions.Messages.Add(new ChatRequestUserMessage(userMessage));

                var response = (await _openAIClient.GetChatCompletionsAsync(chatOptions))
                    .Value.Choices[0].Message.Content;
                
                _logger.LogInformation("Received service recommendation: {Recommendation}", response);
                
                // Return the first recommended service
                return response.Split(',')
                    .Select(s => s.Trim())
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "GeneralInquiry";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service recommendation for user {UserId}", userId);
                return "GeneralInquiry";
            }
        }

        public async Task<string> ProcessServiceRequestAsync(string userId, string serviceType, Dictionary<string, object> parameters)
        {
            if (!_isEnabled || _openAIClient == null)
            {
                _logger.LogWarning("OpenAI service is disabled or not properly configured");
                return "I'm sorry, but the service is currently unavailable. Please try again later.";
            }

            try
            {
                _logger.LogInformation("Processing service request of type {ServiceType} for user {UserId}", serviceType, userId);
                
                var chatOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    MaxTokens = 500,
                    Temperature = 0.5f,
                    Messages =
                    {
                        new ChatRequestSystemMessage(
                            $"You are an AI that helps with social services. The user has requested help with: {serviceType}. " +
                            "Please provide a helpful and empathetic response based on the user's needs.")
                    }
                };

                chatOptions.Messages.Add(new ChatRequestUserMessage(
                    $"I need help with: {serviceType}. Additional details: {string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}"))}"));

                var response = (await _openAIClient.GetChatCompletionsAsync(chatOptions))
                    .Value.Choices[0].Message.Content;
                
                _logger.LogInformation("Processed service request of type {ServiceType} for user {UserId}", serviceType, userId);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing service request of type {ServiceType} for user {UserId}", serviceType, userId);
                return "I'm sorry, but I encountered an error while processing your service request. Please try again later or contact support if the issue persists.";
            }
        }
    }
}
