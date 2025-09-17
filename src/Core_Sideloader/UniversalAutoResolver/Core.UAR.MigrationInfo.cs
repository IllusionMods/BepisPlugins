using MessagePack;
#if AI || HS2
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Data about the migration to be performed
    /// </summary>
    [MessagePackObject]
    public class MigrationInfo
    {
        /// <summary>
        /// Type of migration to perform
        /// </summary>
        [Key(0)]
        public MigrationType MigrationType;
        /// <summary>
        /// Category of the item
        /// </summary>
        [Key(1)]
        public ChaListDefine.CategoryNo Category;
        /// <summary>
        /// GUID of the item to perform migration on
        /// </summary>
        [Key(2)]
        public string GUIDOld;
        /// <summary>
        /// GUID to migrate to
        /// </summary>
        [Key(3)]
        public string GUIDNew;
        /// <summary>
        /// ID of the item to perform migration on
        /// </summary>
        [Key(4)]
        public int IDOld;
        /// <summary>
        /// ID to migrate to
        /// </summary>
        [Key(5)]
        public int IDNew;

        /// <summary>
        /// Create a new MigrationInfo.
        /// </summary>
        /// <param name="migrationType">Type of migration to perform.</param>
        /// <param name="category">Category of the item.</param>
        /// <param name="guidOld">GUID of the item to perform migration on.</param>
        /// <param name="guidNew">GUID to migrate to.</param>
        /// <param name="idOld">TheID of the item to perform migration on.</param>
        /// <param name="idNew">ID to migrate to.</param>
        [SerializationConstructor]
        public MigrationInfo(MigrationType migrationType, ChaListDefine.CategoryNo category, string guidOld, string guidNew, int idOld, int idNew)
        {
            MigrationType = migrationType;
            Category = category;
            GUIDOld = guidOld?.Trim();
            GUIDNew = guidNew?.Trim();
            IDOld = idOld;
            IDNew = idNew;
        }

        /// <inheritdoc cref="MigrationInfo(MigrationType,ChaListDefine.CategoryNo,string,string,int,int)"/>
        public MigrationInfo(MigrationType migrationType, string guidOld, string guidNew)
        {
            MigrationType = migrationType;
            GUIDOld = guidOld?.Trim();
            GUIDNew = guidNew?.Trim();
        }
    }
}
