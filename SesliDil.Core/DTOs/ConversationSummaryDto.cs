using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class MistakeSampleDto
    {
        public string Original { get; set; } = "";
        public string Corrected { get; set; } = "";
    }

    public class ConversationSummaryDto
    {
        // Mevcut alanlar
        public string ConversationId { get; set; } = default!;
        public string Summary { get; set; } = "";     // (opsiyonel) AI metin özeti

        // Yeni metrikler
        public int DurationSeconds { get; set; }      // konuşmanın toplam süresi
        public int FluencyWpm { get; set; }           // kullanıcı WPM (kelime/dk)

        // Hata örnekleri (mesajlardan CorrectedText ile çıkarılabilir)
        public int MistakesCount { get; set; }        // örnek sayısı
        public List<MistakeSampleDto> MistakeSamples { get; set; } = new();

        // Ek faydalı alanlar (UI için güzel olur)
        public int TotalWords { get; set; }           // kullanıcının söylediği toplam kelime
        public int MessageCount { get; set; }         // tüm mesaj sayısı
        public int UserMessageCount { get; set; }     // kullanıcı mesaj sayısı
        public int AgentMessageCount { get; set; }    // agent mesaj sayısı

        public DateTime StartedAtUtc { get; set; }    // ilk mesaj zamanı
        public DateTime EndedAtUtc { get; set; }      // son mesaj zamanı

        // Mistakes kullanmayacaksan da doldurabileceğin alan:
        public List<string> Highlights { get; set; } = new(); // 3–5 kelime/ifade
    }
}
