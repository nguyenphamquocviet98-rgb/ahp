using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("KetQuaXepHang")]
    public class KetQuaXepHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKetQua { get; set; }

        public int? MaKyDanhGia { get; set; }

        public int? MaNhaCungCap { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiemTong { get; set; }

        public int? XepHang { get; set; }

        public bool? DeXuat { get; set; }

        // Navigation properties
        [ForeignKey("MaKyDanhGia")]
        public virtual KyDanhGia? KyDanhGia { get; set; }

        [ForeignKey("MaNhaCungCap")]
        public virtual NhaCungCap? NhaCungCap { get; set; }
    }
}
