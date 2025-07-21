using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class FileStorageDto
    {
        public string FileId { get; set; }
        public string UserId { get; set; }
        public string ConversationId { get; set; }
        public string FileName { get; set; }
        public string FileURL { get; set; }
        public DateTime UploadDate { get; set; }
    }

}
