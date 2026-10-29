namespace ToeicCoachAI.Model.Common
{
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }

        public static ServiceResult Success(string message = "Thành công.")
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static ServiceResult Failure(string message, string? errorCode = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Success(T data, string message = "Thành công.")
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public new static ServiceResult<T> Failure(string message, string? errorCode = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Data = default,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
