using System.ComponentModel.DataAnnotations;

namespace Book_API.Models
{
    /// <summary>
    /// A DTO for adding a new Book
    /// </summary>
    public class BookForCreationDto
    {
        /// <summary>
        /// Name of the Book
        /// </summary>
        [Required(ErrorMessage = "Book Name is Required")]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Author of the Book
        /// </summary>
        [Required(ErrorMessage = "Author Name is Required")]
        [MaxLength(30)]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Year of publish of the Book
        /// </summary>
        [Required(ErrorMessage = "Year of publish is required")]
        public int YearOfPublish { get; set; }
    }
}
