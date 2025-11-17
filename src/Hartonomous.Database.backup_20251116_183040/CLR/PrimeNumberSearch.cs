using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Newtonsoft.Json;

namespace Hartonomous.Clr
{
    public static class PrimeNumberSearch
    {
        /// <summary>
        /// Finds all prime numbers within a given range (inclusive).
        /// Uses a basic trial division method, suitable for demonstrating a long-running CPU-bound task.
        /// </summary>
        /// <param name="rangeStart">The starting number of the range to check.</param>
        /// <param name="rangeEnd">The ending number of the range to check.</param>
        /// <returns>A JSON string representing an array of the prime numbers found.</returns>
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlString clr_FindPrimes(SqlInt64 rangeStart, SqlInt64 rangeEnd)
        {
            if (rangeStart.IsNull || rangeEnd.IsNull || rangeStart.Value > rangeEnd.Value)
            {
                return new SqlString("[]");
            }

            long start = rangeStart.Value;
            long end = rangeEnd.Value;
            var primes = new List<long>();

            if (start < 2) start = 2;

            for (long i = start; i <= end; i++)
            {
                if (IsPrime(i))
                {
                    primes.Add(i);
                }
            }

            return new SqlString(JsonConvert.SerializeObject(primes));
        }

        /// <summary>
        /// Helper function to check if a number is prime.
        /// </summary>
        private static bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            var boundary = (long)Math.Floor(Math.Sqrt(number));

            for (long i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
