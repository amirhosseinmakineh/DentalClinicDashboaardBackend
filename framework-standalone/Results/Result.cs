namespace DentalDashboard.Framwork.Domain
{
    public class Result
    {
        public bool IsSuccess { get; protected set; }

        public string Message { get; protected set; } = string.Empty;

        protected Result(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static Result Success(string message = "")
        {
            return new Result(true, message);
        }

        public static Result Failure(string message)
        {
            return new Result(false, message);
        }
    }

    public class Result<T> : Result
    {
        public T? Data { get; private set; }

        protected Result(T? data, bool isSuccess, string message) : base(isSuccess, message)
        {
            Data = data;
        }

        public static Result<T> Success(T data, string message = "")
        {
            return new Result<T>(data, true, message);
        }

        public new static Result<T> Failure(string message)
        {
            return new Result<T>(default, false, message);
        }
    }
}
