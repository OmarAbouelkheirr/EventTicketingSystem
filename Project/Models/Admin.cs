using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }

        [ForeignKey("Person")]
        public int PersonID { get; set; }
        public virtual Person Person { get; set; }
    }
}