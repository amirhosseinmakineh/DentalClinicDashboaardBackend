using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace DentalDashboard.ApplicationService.Services
{
    public class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly HttpClient httpClient;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;

        public FirebasePushNotificationService(HttpClient httpClient, IUserRepository userRepository, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.userRepository = userRepository;
            this.configuration = configuration;
        }

        public async Task SendAsync(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            var serverKey = configuration["Firebase:ServerKey"];
            if (string.IsNullOrWhiteSpace(serverKey))
                return;

            var token = await userRepository.GetAll()
                .Where(x => x.Id == userId && !x.IsDeleted)
                .Select(x => x.PushNotificationToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(token))
                return;

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={serverKey}");
            request.Content = JsonContent.Create(new
            {
                to = token,
                notification = new { title, body },
                data = data ?? new Dictionary<string, string>()
            });

            await httpClient.SendAsync(request, cancellationToken);
        }
    }
}
