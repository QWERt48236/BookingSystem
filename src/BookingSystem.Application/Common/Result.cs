namespace BookingSystem.Application.Common;

public enum ResultErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Failure
}

public class Result
{
    public bool Succeeded { get; }
    public ResultErrorType ErrorType { get; }
    public IReadOnlyCollection<string> Errors { get; }

    protected Result(bool succeeded, ResultErrorType errorType, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        ErrorType = errorType;
        Errors = errors;
    }

    public static Result Success() => new(true, ResultErrorType.None, []);
    public static Result NotFound(params string[] errors) => new(false, ResultErrorType.NotFound, errors);
    public static Result Validation(IEnumerable<string> errors) => new(false, ResultErrorType.Validation, errors.ToArray());
    public static Result Conflict(params string[] errors) => new(false, ResultErrorType.Conflict, errors);
    public static Result Unauthorized(params string[] errors) => new(false, ResultErrorType.Unauthorized, errors);
    public static Result Failure(params string[] errors) => new(false, ResultErrorType.Failure, errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, ResultErrorType errorType, IReadOnlyCollection<string> errors)
        : base(succeeded, errorType, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, ResultErrorType.None, []);
    public static new Result<T> NotFound(params string[] errors) => new(false, default, ResultErrorType.NotFound, errors);
    public static new Result<T> Validation(IEnumerable<string> errors) => new(false, default, ResultErrorType.Validation, errors.ToArray());
    public static new Result<T> Conflict(params string[] errors) => new(false, default, ResultErrorType.Conflict, errors);
    public static new Result<T> Unauthorized(params string[] errors) => new(false, default, ResultErrorType.Unauthorized, errors);
    public static new Result<T> Failure(params string[] errors) => new(false, default, ResultErrorType.Failure, errors);
}
