namespace DentalDashboard.Domain.Exceptions
{
    public class DomainException : Exception
    {
        private readonly string message;
        public DomainException(string message) 
        {
            this.message = message;
        }
    }
}
