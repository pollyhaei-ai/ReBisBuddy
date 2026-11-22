using BisBuddy.Gear;
using BisBuddy.Items;
using BisBuddy.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BisBuddy.Services.Export;

/// <summary>
/// Builds the sheet JSON that https://xivgear.app/?page=importsheet accepts.
/// xivgear resolves items by id (<c>ItemSlotExport.id</c>) and the job by its English abbreviation.
/// Client-language item names are not part of this payload.
/// </summary>
public static class XivgearSheetExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string Build(Gearset gearset, IItemDataService itemData)
    {
        var items = new Dictionary<string, object?>();
        var ringRightUsed = false;
        var equipLevels = new List<int>();

        foreach (var gearpiece in gearset.Gearpieces)
        {
            var slot = slotKey(gearpiece.GearpieceType, ref ringRightUsed);
            if (slot is null || items.ContainsKey(slot))
                continue;

            var rawId = gearpiece.ItemId;
            var isHq = rawId >= Constants.ItemIdHqOffset;
            var baseId = isHq ? rawId - Constants.ItemIdHqOffset : rawId;
            if (baseId == 0)
                continue;

            var equipLevel = itemData.GetItemEquipLevel(baseId);
            if (equipLevel > 0)
                equipLevels.Add(equipLevel);

            var materia = gearpiece.ItemMateria
                .Select(meld => meld.ItemId >= Constants.ItemIdHqOffset
                    ? meld.ItemId - Constants.ItemIdHqOffset
                    : meld.ItemId)
                .Where(id => id > 0)
                .Select(id => new Dictionary<string, uint> { ["id"] = id })
                .ToArray();

            var item = new Dictionary<string, object?>
            {
                ["id"] = baseId,
                ["materia"] = materia,
            };

            // xivgear treats an id as HQ when the item can be HQ, unless forceNq is set.
            if (!isHq && itemData.ItemCanBeHq(baseId))
                item["forceNq"] = true;

            items[slot] = item;
        }

        var job = itemData.GetEnglishClassJobAbbreviation(gearset.ClassJobInfo.ClassJobId);
        var sheet = new Dictionary<string, object?>
        {
            ["name"] = gearset.Name,
            ["level"] = snapLevel(equipLevels.Count == 0 ? 0 : equipLevels.Max()),
            ["partyBonus"] = job.Equals("BLU", StringComparison.OrdinalIgnoreCase) ? 1 : 5,
            ["sets"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["name"] = gearset.Name,
                    ["items"] = items,
                },
            },
        };

        if (!string.IsNullOrEmpty(job))
            sheet["job"] = job;

        return JsonSerializer.Serialize(sheet, JsonOptions);
    }

    /// <summary>
    /// xivgear's import sheet accepts level 90 and 100 for current combat sets.
    /// Equip levels above 90 map to 100. Anything else maps to 90 so the sheet still opens.
    /// </summary>
    private static int snapLevel(int equipLevel) =>
        equipLevel > 90 ? 100 : 90;

    private static string? slotKey(GearpieceType type, ref bool ringRightUsed)
    {
        if (type == GearpieceType.Finger)
        {
            if (!ringRightUsed)
            {
                ringRightUsed = true;
                return "RingRight";
            }

            return "RingLeft";
        }

        return type switch
        {
            GearpieceType.Weapon => "Weapon",
            GearpieceType.OffHand => "OffHand",
            GearpieceType.Head => "Head",
            GearpieceType.Body => "Body",
            GearpieceType.Hands => "Hand",
            GearpieceType.Legs => "Legs",
            GearpieceType.Feet => "Feet",
            GearpieceType.Ears => "Ears",
            GearpieceType.Neck => "Neck",
            GearpieceType.Wrists => "Wrist",
            _ => null,
        };
    }
}
