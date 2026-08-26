using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DonViTinh")]
    public class DonViTinh
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonViTinh { get; set; }

        [MaxLength(20)]
        public string? TenDonVi { get; set; }

        // Navigation properties
        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}
