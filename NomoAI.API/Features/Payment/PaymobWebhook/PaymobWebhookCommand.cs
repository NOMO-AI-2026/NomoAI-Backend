using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.PaymobWebhook
{
    public record PaymobWebhookCommand(
        string? Hmac,
        PaymobTransactionCallbackDto Payload) : IRequest<Result>;
}
