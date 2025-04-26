using System;
using System.Security.Cryptography;

namespace Credfeto.Package.Push.Helpers;

internal static class RetryDelayCalculator
{
    private static readonly TimeSpan MinDelay = TimeSpan.FromSeconds(5);

    public static TimeSpan CalculateWithJitter(int attempts, int maxJitterSeconds)
    {
        // do a fast first retry, then exponential backoff
        return attempts <= 1
            ? MinDelay
            : MinDelay + TimeSpan.FromSeconds(WithJitter(CalculateBackoff(attempts), maxSeconds: maxJitterSeconds));
    }

    private static double CalculateBackoff(int attempts)
    {
        return Math.Pow(x: 2, y: attempts);
    }

    private static double WithJitter(double delaySeconds, int maxSeconds)
    {
        double nonJitterPeriod = delaySeconds - maxSeconds;
        double jitterRange = maxSeconds * 2;

        if (nonJitterPeriod < 0)
        {
            jitterRange = delaySeconds;
            nonJitterPeriod = delaySeconds / 2;
        }

        double jitter = CalculateJitterSeconds(jitterRange);

        return nonJitterPeriod + jitter;
    }

    private static double CalculateJitterSeconds(double jitterRange)
    {
        return jitterRange * GetRandom();
    }

    private static double GetRandom()
    {
        using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
        {
            Span<byte> rnd = stackalloc byte[sizeof(uint)];
            randomNumberGenerator.GetBytes(rnd);
            uint random = BitConverter.ToUInt32(value: rnd);

            return random / (double)uint.MaxValue;
        }
    }
}
