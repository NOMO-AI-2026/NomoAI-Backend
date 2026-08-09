using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment
{
    public static class PaymentErrors
    {
        public static readonly Error DoctorNotFound = new(
            "Payment.DoctorNotFound",
            "Doctor not found.",
            404);

        public static readonly Error DoctorNotApproved = new(
            "Payment.DoctorNotApproved",
            "Doctor account has not been approved.",
            403);

        public static readonly Error PlanNotFound = new(
            "Payment.PlanNotFound",
            "Subscription plan not found.",
            404);

        public static readonly Error PaymentMethodNotFound = new(
            "Payment.PaymentMethodNotFound",
            "Payment method not found.",
            404);

        public static readonly Error PayMobFailed = new(
            "Payment.PayMobFailed",
            "Failed to create payment link with PayMob.",
            502);

        public static readonly Error IdempotencyConflict = new(
            "Payment.IdempotencyConflict",
            "A payment with this idempotency key is already being processed.",
            409);

        public static readonly Error InvalidHmac = new(
            "Payment.InvalidHmac",
            "Invalid or missing PayMob HMAC signature.",
            401);

        public static readonly Error PaymentNotFound = new(
            "Payment.PaymentNotFound",
            "Payment not found for the given reference.",
            404);

        public static readonly Error InvalidCallback = new(
            "Payment.InvalidCallback",
            "PayMob callback payload is invalid or missing required fields.",
            400);
    }
}
