using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_API.Entities
{
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Author { get; set; } = string.Empty;

        public int YearOfPublish { get; set; }
    }
}
