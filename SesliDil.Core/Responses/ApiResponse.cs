using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Responses
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static ApiResponse Ok(string message = "İşlem başarılı.") => new(true, message);
        public static ApiResponse Fail(string message = "Bir hata oluştu.") => new(false, message);
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T Data { get; set; }

        public ApiResponse(bool success, string message, T data) : base(success, message)
        {
            Data = data;
        }

        public static ApiResponse<T> Ok(T data, string message = "İşlem başarılı.")
            => new(true, message, data);

        public static ApiResponse<T> Fail(string message = "Bir hata oluştu.", T data = default)
            => new(false, message, data);
    }
}
