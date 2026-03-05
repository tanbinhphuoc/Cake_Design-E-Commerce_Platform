namespace Application.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(string txnRef, decimal amount, string orderInfo, string ipAddress);
        bool ValidateCallback(Dictionary<string, string> vnpayData);
        string GetResponseCode(Dictionary<string, string> vnpayData);
    }
}
