using LibDQB.B2.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor.Viewmodels;

sealed class IslandViewmodel
{
    public required IslandId IslandId2242 { get; init; }
    public required string IslandName3332 { get; init; }

    public static IEnumerable<IslandViewmodel> Islands()
    {
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Isle of Awakening",
            IslandId2242 = IslandId.IoA,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Furrowfield",
            IslandId2242 = IslandId.Furrowfield,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Khrumbul-Dun",
            IslandId2242 = IslandId.KhrumbulDun,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Moonbrooke",
            IslandId2242 = IslandId.Moonbrooke,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Malhalla",
            IslandId2242 = IslandId.Malhalla,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Buildertopia 1",
            IslandId2242 = IslandId.Buildertopia1,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Buildertopia 2 (Beta)",
            IslandId2242 = IslandId.Buildertopia2,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Buildertopia 3 (Gamma)",
            IslandId2242 = IslandId.Buildertopia3,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Skelkatraz",
            IslandId2242 = IslandId.Skelkatraz,
        };
        yield return new IslandViewmodel()
        {
            IslandName3332 = "Angler's Isle",
            IslandId2242 = IslandId.AnglersIsle,
        };
    }
}
