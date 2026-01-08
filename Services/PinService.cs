using Journal.Services.Abstractions;

namespace Journal.Services;

public class PinService : IPinService
{
    private const string PinHashKey = "journal_pin_hash";

    public bool HasPin()
        => !string.IsNullOrWhiteSpace(Preferences.Get(PinHashKey, ""));

    public void SetPin(string pin)
    {
        var hash = SecurityService.HashPin(pin);
        Preferences.Set(PinHashKey, hash);
    }

    public bool Verify(string pin)
    {
        var stored = Preferences.Get(PinHashKey, "");
        if (string.IsNullOrWhiteSpace(stored)) return true; // no pin = unlocked
        return SecurityService.Verify(pin, stored);
    }

    public void ClearPin()
        => Preferences.Remove(PinHashKey);
}
