using System;
using System.Collections.Generic;

namespace SesliDil.Core.DTOs
{
    public class MistakeSampleDto
    {
        public string Original { get; set; } = "";
        public string Corrected { get; set; } = "";
    }

    public class ConversationSummaryDto
    {
        public string ConversationId { get; set; } = default!;
        public string Summary { get; set; } = "";

        // Metrikler
        public int DurationSeconds { get; set; }

        // Hata örnekleri
        public int MistakesCount { get; set; }
        public List<MistakeSampleDto> MistakeSamples { get; set; } = new();

        // Ek faydalı alanlar
        public int TotalWords { get; set; }
        public int MessageCount { get; set; }
        public int UserMessageCount { get; set; }
        public int AgentMessageCount { get; set; }

        public DateTime StartedAtUtc { get; set; }
        public DateTime EndedAtUtc { get; set; }

        public List<string> Highlights { get; set; } = new();
    }
}