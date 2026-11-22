using BisBuddy.Gear;
using BisBuddy.Gear.Melds;
using BisBuddy.Gear.Prerequisites;
using BisBuddy.Mappers;
using BisBuddy.Services;
using BisBuddy.Services.ItemData;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SheetMateria = Lumina.Excel.Sheets.Materia;

namespace BisBuddy.Items
{
    public partial class ItemDataService : IItemDataService
    {
        private readonly ITypedLogger<ItemDataService> logger;
        private readonly IDataManager dataManager;
        private readonly IMapper<string, GearpieceType> gearpieceTypeMapper;

        private ILookup<uint, (uint ItemId, CofferSourceType SourceType)>? itemsCoffers = null;
        private ILookup<uint, (List<uint> ItemIds, uint SourceShopId)>? itemsPrerequisites = null;

        public static readonly int MaxItemPrerequisites = 25;
        private ExcelSheet<Item> ItemSheet { get; init; }
        private ExcelSheet<Item>? ItemSheetEn { get; init; }
        private ExcelSheet<ItemUICategory>? ItemUiCategorySheetEn { get; init; }
        private ExcelSheet<SpecialShop> ShopSheet { get; init; }
        private ExcelSheet<SheetMateria> Materia { get; init; }
        private ExcelSheet<ClassJob> ClassJobSheet { get; init; }
        private ExcelSheet<ClassJob>? ClassJobSheetEn { get; init; }
        public ILookup<uint, (uint ItemId, CofferSourceType SourceType)> ItemsCoffers
        {
            get
            {
                if (itemsCoffers == null)
                {
                    if (ItemSheetEn is null)
                    {
                        logger.Error("English Item sheet is unavailable. Coffer prerequisites were not generated.");
                        itemsCoffers = emptyCoffers();
                    }
                    else
                    {
                        itemsCoffers = generateItemsCoffers(ItemSheetEn);
                    }
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        itemsCoffers = null;
                    });
                }
                return itemsCoffers;
            }
        }
        public ILookup<uint, (List<uint> ItemIds, uint SourceShopId)> ItemsPrerequisites
        {
            get
            {
                if (itemsPrerequisites == null)
                {
                    itemsPrerequisites = generateItemsPrerequisites();
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        itemsPrerequisites = null;
                    });
                }
                return itemsPrerequisites;
            }
        }
        private Dictionary<string, uint> NameToId { get; init; }
        private Dictionary<uint, MateriaDetails> MateriaDetailsCache { get; init; } = [];
        private Dictionary<(uint materiaId, int materiaGrade), uint> materiaItemIds { get; init; }

        public ItemDataService(
            ITypedLogger<ItemDataService> logger,
            IDataManager dataManager,
            IMapper<string, GearpieceType> gearpieceTypeMapper
            )
        {
            this.logger = logger;
            this.dataManager = dataManager;
            this.gearpieceTypeMapper = gearpieceTypeMapper;

            ItemSheet = loadTraditionalSheet<Item>("Item")
                ?? dataManager.GetExcelSheet<Item>()
                ?? throw new InvalidOperationException("Item sheet not found");
            ItemSheetEn = tryLoadEnglishSheet<Item>("Item");
            ItemUiCategorySheetEn = tryLoadEnglishSheet<ItemUICategory>("ItemUICategory");
            ShopSheet = loadTraditionalSheet<SpecialShop>("SpecialShop")
                ?? dataManager.GetExcelSheet<SpecialShop>()
                ?? throw new InvalidOperationException("Special shop sheet not found");
            Materia = loadTraditionalSheet<SheetMateria>("Materia")
                ?? dataManager.GetExcelSheet<SheetMateria>()
                ?? throw new InvalidOperationException("Materia sheet not found");
            ClassJobSheet = loadTraditionalSheet<ClassJob>("ClassJob")
                ?? dataManager.GetExcelSheet<ClassJob>()
                ?? throw new InvalidOperationException("ClassJob sheet not found");
            ClassJobSheetEn = tryLoadEnglishSheet<ClassJob>("ClassJob");
            NameToId = [];
            materiaItemIds = [];
        }

        private static readonly ClientLanguage[] TraditionalChineseLanguages =
        [
            ClientLanguage.ChineseTraditional,
            ClientLanguage.TraditionalChinese,
        ];

        /// <summary>
        /// Loads a sheet in the Taiwan client's Traditional Chinese language.
        /// yanmucorp Dalamud 13 exposes both <see cref="ClientLanguage.ChineseTraditional"/> and
        /// <see cref="ClientLanguage.TraditionalChinese"/>. The first one that returns a sheet is used.
        /// </summary>
        private ExcelSheet<T>? loadTraditionalSheet<T>(string label) where T : struct, IExcelRow<T>
        {
            foreach (var language in TraditionalChineseLanguages)
            {
                var sheet = tryGetSheet<T>(language, label);
                if (sheet is not null)
                {
                    logger.Info($"Loaded {label} from ClientLanguage.{language}.");
                    return sheet;
                }
            }

            logger.Error($"Traditional Chinese {label} sheet was not available. Falling back to the client default language.");
            return null;
        }

        private ExcelSheet<T>? tryLoadEnglishSheet<T>(string label) where T : struct, IExcelRow<T>
        {
            var sheet = tryGetSheet<T>(ClientLanguage.English, label);
            if (sheet is null)
                logger.Error($"English {label} sheet is not available. Name lookup and xivgear job abbreviations that need it will fail safely.");
            return sheet;
        }

        private ExcelSheet<T>? tryGetSheet<T>(ClientLanguage language, string label) where T : struct, IExcelRow<T>
        {
            try
            {
                return dataManager.GetExcelSheet<T>(language);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, $"ClientLanguage.{language} could not be loaded for {label}.");
                return null;
            }
        }

        private static ILookup<uint, (uint ItemId, CofferSourceType SourceType)> emptyCoffers() =>
            Array.Empty<(uint ItemId, CofferSourceType SourceType)>().ToLookup(_ => 0u, pair => pair);
    }

    public interface IItemDataService
    {
        /// <summary>
        /// Mapping of which items can be obtained via opening what coffers
        /// 
        /// item inside coffer -> coffer
        /// </summary>
        public ILookup<uint, (uint ItemId, CofferSourceType SourceType)> ItemsCoffers { get; }

        /// <summary>
        /// Mapping of which items can be obtained by doing some manner of trade-in.
        /// Since a trade-in may require more than one item, this mapping is 1-many
        /// 
        /// item obtained -> items required to complete trade in
        /// </summary>
        public ILookup<uint, (List<uint> ItemIds, uint SourceShopId)> ItemsPrerequisites { get; }

        public uint ConvertItemIdToHq(uint id);
        public string SeStringToString(ReadOnlySeString input);
        public string GetItemNameById(uint id);
        public uint GetItemIdByName(string name);
        public string GetShopNameById(uint shopId);
        public uint GetMateriaItemId(ushort materiaTypeId, byte materiaGrade);
        public int GetPercentChanceToAttach(uint materiaGrade, int overmeldSlotIdx, bool attachToHq);

        public bool ItemIdIsHq(uint itemId);


        /// <summary>
        /// Returns if an item can have materia attached to it
        /// </summary>
        /// <param name="itemId">The item id to check</param>
        /// <returns>If it can be melded. If invalid item id, returns false.</returns>
        public bool ItemIsMeldable(uint itemId);

        /// <summary>
        /// Returns the ItemUICategory for the given item id
        /// </summary>
        /// <param name="itemId">The item id to check</param>
        /// <returns>The corresponding ItemUiCategory</returns>
        public ItemUICategory GetItemUICategory(uint itemId);

        /// <summary>
        /// Returns the icon id for the given item id
        /// </summary>
        /// <param name="itemId">The item id to search for</param>
        /// <returns>The corresponding icon id</returns>
        public ushort GetItemIconId(uint itemId);

        /// <summary>
        /// Returns the number of materia slots an item has
        /// </summary>
        /// <param name="itemId">The item id to check</param>
        /// <returns>How many materia can be attached to this item.</returns>
        public (int Normal, int Advanced) GetItemMateriaSlotCount(uint itemId);

        /// <summary>   
        /// Extends the given PrerequisiteNode with new leaves at the node's direct child level according to current
        /// data. Used on config loading to automatically add new prerequisite information that wasn't available when the gearset
        /// was being added. Will never remove prerequisites, only add. It will also retain state for old nodes that already
        /// exist in the tree.
        /// </summary>
        /// <param name="itemId">The id of the item to extend prerequisites for</param>
        /// <param name="oldPrerequisiteNode">The old tree of prerequisites for this item</param>
        /// <param name="isCollected">If the config has this node set as collected</param>
        /// <param name="collectLock">If the config has this node's collect state as locked</param>
        /// <returns>The most up-to-date PrerequisiteNode. Could be the unmodified original if no changes were made</returns>
        public void ExtendItemPrerequisites(
            PrerequisiteNode? oldPrerequisiteNode,
            int maxDepth = 8
            );

        public PrerequisiteNode? BuildGearpiecePrerequisiteTree(
            uint itemId,
            bool isCollected = false,
            bool collectLock = false,
            int maxDepth = 8
            );

        /// <summary>
        /// Returns information about a specific materia item
        /// </summary>
        /// <param name="materiaItemId">The RowId of a materia item</param>
        /// <returns>A <see cref="MateriaDetails"/> struct containing information on the given materia item id</returns>
        public MateriaDetails GetMateriaInfo(uint materiaItemId);

        public bool ItemIsShield(uint itemId);

        /// <summary>
        /// Use an item's corresponding ClassJobCategory to return the list of job abbreviations that
        /// can equip the item
        /// </summary>
        /// <param name="itemId">The RowId or HQ-Offset RowId for the item</param>
        /// <returns>The set of 3-letter job abbreviations that can equip the item</returns>
        public HashSet<string> GetItemClassJobCategories(uint itemId);

        /// <summary>
        /// Use an item's corresponding EquipSlotCategory to find it's GearpieceType
        /// </summary>
        /// <param name="itemId">The RowId or HQ-Offset RowId for the item</param>
        /// <returns>The corresponding GearpieceType</returns>
        public GearpieceType GetItemGearpieceType(uint itemId);

        /// <summary>
        /// Retrieve the <see cref="ClassJobInfo"/> data for the job that uses the 3-letter <paramref name="abbrevation"/>
        /// when shown with the game loaded in the provided <paramref name="language"/>
        /// </summary>
        /// <param name="abbrevation">The 3-letter localized job abbreviation string. Ex: (GLA, SMN, LTW)</param>
        /// <param name="language"> The language to return the ClassJobInfo in</param>
        /// <returns>The <see cref="ClassJobInfo"/> for the matching job if one is found, else a default JobInfo object
        /// representing no job being found with a 0 JobId</returns>
        public ClassJobInfo GetClassJobInfoByEnAbbreviation(string abbrevation, ClientLanguage? language = null);

        /// <summary>
        /// Retrieve the <see cref="ClassJobInfo"/> data for the job with the matching <paramref name="jobId"/>,
        /// in the current game language
        /// </summary>
        /// <param name="jobId">The row index of the job found in the ClassJob table</param>
        /// <returns>The <see cref="ClassJobInfo"/> for the matching job if one is found, else a default JobInfo object
        /// representing no job being found with a 0 JobId</returns>
        public ClassJobInfo GetClassJobInfoById(uint jobId);

        /// <summary>
        /// English ClassJob abbreviation for the row, read from the English sheet.
        /// Empty when the English sheet or the row is unavailable.
        /// </summary>
        public string GetEnglishClassJobAbbreviation(uint classJobId);

        public bool ItemCanBeHq(uint itemId);

        /// <summary>
        /// Equip level of the item, or 0 when the row cannot be read.
        /// </summary>
        public int GetItemEquipLevel(uint itemId);

        /// <summary>
        /// Given a list of item ids, return all class job ids that can use them all
        /// If none can use them all, returns 0
        /// </summary>
        /// <param name="itemIds">The item ids to check</param>
        /// <returns>Enumerable of class job ids that can use all the item ids provided</returns>
        public IEnumerable<uint> FindClassJobIdUsers(IEnumerable<uint> itemIds);
    }
}
