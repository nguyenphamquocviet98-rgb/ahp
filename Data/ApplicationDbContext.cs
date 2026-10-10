using code.Services;
using Microsoft.EntityFrameworkCore;
using code.Models;

namespace code.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _http;
        private bool HasRequest => _http?.HttpContext != null;
        private bool IsAdmin => _http?.HttpContext?.User.IsPlatformAdmin() == true;
        private int CompanyId => _http?.HttpContext?.User.CompanyId() ?? 0;
        private int UserId => int.TryParse(_http?.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? http = null)
            : base(options)
        {
            _http = http;
        }

        // Bảng gốc
        public virtual DbSet<NguoiDung> NguoiDungs { get; set; }
        public virtual DbSet<DanhMuc> DanhMucs { get; set; }
        public virtual DbSet<DonViTinh> DonViTinhs { get; set; }
        public virtual DbSet<SanPham> SanPhams { get; set; }
        public virtual DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public virtual DbSet<PhieuNhap> PhieuNhaps { get; set; }
        public virtual DbSet<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; }
        public virtual DbSet<TonKho> TonKhos { get; set; }
        public virtual DbSet<LichSuNhapHang> LichSuNhapHangs { get; set; }
        public virtual DbSet<TieuChi> TieuChis { get; set; }
        public virtual DbSet<KyDanhGia> KyDanhGias { get; set; }
        public virtual DbSet<MaTranSoSanh> MaTranSoSanhs { get; set; }
        public virtual DbSet<TrongSoTieuChi> TrongSoTieuChis { get; set; }
        public virtual DbSet<DiemDanhGiaNCC> DiemDanhGiaNCCs { get; set; }
        public virtual DbSet<KetQuaXepHang> KetQuaXepHangs { get; set; }

        // Bảng mới - Sàn B2B
        public virtual DbSet<DoanhNghiep> DoanhNghieps { get; set; }
        public virtual DbSet<TonKhoNPP> TonKhoNPPs { get; set; }
        public virtual DbSet<DonHangB2B> DonHangB2Bs { get; set; }
        public virtual DbSet<ChiTietDonHangB2B> ChiTietDonHangB2Bs { get; set; }
        public virtual DbSet<PhienAHP_B2B> PhienAHP_B2Bs { get; set; }
        public virtual DbSet<KetQuaAHP_B2B> KetQuaAHP_B2Bs { get; set; }
        public virtual DbSet<DanhGiaNPP> DanhGiaNPPs { get; set; }
        public virtual DbSet<QuangCaoBanner> QuangCaoBanners { get; set; }
        public virtual DbSet<DoanhThuSan> DoanhThuSans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Token dựa trên cột sẵn có: SaveChanges thất bại và rollback nếu trạng thái/kho đã đổi.
            modelBuilder.Entity<DonHangB2B>().Property(d => d.TrangThai).IsConcurrencyToken();
            modelBuilder.Entity<TonKhoNPP>().Property(t => t.SoLuongTon).IsConcurrencyToken();
            modelBuilder.Entity<PhieuNhap>().Property(p => p.TrangThai).IsConcurrencyToken();

            // Dữ liệu nghiệp vụ riêng theo doanh nghiệp; tác vụ nền không có HTTP vẫn đọc được.
            modelBuilder.Entity<PhieuNhap>().HasQueryFilter(p => !HasRequest || IsAdmin ||
                (CompanyId > 0 ? p.NguoiDung != null && p.NguoiDung.MaDoanhNghiep == CompanyId : p.MaNguoiDung == UserId && UserId > 0));
            modelBuilder.Entity<ChiTietPhieuNhap>().HasQueryFilter(c => !HasRequest || IsAdmin ||
                (c.PhieuNhap != null && (CompanyId > 0 ? c.PhieuNhap.NguoiDung != null && c.PhieuNhap.NguoiDung.MaDoanhNghiep == CompanyId : c.PhieuNhap.MaNguoiDung == UserId && UserId > 0)));
            modelBuilder.Entity<LichSuNhapHang>().HasQueryFilter(c => !HasRequest || IsAdmin ||
                (c.PhieuNhap != null && (CompanyId > 0 ? c.PhieuNhap.NguoiDung != null && c.PhieuNhap.NguoiDung.MaDoanhNghiep == CompanyId : c.PhieuNhap.MaNguoiDung == UserId && UserId > 0)));
            modelBuilder.Entity<KyDanhGia>().HasQueryFilter(p => !HasRequest || IsAdmin ||
                (CompanyId > 0 ? p.NguoiDung != null && p.NguoiDung.MaDoanhNghiep == CompanyId : p.MaNguoiDung == UserId && UserId > 0));
            modelBuilder.Entity<KetQuaXepHang>().HasQueryFilter(c => !HasRequest || IsAdmin ||
                (c.KyDanhGia != null && (CompanyId > 0 ? c.KyDanhGia.NguoiDung != null && c.KyDanhGia.NguoiDung.MaDoanhNghiep == CompanyId : c.KyDanhGia.MaNguoiDung == UserId && UserId > 0)));

            // Cấu hình quan hệ SanPham 1-1 TonKho
            modelBuilder.Entity<TonKho>()
                .HasOne(t => t.SanPham)
                .WithOne(s => s.TonKho)
                .HasForeignKey<TonKho>(t => t.MaSanPham);

            // Cấu hình MaTranSoSanh với TieuChiDong và TieuChiCot
            modelBuilder.Entity<MaTranSoSanh>()
                .HasOne(m => m.TieuChiDong)
                .WithMany(t => t.MaTranSoSanhDongs)
                .HasForeignKey(m => m.MaTieuChiDong)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaTranSoSanh>()
                .HasOne(m => m.TieuChiCot)
                .WithMany(t => t.MaTranSoSanhCots)
                .HasForeignKey(m => m.MaTieuChiCot)
                .OnDelete(DeleteBehavior.Restrict);

            // Computed column ThanhTien trong ChiTietPhieuNhap
            modelBuilder.Entity<ChiTietPhieuNhap>()
                .Property(c => c.ThanhTien)
                .ValueGeneratedOnAddOrUpdate();

            // Cấu hình DoanhNghiep - NguoiDung (1-N)
            modelBuilder.Entity<NguoiDung>()
                .HasOne(n => n.DoanhNghiep)
                .WithMany(d => d.NguoiDungs)
                .HasForeignKey(n => n.MaDoanhNghiep)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình TonKhoNPP - unique (MaDoanhNghiep, MaSanPham)
            modelBuilder.Entity<TonKhoNPP>()
                .HasIndex(t => new { t.MaDoanhNghiep, t.MaSanPham })
                .IsUnique();

            // Cấu hình DonHangB2B
            modelBuilder.Entity<DonHangB2B>()
                .HasOne(d => d.NPPMua)
                .WithMany()
                .HasForeignKey(d => d.MaNPPMua)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonHangB2B>()
                .HasOne(d => d.NPPBan)
                .WithMany()
                .HasForeignKey(d => d.MaNPPBan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonHangB2B>()
                .HasOne(d => d.NguoiDat)
                .WithMany()
                .HasForeignKey(d => d.MaNguoiDat)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietDonHangB2B>()
                .Property(c => c.ThanhTien)
                .ValueGeneratedOnAddOrUpdate();

            // Cấu hình PhienAHP_B2B
            modelBuilder.Entity<PhienAHP_B2B>()
                .HasOne(p => p.NPPMua)
                .WithMany()
                .HasForeignKey(p => p.MaNPPMua)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhienAHP_B2B>()
                .HasOne(p => p.SanPham)
                .WithMany()
                .HasForeignKey(p => p.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhienAHP_B2B>()
                .HasOne(p => p.NPPDeXuat)
                .WithMany()
                .HasForeignKey(p => p.MaNPPDeXuat)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình KetQuaAHP_B2B
            modelBuilder.Entity<KetQuaAHP_B2B>()
                .HasOne(k => k.DoanhNghiep)
                .WithMany()
                .HasForeignKey(k => k.MaDoanhNghiep)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình DanhGiaNPP
            modelBuilder.Entity<DanhGiaNPP>()
                .HasOne(d => d.NPPMua)
                .WithMany()
                .HasForeignKey(d => d.MaNPPMua)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DanhGiaNPP>()
                .HasOne(d => d.NPPBan)
                .WithMany()
                .HasForeignKey(d => d.MaNPPBan)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình QuangCaoBanner
            modelBuilder.Entity<QuangCaoBanner>()
                .HasOne(q => q.DoanhNghiep)
                .WithMany()
                .HasForeignKey(q => q.MaDoanhNghiep)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình DoanhThuSan
            modelBuilder.Entity<DoanhThuSan>()
                .HasOne(d => d.DoanhNghiep)
                .WithMany()
                .HasForeignKey(d => d.MaDoanhNghiep)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình tương thích cho các bảng có Database Triggers (vô hiệu hóa OUTPUT clause của EF Core)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    modelBuilder.Entity(entityType.ClrType).ToTable(tb => tb.UseSqlOutputClause(false));
                }
            }
        }
    }
}

