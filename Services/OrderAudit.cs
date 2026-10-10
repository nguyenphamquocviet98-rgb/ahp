using System.Security.Claims;
using System.Text.Json;
using code.Models;

namespace code.Services;

public record OrderAuditEntry(DateTime Time, int UserId, string UserName, string Status);

public static class OrderAudit
{
    public static List<OrderAuditEntry> Read(string? json) => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<OrderAuditEntry>>(json) ?? [];
    public static void Record(DonHangB2B order, ClaimsPrincipal user)
    {
        var history = Read(order.LichSuXuLyJson);
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        history.Add(new(DateTime.Now, id, user.Identity?.Name ?? "", order.TrangThai));
        order.LichSuXuLyJson = JsonSerializer.Serialize(history);
    }
}
