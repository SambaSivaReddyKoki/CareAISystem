using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareAI.Core.Models;
using CareAI.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CareAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly ILangChainService _langChainService;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(
            ILangChainService langChainService,
            ILogger<ConversationController> logger)
        {
            _langChainService = langChainService;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserId))
                {
                    return BadRequest("User ID is required");
                }

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Title = $"Conversation {DateTime.UtcNow:yyyy-MM-dd}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    State = new Dictionary<string, object>(),
                    Messages = new List<Message>()
                };

                // Save conversation to database
                // _dbContext.Conversations.Add(conversation);
                // await _dbContext.SaveChangesAsync();


                // Simulate an async operation to make the method truly async
                await Task.Delay(10);


                return Ok(new
                {
                    ConversationId = conversation.Id,
                    Message = "Conversation started successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return StatusCode(500, "An error occurred while starting the conversation");
            }
        }

        [HttpPost("{conversationId}/message")]
        public async Task<IActionResult> SendMessage(string conversationId, [FromBody] SendMessageRequest request)
        {
            try
            {
                // 1. Process the message with LangChain
                var response = await _langChainService.ProcessMessageAsync(conversationId, request.UserId, request.Message);
                
                // 2. Check if this is a service request
                var serviceType = await _langChainService.GetServiceRecommendationAsync(request.UserId, request.Message);
                
                if (serviceType != "GeneralInquiry")
                {
                    // Process as service request
                    var serviceResponse = await _langChainService.ProcessServiceRequestAsync(
                        request.UserId, 
                        serviceType,
                        new Dictionary<string, object> { { "userMessage", request.Message } });
                    
                    response = $"{response}\n\n{serviceResponse}";
                }

                return Ok(new
                {
                    Message = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return StatusCode(500, "An error occurred while processing your message");
            }
        }
    }

    public class StartConversationRequest
    {
        public required string UserId { get; set; }
    }

    public class SendMessageRequest
    {
        public required string UserId { get; set; }
        public required string Message { get; set; }
    }
}
