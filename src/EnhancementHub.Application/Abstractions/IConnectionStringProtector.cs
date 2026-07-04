namespace EnhancementHub.Application.Abstractions;

public interface IConnectionStringProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedData);
}
