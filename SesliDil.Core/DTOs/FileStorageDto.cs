using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class FileStorageDto
    {
        public int FileId { get; set; }
        public int UserId { get; set; }
        public int ThreadId { get; set; }
        public string FileName { get; set; }
        public string FileURL { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
