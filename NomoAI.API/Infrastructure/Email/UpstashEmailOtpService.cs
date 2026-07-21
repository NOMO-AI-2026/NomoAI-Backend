using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Common.Redis;
using StackExchange.Redis;

namespace NomoAI.API.Infrastructure.Email;

public sealed class UpstashEmailOtpService
    : IEmailOtpService
{
    //private const int ReservationDurationSeconds = 30;

    private const string CreateOtpScript = """
        if redis.call('EXISTS', KEYS[2]) == 1 then
            return 0
        end

        redis.call('DEL', KEYS[1])

        redis.call(
            'HSET',
            KEYS[1],
            'hash', ARGV[1],
            'targetEmail', ARGV[2],
            'attempts', '0',
            'maxAttempts', ARGV[3],
            'state', 'active'
        )

        redis.call('EXPIRE', KEYS[1], ARGV[4])

        redis.call(
            'SET',
            KEYS[2],
            '1',
            'EX',
            ARGV[5]
        )

        return 1
        """;

    private const string VerifyOtpScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return { 'NOT_FOUND' }
        end

        local state =
            redis.call('HGET', KEYS[1], 'state')

        local currentTime =
            tonumber(ARGV[3])

        if state == 'processing' then
            local reservationUntil =
                tonumber(
                    redis.call(
                        'HGET',
                        KEYS[1],
                        'reservationUntil'
                    ) or '0'
                )

            if reservationUntil > currentTime then
                return { 'IN_PROGRESS' }
            end

            redis.call(
                'HSET',
                KEYS[1],
                'state',
                'active'
            )

            redis.call(
                'HDEL',
                KEYS[1],
                'reservationToken',
                'reservationUntil'
            )
        end

        local attempts =
            tonumber(
                redis.call(
                    'HGET',
                    KEYS[1],
                    'attempts'
                ) or '0'
            )

        local maxAttempts =
            tonumber(
                redis.call(
                    'HGET',
                    KEYS[1],
                    'maxAttempts'
                ) or '0'
            )

        if attempts >= maxAttempts then
            redis.call('DEL', KEYS[1])

            return { 'ATTEMPTS_EXCEEDED' }
        end

        local expectedHash =
            redis.call(
                'HGET',
                KEYS[1],
                'hash'
            )

        if expectedHash ~= ARGV[1] then
            attempts =
                redis.call(
                    'HINCRBY',
                    KEYS[1],
                    'attempts',
                    1
                )

            if attempts >= maxAttempts then
                redis.call('DEL', KEYS[1])

                return { 'ATTEMPTS_EXCEEDED' }
            end

            return { 'INVALID' }
        end

        local reservationUntil =
            currentTime + tonumber(ARGV[4])

        redis.call(
            'HSET',
            KEYS[1],
            'state', 'processing',
            'reservationToken', ARGV[2],
            'reservationUntil', reservationUntil
        )

        local targetEmail =
            redis.call(
                'HGET',
                KEYS[1],
                'targetEmail'
            )

        return { 'OK', targetEmail }
        """;

    private const string ConsumeOtpScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return 0
        end

        local storedToken =
            redis.call(
                'HGET',
                KEYS[1],
                'reservationToken'
            )

        if storedToken ~= ARGV[1] then
            return 0
        end

        redis.call('DEL', KEYS[1])

        return 1
        """;

    private const string ReleaseOtpScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return 0
        end

        local storedToken =
            redis.call(
                'HGET',
                KEYS[1],
                'reservationToken'
            )

        if storedToken ~= ARGV[1] then
            return 0
        end

        redis.call(
            'HSET',
            KEYS[1],
            'state',
            'active'
        )

        redis.call(
            'HDEL',
            KEYS[1],
            'reservationToken',
            'reservationUntil'
        )

        return 1
        """;

    private readonly IDatabase _database;
    private readonly EmailOtpOptions _otpOptions;
    private readonly UpstashRedisOptions _redisOptions;
    private readonly ILogger<UpstashEmailOtpService> _logger;

    public UpstashEmailOtpService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<EmailOtpOptions> otpOptions,
        IOptions<UpstashRedisOptions> redisOptions,
        ILogger<UpstashEmailOtpService> logger)
    {
        _database =
            connectionMultiplexer.GetDatabase();

        _otpOptions =
            otpOptions.Value;

        _redisOptions =
            redisOptions.Value;

        _logger =
            logger;
    }

    public async Task<Result<EmailOtpCreated>> CreateAsync(
        string userId,
        string targetEmail,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedUserId =
            userId.Trim();

        string normalizedTargetEmail =
            targetEmail.Trim();

        string otp =
            GenerateOtp();

        string otpHash =
            ComputeHash(
                normalizedUserId,
                purpose,
                otp);

        DateTime now =
            DateTime.UtcNow;

        RedisKey otpKey =
            BuildOtpKey(
                normalizedUserId,
                purpose);

        RedisKey cooldownKey =
            BuildCooldownKey(
                normalizedUserId,
                purpose);

        try
        {
            RedisResult scriptResult =
                await _database.ScriptEvaluateAsync(
                    CreateOtpScript,
                    new RedisKey[]
                    {
                        otpKey,
                        cooldownKey
                    },
                    new RedisValue[]
                    {
                        otpHash,
                        normalizedTargetEmail,
                        _otpOptions.MaxAttempts,
                        _otpOptions.ExpirationMinutes * 60,
                        _otpOptions.ResendCooldownSeconds
                    });

            long created =
                (long)scriptResult;

            if (created == 0)
            {
                return Result.Failure<EmailOtpCreated>(
                    EmailOtpErrors.ResendTooSoon);
            }

            return Result.Success(
                new EmailOtpCreated(
                    otp,
                    now.AddMinutes(
                        _otpOptions.ExpirationMinutes),
                    now.AddSeconds(
                        _otpOptions.ResendCooldownSeconds)));
        }
        catch (RedisConnectionException exception)
        {
            LogRedisFailure(
                exception,
                "create",
                normalizedUserId,
                purpose);

            return Result.Failure<EmailOtpCreated>(
                EmailOtpErrors.ServiceUnavailable);
        }
        catch (RedisTimeoutException exception)
        {
            LogRedisFailure(
                exception,
                "create",
                normalizedUserId,
                purpose);

            return Result.Failure<EmailOtpCreated>(
                EmailOtpErrors.ServiceUnavailable);
        }
    }

    public async Task<Result<VerifiedEmailOtp>>
        VerifyAndReserveAsync(
            string userId,
            string otp,
            EmailOtpPurpose purpose,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedUserId =
            userId.Trim();

        string submittedHash =
            ComputeHash(
                normalizedUserId,
                purpose,
                otp.Trim());

        string reservationToken =
            Guid.NewGuid().ToString("N");

        long currentUnixTime =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        RedisKey otpKey =
            BuildOtpKey(
                normalizedUserId,
                purpose);

        try
        {
                    RedisResult scriptResult =
             await _database.ScriptEvaluateAsync(
                 VerifyOtpScript,
                 new RedisKey[]
                 {
                    otpKey
                 },
                 new RedisValue[]
                 {
                    submittedHash,
                    reservationToken,
                    currentUnixTime,
                    _otpOptions.ExpirationMinutes * 60
                 });

            RedisResult[] values =
                (RedisResult[])scriptResult;

            string status =
                values[0].ToString();

            switch (status)
            {
                case "OK":
                    string targetEmail =
                        values[1].ToString();

                    return Result.Success(
                        new VerifiedEmailOtp(
                            normalizedUserId,
                            targetEmail,
                            purpose,
                            reservationToken));

                case "ATTEMPTS_EXCEEDED":
                    return Result.Failure<
                        VerifiedEmailOtp>(
                        EmailOtpErrors.AttemptsExceeded);

                case "IN_PROGRESS":
                    return Result.Failure<
                        VerifiedEmailOtp>(
                        EmailOtpErrors
                            .VerificationInProgress);

                case "INVALID":
                case "NOT_FOUND":
                default:
                    return Result.Failure<
                        VerifiedEmailOtp>(
                        EmailOtpErrors
                            .InvalidOrExpired);
            }
        }
        catch (RedisConnectionException exception)
        {
            LogRedisFailure(
                exception,
                "verify",
                normalizedUserId,
                purpose);

            return Result.Failure<VerifiedEmailOtp>(
                EmailOtpErrors.ServiceUnavailable);
        }
        catch (RedisTimeoutException exception)
        {
            LogRedisFailure(
                exception,
                "verify",
                normalizedUserId,
                purpose);

            return Result.Failure<VerifiedEmailOtp>(
                EmailOtpErrors.ServiceUnavailable);
        }
    }

    public async Task ConsumeAsync(
        string userId,
        EmailOtpPurpose purpose,
        string reservationToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _database.ScriptEvaluateAsync(
                ConsumeOtpScript,
                new RedisKey[]
                {
                    BuildOtpKey(
                        userId.Trim(),
                        purpose)
                },
                new RedisValue[]
                {
                    reservationToken
                });
        }
        catch (RedisConnectionException exception)
        {
            LogRedisFailure(
                exception,
                "consume",
                userId,
                purpose);

            throw;
        }
        catch (RedisTimeoutException exception)
        {
            LogRedisFailure(
                exception,
                "consume",
                userId,
                purpose);

            throw;
        }
    }

    public async Task ReleaseAsync(
        string userId,
        EmailOtpPurpose purpose,
        string reservationToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _database.ScriptEvaluateAsync(
                ReleaseOtpScript,
                new RedisKey[]
                {
                    BuildOtpKey(
                        userId.Trim(),
                        purpose)
                },
                new RedisValue[]
                {
                    reservationToken
                });
        }
        catch (RedisConnectionException exception)
        {
            LogRedisFailure(
                exception,
                "release",
                userId,
                purpose);
        }
        catch (RedisTimeoutException exception)
        {
            LogRedisFailure(
                exception,
                "release",
                userId,
                purpose);
        }
    }

    private string GenerateOtp()
    {
        int minimum =
            (int)Math.Pow(
                10,
                _otpOptions.Length - 1);

        int maximum =
            (int)Math.Pow(
                10,
                _otpOptions.Length);

        int value =
            RandomNumberGenerator.GetInt32(
                minimum,
                maximum);

        return value.ToString(
            CultureInfo.InvariantCulture);
    }

    private string ComputeHash(
        string userId,
        EmailOtpPurpose purpose,
        string otp)
    {
        string payload =
            $"{userId}|{(int)purpose}|{otp}";

        byte[] key =
            Encoding.UTF8.GetBytes(
                _otpOptions.HashKey);

        byte[] payloadBytes =
            Encoding.UTF8.GetBytes(
                payload);

        using var hmac =
            new HMACSHA256(key);

        byte[] hash =
            hmac.ComputeHash(
                payloadBytes);

        return Convert.ToHexString(hash);
    }

    private RedisKey BuildOtpKey(
        string userId,
        EmailOtpPurpose purpose)
    {
        string purposeName =
            purpose switch
            {
                EmailOtpPurpose.ConfirmEmail =>
                    "confirm-email",

                EmailOtpPurpose.ChangeEmail =>
                    "change-email",
                EmailOtpPurpose.ResetPassword =>
                    "reset-password",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(purpose),
                    purpose,
                    "Unsupported email OTP purpose.")
            };

        return
            $"{_redisOptions.InstancePrefix}" +
            $"email-otp:{purposeName}:{userId}";
    }

    private RedisKey BuildCooldownKey(
     string userId,
     EmailOtpPurpose purpose)
    {
        string purposeName =
            purpose switch
            {
                EmailOtpPurpose.ConfirmEmail =>
                    "confirm-email",

                EmailOtpPurpose.ChangeEmail =>
                    "change-email",

                EmailOtpPurpose.ResetPassword =>
                    "reset-password",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(purpose),
                    purpose,
                    "Unsupported email OTP purpose.")
            };

        return
            $"{_redisOptions.InstancePrefix}" +
            $"email-otp:cooldown:{purposeName}:{userId}";
    }

    private void LogRedisFailure(
        Exception exception,
        string operation,
        string userId,
        EmailOtpPurpose purpose)
    {
        _logger.LogError(
            exception,
            "Redis OTP operation {Operation} failed " +
            "for user {UserId} and purpose {Purpose}.",
            operation,
            userId,
            purpose);
    }
}