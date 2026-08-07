using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NomoAI.API.Infrastructure.PayMob.Models
{
    public class CreateAuthTokenResponse
    {
        [JsonPropertyName("profile")]
        public ProfileDto? Profile { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class ProfileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("profile_type")]
        public string? ProfileType { get; set; }

        [JsonPropertyName("phones")]
        public List<string>? Phones { get; set; }

        [JsonPropertyName("company_emails")]
        public List<string>? CompanyEmails { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("email_notification")]
        public bool EmailNotification { get; set; }

        [JsonPropertyName("order_retrieval_endpoint")]
        public string? OrderRetrievalEndpoint { get; set; }

        [JsonPropertyName("delivery_update_endpoint")]
        public string? DeliveryUpdateEndpoint { get; set; }

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("is_mobadra")]
        public bool IsMobadra { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("is_2fa_enabled")]
        public bool Is2FaEnabled { get; set; }

        [JsonPropertyName("otp_sent_to")]
        public string? OtpSentTo { get; set; }

        [JsonPropertyName("dashboard_merchant_status")]
        public string? DashboardMerchantStatus { get; set; }

        [JsonPropertyName("activation_method")]
        public int? ActivationMethod { get; set; }

        [JsonPropertyName("signed_up_through")]
        public int? SignedUpThrough { get; set; }

        [JsonPropertyName("failed_attempts")]
        public int? FailedAttempts { get; set; }

        [JsonPropertyName("custom_export_columns")]
        public List<object>? CustomExportColumns { get; set; }

        [JsonPropertyName("server_IP")]
        public List<string>? ServerIP { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("primary_phone_number")]
        public string? PrimaryPhoneNumber { get; set; }

        [JsonPropertyName("primary_phone_verified")]
        public bool? PrimaryPhoneVerified { get; set; }

        [JsonPropertyName("is_temp_password")]
        public bool? IsTempPassword { get; set; }

        [JsonPropertyName("otp_2fa_sent_at")]
        public DateTime? Otp2FaSentAt { get; set; }

        [JsonPropertyName("otp_2fa_attempt")]
        public int? Otp2FaAttempt { get; set; }

        [JsonPropertyName("otp_sent_at")]
        public DateTime? OtpSentAt { get; set; }

        [JsonPropertyName("otp_validated_at")]
        public DateTime? OtpValidatedAt { get; set; }

        [JsonPropertyName("awb_banner")]
        public string? AwbBanner { get; set; }

        [JsonPropertyName("email_banner")]
        public string? EmailBanner { get; set; }

        [JsonPropertyName("identification_number")]
        public string? IdentificationNumber { get; set; }

        [JsonPropertyName("delivery_status_callback")]
        public string? DeliveryStatusCallback { get; set; }

        [JsonPropertyName("merchant_external_link")]
        public string? MerchantExternalLink { get; set; }

        [JsonPropertyName("merchant_status")]
        public int? MerchantStatus { get; set; }

        [JsonPropertyName("deactivated_by_bank")]
        public bool? DeactivatedByBank { get; set; }

        [JsonPropertyName("bank_deactivation_reason")]
        public string? BankDeactivationReason { get; set; }

        [JsonPropertyName("bank_merchant_status")]
        public int? BankMerchantStatus { get; set; }

        [JsonPropertyName("national_id")]
        public string? NationalId { get; set; }

        [JsonPropertyName("super_agent")]
        public object? SuperAgent { get; set; }

        [JsonPropertyName("wallet_limit_profile")]
        public object? WalletLimitProfile { get; set; }

        [JsonPropertyName("address")]
        public object? Address { get; set; }

        [JsonPropertyName("commercial_registration")]
        public object? CommercialRegistration { get; set; }

        [JsonPropertyName("commercial_registration_area")]
        public object? CommercialRegistrationArea { get; set; }

        [JsonPropertyName("distributor_code")]
        public string? DistributorCode { get; set; }

        [JsonPropertyName("distributor_branch_code")]
        public string? DistributorBranchCode { get; set; }

        [JsonPropertyName("allow_terminal_order_id")]
        public bool? AllowTerminalOrderId { get; set; }

        [JsonPropertyName("allow_encryption_bypass")]
        public bool? AllowEncryptionBypass { get; set; }

        [JsonPropertyName("wallet_phone_number")]
        public string? WalletPhoneNumber { get; set; }

        [JsonPropertyName("suspicious")]
        public int? Suspicious { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("bank_staffs")]
        public Dictionary<string, object>? BankStaffs { get; set; }

        [JsonPropertyName("bank_rejection_reason")]
        public string? BankRejectionReason { get; set; }

        [JsonPropertyName("bank_received_documents")]
        public bool? BankReceivedDocuments { get; set; }

        [JsonPropertyName("bank_merchant_digital_status")]
        public int? BankMerchantDigitalStatus { get; set; }

        [JsonPropertyName("bank_digital_rejection_reason")]
        public string? BankDigitalRejectionReason { get; set; }

        [JsonPropertyName("filled_business_data")]
        public bool? FilledBusinessData { get; set; }

        [JsonPropertyName("day_start_time")]
        public string? DayStartTime { get; set; }

        [JsonPropertyName("day_end_time")]
        public string? DayEndTime { get; set; }

        [JsonPropertyName("withhold_transfers")]
        public bool? WithholdTransfers { get; set; }

        [JsonPropertyName("manual_settlement")]
        public bool? ManualSettlement { get; set; }

        [JsonPropertyName("sms_sender_name")]
        public string? SmsSenderName { get; set; }

        [JsonPropertyName("withhold_transfers_reason")]
        public string? WithholdTransfersReason { get; set; }

        [JsonPropertyName("withhold_transfers_notes")]
        public string? WithholdTransfersNotes { get; set; }

        [JsonPropertyName("can_bill_deposit_with_card")]
        public bool? CanBillDepositWithCard { get; set; }

        [JsonPropertyName("can_topup_merchants")]
        public bool? CanTopupMerchants { get; set; }

        [JsonPropertyName("topup_transfer_id")]
        public object? TopupTransferId { get; set; }

        [JsonPropertyName("referral_eligible")]
        public bool? ReferralEligible { get; set; }

        [JsonPropertyName("is_eligible_to_be_ranger")]
        public bool? IsEligibleToBeRanger { get; set; }

        [JsonPropertyName("eligible_for_manual_refunds")]
        public bool? EligibleForManualRefunds { get; set; }

        [JsonPropertyName("is_ranger")]
        public bool? IsRanger { get; set; }

        [JsonPropertyName("is_poaching")]
        public bool? IsPoaching { get; set; }

        [JsonPropertyName("first_transaction_date")]
        public DateTime? FirstTransactionDate { get; set; }

        [JsonPropertyName("paymob_app_merchant")]
        public bool? PaymobAppMerchant { get; set; }

        [JsonPropertyName("settlement_frequency")]
        public object? SettlementFrequency { get; set; }

        [JsonPropertyName("day_of_the_week")]
        public object? DayOfTheWeek { get; set; }

        [JsonPropertyName("day_of_the_month")]
        public object? DayOfTheMonth { get; set; }

        [JsonPropertyName("allow_transaction_notifications")]
        public bool? AllowTransactionNotifications { get; set; }

        [JsonPropertyName("allow_transfer_notifications")]
        public bool? AllowTransferNotifications { get; set; }

        [JsonPropertyName("sallefny_amount_whole")]
        public double? SallefnyAmountWhole { get; set; }

        [JsonPropertyName("sallefny_fees_whole")]
        public double? SallefnyFeesWhole { get; set; }

        [JsonPropertyName("paymob_app_first_login")]
        public DateTime? PaymobAppFirstLogin { get; set; }

        [JsonPropertyName("paymob_app_last_activity")]
        public DateTime? PaymobAppLastActivity { get; set; }

        [JsonPropertyName("payout_enabled")]
        public bool? PayoutEnabled { get; set; }

        [JsonPropertyName("payout_terms")]
        public bool? PayoutTerms { get; set; }

        [JsonPropertyName("is_bills_new")]
        public bool? IsBillsNew { get; set; }

        [JsonPropertyName("can_process_multiple_refunds")]
        public bool? CanProcessMultipleRefunds { get; set; }

        [JsonPropertyName("settlement_classification")]
        public int? SettlementClassification { get; set; }

        [JsonPropertyName("vat_classification")]
        public int? VatClassification { get; set; }

        [JsonPropertyName("instant_settlement_enabled")]
        public bool? InstantSettlementEnabled { get; set; }

        [JsonPropertyName("instant_settlement_transaction_otp_verified")]
        public bool? InstantSettlementTransactionOtpVerified { get; set; }

        [JsonPropertyName("preferred_language")]
        public string? PreferredLanguage { get; set; }

        [JsonPropertyName("ignore_flash_callbacks")]
        public bool? IgnoreFlashCallbacks { get; set; }

        [JsonPropertyName("receive_callback_card_info")]
        public bool? ReceiveCallbackCardInfo { get; set; }

        [JsonPropertyName("onboarding_freshdesk_ticket_id")]
        public object? OnboardingFreshdeskTicketId { get; set; }

        [JsonPropertyName("paymob_event")]
        public bool? PaymobEvent { get; set; }

        [JsonPropertyName("acq_partner")]
        public object? AcqPartner { get; set; }

        [JsonPropertyName("dom")]
        public object? Dom { get; set; }

        [JsonPropertyName("bank_related")]
        public object? BankRelated { get; set; }

        [JsonPropertyName("settlement_classification_profile")]
        public object? SettlementClassificationProfile { get; set; }

        [JsonPropertyName("permissions")]
        public List<object>? Permissions { get; set; }
    }

    public class UserDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("date_joined")]
        public DateTime? DateJoined { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("is_staff")]
        public bool IsStaff { get; set; }

        [JsonPropertyName("is_superuser")]
        public bool IsSuperuser { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }

        [JsonPropertyName("user_permissions")]
        public List<int>? UserPermissions { get; set; }

        [JsonPropertyName("groups")]
        public List<object>? Groups { get; set; }
    }
}
