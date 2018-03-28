using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static ChaListDefine;

namespace Sideloader.AutoResolver
{
    public static class StructReference
    {
        private static Dictionary<string, PropertyInfo> GeneratePropertyInfoDictionary(Type t, IEnumerable<string> properties)
        {
            return properties
                .Select(property => AccessTools.Property(t, property))
                .ToDictionary(property => $"{t.Name}.{property.Name}");
        }

        public static Dictionary<CategoryNo, string> ChaFileFaceCategories = new Dictionary<CategoryNo, string>
        {
            //these might be very wrong, please advise
            { CategoryNo.ao_head, "headId" },
            { CategoryNo.mt_cheek, "skinId" },
            { CategoryNo.mt_face_detail, "detailId" },
            { CategoryNo.mt_eyebrow, "eyebrowId" },
            { CategoryNo.mt_nose, "noseId" },
            { CategoryNo.mt_eye_hi_up, "hlUpId" },
            { CategoryNo.mt_eye_hi_down, "hlDownId" },
            { CategoryNo.mt_eye_white, "whiteId" },
            { CategoryNo.mt_eyeline_up, "eyelineUpId" },
            { CategoryNo.mt_eyeline_down, "eyelineDownId" },
            { CategoryNo.mt_mole, "moleId" },
            { CategoryNo.mt_lipline, "lipLineId" },
        };

        private static Dictionary<string, PropertyInfo> _chaFileFaceProperties = GeneratePropertyInfoDictionary(typeof(ChaFileFace), ChaFileFaceCategories.Values);
        public static Dictionary<string, PropertyInfo> ChaFileFaceProperties => _chaFileFaceProperties;
    }
}
