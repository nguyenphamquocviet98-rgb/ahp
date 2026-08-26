using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("TieuChi")]
    public class TieuChi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTieuChi { get; set; }

        [MaxLength(50)]
        public string? TenTieuChi { get; set; }

        [MaxLength(10)]
        public string? LoaiTieuChi { get; set; }

        // Navigation properties
        public virtual ICollection<TrongSoTieuChi> TrongSoTieuChis { get; set; } = new List<TrongSoTieuChi>();
        public virtual ICollection<DiemDanhGiaNCC> DiemDanhGiaNCCs { get; set; } = new List<DiemDanhGiaNCC>();
        public virtual ICollection<MaTranSoSanh> MaTranSoSanhDongs { get; set; } = new List<MaTranSoSanh>();
        public virtual ICollection<MaTranSoSanh> MaTranSoSanhCots { get; set; } = new List<MaTranSoSanh>();
    }
}
