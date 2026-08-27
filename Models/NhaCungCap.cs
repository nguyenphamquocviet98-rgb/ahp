using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("NhaCungCap")]
    public class NhaCungCap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNhaCungCap { get; set; }

        [MaxLength(20)]
        public string? MaNhaCungCapCode { get; set; }

        [MaxLength(100)]
        public string? TenNhaCungCap { get; set; }

        [MaxLength(50)]
        public string? NguoiLienHe { get; set; }

        [MaxLength(15)]
        public string? SoDienThoai { get; set; }

        [MaxLength(50)]
        public string? Email { get; set; }

        [MaxLength(150)]
        public string? DiaChi { get; set; }

        [MaxLength(255)]
        public string? HinhAnh { get; set; }

        public bool? TrangThai { get; set; }

        // Navigation properties
        public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
        public virtual ICollection<DiemDanhGiaNCC> DiemDanhGiaNCCs { get; set; } = new List<DiemDanhGiaNCC>();
        public virtual ICollection<KetQuaXepHang> KetQuaXepHangs { get; set; } = new List<KetQuaXepHang>();
    }
}
