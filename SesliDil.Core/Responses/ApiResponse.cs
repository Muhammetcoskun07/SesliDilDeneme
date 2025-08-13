using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Responses
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public string Error { get; set; }
        public T Body { get; set; }

        public ApiResponse(string message, T body, string error = null)
        {
            Message = message;
            Body = body;
            Error = error;
        }
    }
}
