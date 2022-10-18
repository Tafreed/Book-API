namespace Book_API.Services
{
    public interface IMailService
    {
        void Send(string subject, string message);
    }
}