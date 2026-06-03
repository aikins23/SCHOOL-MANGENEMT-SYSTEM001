using System;

namespace Kingdom.Tests
{
    internal static class AssertEx
    {
        public static void True(bool condition, string message = null)
        {
            if (!condition) throw new Exception(message ?? "Expected condition to be true.");
        }

        public static void False(bool condition, string message = null)
        {
            if (condition) throw new Exception(message ?? "Expected condition to be false.");
        }

        public static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(message ?? $"Expected '{expected}', got '{actual}'.");
            }
        }

        public static void NotEqual<T>(T notExpected, T actual, string message = null)
        {
            if (object.Equals(notExpected, actual))
            {
                throw new Exception(message ?? $"Did not expect '{actual}'.");
            }
        }

        public static void Null(object value, string message = null)
        {
            if (value != null) throw new Exception(message ?? $"Expected null, got '{value}'.");
        }

        public static void NotNull(object value, string message = null)
        {
            if (value == null) throw new Exception(message ?? "Expected a non-null value.");
        }

        public static void Empty(string value, string message = null)
        {
            if (!string.IsNullOrEmpty(value))
            {
                throw new Exception(message ?? $"Expected empty string, got '{value}'.");
            }
        }

        public static void Contains(string expectedSubstring, string actual, string message = null)
        {
            if (actual == null || actual.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new Exception(message ?? $"Expected '{actual}' to contain '{expectedSubstring}'.");
            }
        }

        public static T Throws<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new Exception(message ?? $"Expected exception {typeof(T).Name}, got {ex.GetType().Name}.");
            }

            throw new Exception(message ?? $"Expected exception {typeof(T).Name}, but no exception was thrown.");
        }
    }
}
