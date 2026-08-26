using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("MaTranSoSanh")]
    public class MaTranSoSanh
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSoSanh { get; set; }

        public int? MaKyDanhGia { get; set; }

        public int? MaTieuChiDong { get; set; }

        public int? MaTieuChiCot { get; set; }

        [Column(TypeName = "decimal(5,3)")]
        public decimal? GiaTri { get; set; }

        // Navigation properties
        [ForeignKey("MaKyDanhGia")]
        public virtual KyDanhGia? KyDanhGia { get; set; }

        [ForeignKey("MaTieuChiDong")]
        public virtual TieuChi? TieuChiDong { get; set; }

        [ForeignKey("MaTieuChiCot")]
        public virtual TieuChi? TieuChiCot { get; set; }
    }
}
