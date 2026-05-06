using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Shouldly;

namespace SapNwRfcCore.Tests;

internal static class ShouldlyExtensions
{
    /// <summary>
    /// Asserts that the amount of entries in the list is the expected amount.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the list.</typeparam>
    /// <param name="list">The list to check for.</param>
    /// <param name="expected">The expected amount.</param>
    /// <param name="customMessage">A customized message.</param>
    /// <returns>The list for chaining.</returns>
    public static IList<TItem> ShouldHaveCount<TItem>(this IList<TItem> list, int expected, string customMessage = null)
    {
        list.Count.ShouldBe(expected, customMessage);

        return list;
    }

    public static TimeSpan ExecutionTime(this Action action)
    {
        var start = Stopwatch.StartNew();
        action();
        start.Stop();
        return start.Elapsed;
    }

    public static void ShouldBeEquivalentToObject<TExpected, TActual>(this TActual actual, TExpected expected)
    {
        // Get all public properties of the expected type
        var properties = typeof(TExpected).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Get the value of the property from both the expected and actual objects
            var expectedValue = property.GetValue(expected);
            object actualValue = null;
            try
            {
                actualValue = property.GetValue(actual);
            }
            catch (TargetException)
            {
                if (actual.GetType().GetMember(property.Name)?.FirstOrDefault() is FieldInfo field)
                    actualValue = field.GetValue(actual);
            }

            // Use Shouldly's built-in assertion to compare the values
            actualValue.ShouldBe(expectedValue, $"Property '{property.Name}' has a different value.");
        }
    }
}
