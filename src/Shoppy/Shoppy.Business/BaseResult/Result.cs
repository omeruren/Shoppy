using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shoppy.Business.BaseResult;

public sealed class Result<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    [JsonPropertyName("errorMessages")]
    public List<string> ErrorMessages { get; set; } = [];
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccessful { get; set; } = true;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    [JsonConstructor]
    public Result()
    {

    }
    public Result(T data)
    {
        Data = data;
    }

    public Result(int statusCode, List<string> errorMessages)
    {
        IsSuccessful = false;
        StatusCode = statusCode;
        ErrorMessages = errorMessages;
    }

    public Result(int statusCode, string errorMessage)
    {
        IsSuccessful = false;
        StatusCode = statusCode;
        ErrorMessages = [errorMessage];
    }


    public static implicit operator Result<T>(T data) => new(data);

    public static implicit operator Result<T>((int statusCode, List<string> errorMessages) parameters)
        => new(parameters.statusCode, parameters.errorMessages);

    public static implicit operator Result<T>((int statusCode, string errorMessage) parameters)
        => new(parameters.statusCode, parameters.errorMessage);

    // SUCCESS
    public static Result<T> Success(T data) => new(data);

    public static Result<T> Success(T data, int statusCode) => new(data) { StatusCode = statusCode };

    // FAILURE
    public static Result<T> Failure(int statusCode, List<string> errorMessages) => new(statusCode, errorMessages);

    public static Result<T> Failure(int statusCode, string errorMessage) => new(statusCode, errorMessage);

    public static Result<T> Failure(string errorMessage) => new(500, errorMessage);

    public override string ToString() => JsonSerializer.Serialize(this);

}
