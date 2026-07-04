namespace EnhancementHub.Application.Abstractions;

public interface IPiiRedactionService
{
    string Redact(string input);
}
