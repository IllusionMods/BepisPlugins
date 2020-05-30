#if AI || HS2
using AIChara;
#endif

namespace Sideloader.AutoResolver
{
    /// <summary>
    /// Data about the migration to be performed
    /// </summary>
    public class MigrationInfo
    {
        /// <summary>
        /// Type of migration to perform
        /// </summary>
        public MigrationType MigrationType;
        /// <summary>
        /// Category of the item
        /// </summary>
        public ChaListDefine.CategoryNo Category;
        /// <summary>
        /// GUID of the item to perform migration on
        /// </summary>
        public string GUIDOld;
        /// <summary>
        /// GUID to migrate to
        /// </summary>
        public string GUIDNew;
        /// <summary>
        /// ID of the item to perform migration on
        /// </summary>
        public int IDOld;
        /// <summary>
        /// ID to migrate to
        /// </summary>
        public int IDNew;

        internal MigrationInfo(MigrationType migrationType, ChaListDefine.CategoryNo category, string guidOld, string guidNew, int idOld, int idNew)
        {
            MigrationType = migrationType;
            Category = category;
            GUIDOld = guidOld;
            GUIDNew = guidNew;
            IDOld = idOld;
            IDNew = idNew;
        }
        internal MigrationInfo(MigrationType migrationType, string guidOld, string guidNew)
        {
            MigrationType = migrationType;
            GUIDOld = guidOld;
            GUIDNew = guidNew;
        }
    }
}
