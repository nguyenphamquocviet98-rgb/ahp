using Microsoft.EntityFrameworkCore;
using code.Models;

namespace code.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
