using System.Security.Claims;
using code.Models;

namespace code.Services;

public static class BusinessAccess
{
    public static int CompanyId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("MaDoanhNghiep"), out var id) ? id : 0;
    public static bool IsPlatformAdmin(this ClaimsPrincipal user) => user.IsInRole("admin") || user.IsInRole("admin_san");
    public static bool IsCompanyManager(this ClaimsPrincipal user) => user.CompanyId() > 0 &&
        (user.IsInRole("chu_npp") || user.IsInRole("quanly"));
    public static bool IsCompanyMember(this ClaimsPrincipal user) => user.CompanyId() > 0 &&
        (user.IsCompanyManager() || user.IsInRole("nhanvien_npp") || user.IsInRole("nhanvien"));
    public static bool CanViewOrder(this ClaimsPrincipal user, DonHangB2B order) => user.IsPlatformAdmin() ||
        (user.IsCompanyMember() && (order.MaNPPMua == user.CompanyId() || order.MaNPPBan == user.CompanyId()));
}
