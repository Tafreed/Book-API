namespace Book_API.Models
{
    /// <summary>
    /// A DTO for the Book Entity
    /// </summary>
    public class BookDto
    {
        /// <summary>
        /// The Book Id Unique for each Book
        /// </summary>
        public int BookId { get; set; }

        /// <summary>
        /// Name of the Book
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Author of the Book
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Year of Publish of the book
        /// </summary>
        public int YearOfPublish { get; set; }
    }
}
