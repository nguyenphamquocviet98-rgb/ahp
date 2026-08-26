using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DiemDanhGiaNCC")]
    public class DiemDanhGiaNCC
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDiem { get; set; }

        public int? MaKyDanhGia { get; set; }

        public int? MaNhaCungCap { get; set; }

        public int? MaTieuChi { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Diem { get; set; }

        // Navigation properties
        [ForeignKey("MaKyDanhGia")]
        public virtual KyDanhGia? KyDanhGia { get; set; }

        [ForeignKey("MaNhaCungCap")]
        public virtual NhaCungCap? NhaCungCap { get; set; }

        [ForeignKey("MaTieuChi")]
        public virtual TieuChi? TieuChi { get; set; }
    }
}
