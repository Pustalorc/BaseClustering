extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using UnityEngine;

namespace Pustalorc.Libraries.BaseClustering.API.Utilities;

/// <summary>
///     A class with extensions to calculate the average center.
/// </summary>
[PublicAPI]
public static class AverageCenterExtensions
{
    /// <summary>
    ///     Calculates the average center of an <see cref="IEnumerable{Vector3}" />.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{Vector3}" /> to get the average center from.</param>
    /// <returns>
    ///     A <see cref="Vector3" /> that is the average center of <paramref name="source" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     If <paramref name="source" /> is null, then this exception is thrown, as <paramref name="source" /> should never be
    ///     null.
    /// </exception>
    public static Vector3 AverageCenter(this IEnumerable<Vector3> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = source.ToList();

        var sum = Vector3.zero;

        checked
        {
            sum = list.Aggregate(sum, static (current, element) => current + element);
        }

        if (list.Count > 0) return sum / list.Count;

        return Vector3.zero;
    }

    /// <summary>
    ///     Calculates the average center of an <see cref="IEnumerable{TSource}" />.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{TSource}" /> to get the average center from.</param>
    /// <param name="selector">
    ///     The way that the <paramref name="source" /> should be selected to convert it to an
    ///     <see cref="IEnumerable{Vector3}" />.
    /// </param>
    /// <typeparam name="TSource">A type that can select a <see cref="Vector3" />.</typeparam>
    /// <returns>
    ///     A <see cref="Vector3" /> that is the average center of <paramref name="source" /> after applying a
    ///     <paramref name="selector" />.
    /// </returns>
    /// <remarks>
    ///     This method calls <see cref="AverageCenter" />, which only takes an <see cref="IEnumerable{Vector3}" />.
    ///     <br />
    ///     Therefore, any input for this average center should support a selector to a <see cref="Vector3" />.
    /// </remarks>
    public static Vector3 AverageCenter<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
    {
        return source.Select(selector).AverageCenter();
    }
}