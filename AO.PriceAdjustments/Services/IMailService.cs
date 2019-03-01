namespace AO.PriceAdjustments.Services
{
    public interface IMailService
    {
        void SendMail(string subject, string body, string to);
    }
}