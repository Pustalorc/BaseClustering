using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities;

/// <summary>
/// A class with extensions that the plugin utilizes.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Calculates the average center of an <see cref="IEnumerable{Vector3}"/>.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{Vector3}"/> to get the average center from.</param>
    /// <returns>
    /// A <see cref="Vector3"/> that is the average center of <paramref name="source"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="source"/> is null, then this exception is thrown, as <paramref name="source"/> should never be null.
    /// </exception>
    public static Vector3 AverageCenter(this IEnumerable<Vector3> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = source.ToList();

        var sum = Vector3.zero;

        checked
        {
            sum = list.Aggregate(sum, (current, element) => current + element);
        }

        if (list.Count > 0) return sum / list.Count;

        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the average center of an <see cref="IEnumerable{TSource}"/>.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{TSource}"/> to get the average center from.</param>
    /// <param name="selector">The way that the <paramref name="source"/> should be selected to convert it to an <see cref="IEnumerable{Vector3}"/>.</param>
    /// <typeparam name="TSource">A type that can select a <see cref="Vector3"/>.</typeparam>
    /// <returns>
    /// A <see cref="Vector3"/> that is the average center of <paramref name="source"/> after applying a <paramref name="selector"/>.
    /// </returns>
    /// <remarks>
    /// This method calls <see cref="Extensions.AverageCenter"/>, which only takes an <see cref="IEnumerable{Vector3}"/>.
    /// <br/>
    /// Therefore any input for this average center should support a selector to a <see cref="Vector3"/>.
    /// </remarks>
    public static Vector3 AverageCenter<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
    {
        return source.Select(selector).AverageCenter();
    }

    /// <summary>
    /// Checks if any element from <paramref name="args"/> is equal to the <see cref="string"/> <paramref name="include"/>.
    /// </summary>
    /// <param name="args">The <see cref="IEnumerable{String}"/> that should be searched.</param>
    /// <param name="include">The <see cref="string"/> that we should find.</param>
    /// <param name="index">
    /// If <paramref name="include"/> is found in <paramref name="args"/>, this will be a number greater than -1 but smaller than args.Count
    /// <br/>
    /// If <paramref name="include"/> isn't found in <paramref name="args"/>, this will be -1.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="index"/> is > -1.
    /// <br/>
    /// <see langword="false"/> if <paramref name="index"/> is == -1.
    /// </returns>
    public static bool CheckArgsIncludeString(this IEnumerable<string> args, string include, out int index)
    {
        index = args.ToList().FindIndex(k => k.Equals(include, StringComparison.OrdinalIgnoreCase));
        return index > -1;
    }

    /// <summary>
    /// Gets all the of <see cref="ItemAsset"/>s that an element of <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The <see cref="IEnumerable{String}"/> that should be searched.</param>
    /// <param name="index">
    /// If any element of <paramref name="args"/> can be an <see cref="ItemAsset"/>(s), this will be a number greater than -1 but smaller than args.Count
    /// <br/>
    /// If no elements of <paramref name="args"/> can be an <see cref="ItemAsset"/>(s), this will be -1.
    /// </param>
    /// <returns>
    /// An empty <see cref="List{ItemAsset}"/> if no element in <paramref name="args"/> can be an <see cref="ItemAsset"/>.
    /// <br/>
    /// A <see cref="List{ItemAsset}"/> with all the <see cref="ItemAsset"/>s from one of the entries in <paramref name="args"/>.
    /// </returns>
    public static List<ItemAsset> GetMultipleItemAssets(this IEnumerable<string> args, out int index)
    {
        var argsL = args.ToList();
        var assets = Assets.find(EAssetType.ITEM).Cast<ItemAsset>()
            .Where(k => k is { itemName: not null, name: not null }).OrderBy(k => k.itemName.Length).ToList();

        for (index = 0; index < argsL.Count; index++)
        {
            var itemAssets = assets.Where(k =>
                argsL[0].Equals(k.id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                argsL[0].Split(' ').All(l => k.itemName.ToLower().Contains(l)) ||
                argsL[0].Split(' ').All(l => k.name.ToLower().Contains(l))).ToList();

            if (itemAssets.Count <= 0)
                continue;

            return itemAssets;
        }

        index = -1;
        return new List<ItemAsset>();
    }

    /// <summary>
    /// Gets a <see cref="float"/> from an element in <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The <see cref="IEnumerable{String}"/> that should be searched.</param>
    /// <param name="index">
    /// If any element of <paramref name="args"/> is a valid <see cref="float"/>, this will be a number greater than -1 but smaller than args.Count
    /// <br/>
    /// If no elements of <paramref name="args"/> is a valid <see cref="float"/>, this will be -1.
    /// </param>
    /// <returns>
    /// A <see cref="float"/> from one of the entries in <paramref name="args"/>.
    /// </returns>
    public static float GetFloat(this IEnumerable<string> args, out int index)
    {
        var output = float.NegativeInfinity;
        index = args.ToList().FindIndex(k => float.TryParse(k, out output));
        return output;
    }

    /// <summary>
    /// Gets a <see cref="float"/> from an element in <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The <see cref="IEnumerable{String}"/> that should be searched.</param>
    /// <param name="index">
    /// If any element of <paramref name="args"/> is a valid <see cref="IRocketPlayer"/>, this will be a number greater than -1 but smaller than args.Count
    /// <br/>
    /// If no elements of <paramref name="args"/> is a valid <see cref="IRocketPlayer"/>, this will be -1.
    /// </param>
    /// <returns>
    /// <see langword="null"/> if none of the entries in <paramref name="args"/> can be an <see cref="IRocketPlayer"/>.
    /// <br/>
    /// A <see cref="IRocketPlayer"/> from one of the entries in <paramref name="args"/>.
    /// </returns>
    public static IRocketPlayer? GetIRocketPlayer(this IEnumerable<string> args, out int index)
    {
        IRocketPlayer? output = null;
        index = args.ToList().FindIndex(k =>
        {
            output = UnturnedPlayer.FromName(k);
            if (output == null && ulong.TryParse(k, out var id) && id > 76561197960265728)
                output = new RocketPlayer(id.ToString());

            return output != null;
        });
        return output;
    }

    /// <summary>
    /// Compares a <see cref="Vector3"/> to see if it is a <see cref="Vector3.negativeInfinity"/>, as default comparison doesn't correctly check it.
    /// </summary>
    /// <param name="vector">The <see cref="Vector3"/> to compare to negative infinity.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="vector"/> has any <see cref="float.NegativeInfinity"/> component.
    /// <br/>
    /// <see langword="false"/> if <paramref name="vector"/> has no <see cref="float.NegativeInfinity"/> component.
    /// </returns>
    public static bool IsNegativeInfinity(this Vector3 vector)
    {
        return float.IsNegativeInfinity(vector.x) || float.IsNegativeInfinity(vector.y) ||
               float.IsNegativeInfinity(vector.z);
    }
}