using code.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace code.Services;

public class PasswordService
{
    private const string Prefix = "v1:";
    private readonly PasswordHasher<NguoiDung> _hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));

    public string Hash(NguoiDung user, string password) => Prefix + _hasher.HashPassword(user, password);

    // Tài khoản cũ được chuyển sang hash chỉ sau khi nhập đúng mật khẩu hiện có.
    public bool Verify(NguoiDung user, string password, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (string.IsNullOrEmpty(user.MatKhau) || string.IsNullOrEmpty(password)) return false;
        if (!user.MatKhau.StartsWith(Prefix, StringComparison.Ordinal))
        {
            needsUpgrade = string.Equals(user.MatKhau, password, StringComparison.Ordinal);
            return needsUpgrade;
        }
        try
        {
            var result = _hasher.VerifyHashedPassword(user, user.MatKhau[Prefix.Length..], password);
            needsUpgrade = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { return false; }
    }
}
