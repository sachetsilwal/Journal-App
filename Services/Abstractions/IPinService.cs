namespace Journal.Services.Abstractions;

public interface IPinService
{
    bool HasPin();
    void SetPin(string pin);
    bool Verify(string pin);
    void ClearPin();
}
