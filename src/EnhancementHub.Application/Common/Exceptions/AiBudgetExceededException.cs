namespace EnhancementHub.Application.Common.Exceptions;

public sealed class AiBudgetExceededException : Exception
{
    public AiBudgetExceededException(string message)
        : base(message)
    {
    }
}
