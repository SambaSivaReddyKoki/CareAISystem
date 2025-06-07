using System;
using System.Collections.Generic;

namespace CareAI.Core.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public required string Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class Conversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public required string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> State { get; set; } = new();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string ConversationId { get; set; }
        public required string Content { get; set; }
        public MessageRole Role { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public enum MessageRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    public class ServiceRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public required string ServiceType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    public enum RequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        AwaitingInformation
    }
}
