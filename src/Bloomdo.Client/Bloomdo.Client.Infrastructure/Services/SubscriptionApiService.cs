using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Subscription;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Infrastructure.Services;

public class SubscriptionApiService(HttpClient httpClient) : ISubscriptionApiService
{
    public async Task<SubscriptionStatusResponse?> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(ApiRoutes.Subscription.Status, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>(ct);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetSubscriptionStatus failed: {ex.Message}");
            return null;
        }
    }

    public async Task<CreateCheckoutSessionResponse?> CreateCheckoutSessionAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateCheckoutSessionRequest { Plan = plan };
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Subscription.Checkout, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<CreateCheckoutSessionResponse>(ct);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateCheckoutSession failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(ApiRoutes.Subscription.Cancel, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CancelSubscription failed: {ex.Message}");
            return false;
        }
    }
}
