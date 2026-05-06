namespace ProductApi.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
        => new Result(true, string.Empty);

    public static Result Failure(string error)
        => new Result(false, error);

    public static Result<T> Success<T>(T value)
        => Result<T>.Success(value);

    public static Result<T> Failure<T>(string error)
        => Result<T>.Failure(error);
}

public class Result<T> : Result
{
    public T Value { get; }

    private Result(bool isSuccess, T value, string error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
        => new Result<T>(true, value, string.Empty);

    public new static Result<T> Failure(string error)
        => new Result<T>(false, default!, error);
}
