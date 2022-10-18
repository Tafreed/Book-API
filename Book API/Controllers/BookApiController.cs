using AutoMapper;
using Book_API.Entities;
using Book_API.Models;
using Book_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Book_API.Controllers
{
    [Route("api/")]
    [Authorize]
    [ApiVersion("1.0")]
    [ApiController]
    public class BookApiController : ControllerBase
    {
        const int maxPageSize = 20;
        private readonly IBookInfo _bookInfoRepository;
        private readonly ILogger _logger;
        private readonly IMailService _mailService;
        private readonly IMapper _mapper;

        public BookApiController(ILogger<BookApiController> logger,IBookInfo bookInfoRepository,IMailService mailService, IMapper mapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _bookInfoRepository = bookInfoRepository  ?? 
                throw new ArgumentNullException(nameof(bookInfoRepository));
        }

        /// <summary>
        /// Get All Books
        /// </summary>
        /// <param name="name">Name of the Book</param>
        /// <param name="searchQuery">Searching Criteria</param>
        /// <param name="pageNumber">Page Number</param>
        /// <param name="pageSize">Page Size (maximum 10)</param>
        /// <response code = "200">Returns the requested city</response>>
        /// <returns></returns>
        [HttpGet("books")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
            [FromQuery] string? name, string? searchQuery, int pageNumber=1, int pageSize=10)
        {
            if(pageSize > maxPageSize) pageSize = maxPageSize;

            var (books,paginationMetadata) = await _bookInfoRepository.GetBooksAsync(name,searchQuery,pageNumber,pageSize);

            Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetadata));
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        /// <summary>
        /// Get a Book by the BookId
        /// </summary>
        /// <param name="id">The Id of the Book</param>
        /// <returns></returns>
        [HttpGet("books/{id}")]
        public async Task<ActionResult<BookForCreationDto>> GetBook(int id)
        {

            var book = await _bookInfoRepository.GetBookAsync(id);
            if (book == null)
            {
                _logger.LogInformation($"The book with bookId {id} wasn't found on the server");
                return NotFound();
            }
            return Ok(book);
        }

        /// <summary>
        /// Add a new Book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPost("books")]
        public async Task<ActionResult<BookForCreationDto>> AddBook(BookForCreationDto book)
        {
            await _bookInfoRepository.AddBookAsync(_mapper.Map<Book>(book));

            _mailService.Send(
                "New Book Added.",
                $"A book with title {book.Title} is added to the collection.");
            return Ok(201);
        }

        /// <summary>
        /// Update a existing Book
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="newBook"></param>
        /// <returns></returns>
        [HttpPut("books/{bookId}")]
        public async Task<ActionResult> UpdateBook(int bookId, BookForUpdateDto newBook)
        {
            try
            {
                if(!await _bookInfoRepository.BookExistsAsync(bookId))
                {
                    return NotFound();
                }

                var bookToUpdate = await _bookInfoRepository.GetBookAsync(bookId);
                var book = _mapper.Map<Book>(newBook);

                await _bookInfoRepository.UpdateBookAsync(book, bookToUpdate);
                
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error updating data");
            }
            return NoContent();
        }

        /// <summary>
        /// Delete a book by its BookId
        /// </summary>
        /// <param name="bookId"></param>
        /// <returns></returns>
        [HttpDelete("books/{bookId}")]
        public async Task<ActionResult> DeleteBook(int bookId)
        {
            var bookToDelete = await _bookInfoRepository.GetBookAsync(bookId);
            if (bookToDelete != null)
            {
                await _bookInfoRepository.DeleteBookAsync(bookToDelete);
                return NoContent();
            }
            else
            {
                _logger.LogInformation($"The Book with Id {bookId} is not found");
                return NotFound($"The Book with Id {bookId} is not found");
            };
        }
    }
}
