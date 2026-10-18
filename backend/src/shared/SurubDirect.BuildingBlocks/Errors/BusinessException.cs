namespace SurubDirect.BuildingBlocks;
public sealed class BusinessException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}
