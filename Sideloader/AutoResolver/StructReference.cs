using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared;
using CategoryNo = ChaListDefine.CategoryNo;
using static ChaFileDefine;

namespace Sideloader.AutoResolver
{
    public struct CategoryProperty
    {
        public CategoryNo Category;
        public string Property;

        public string Prefix;

        public CategoryProperty(CategoryNo category, string property, string prefix = "")
        {
            Category = category;
            Property = property;

            Prefix = prefix;
        }

        public override string ToString()
        {
            return Prefix != "" ? $"{Prefix}.{Property}" : Property;
        }
    }

    public static class StructReference
    {
        #region Helper Methods
        private static Dictionary<CategoryProperty, StructValue<int>> GeneratePropertyInfoDictionary(Type t, IEnumerable<CategoryProperty> properties, string prefix = "")
        {
            var result = new Dictionary<CategoryProperty, StructValue<int>>();

            foreach (CategoryProperty property in properties)
            {
                var newProp = property;

                if (prefix != "")
                    newProp.Prefix = prefix;

                result.Add(newProp, new StructValue<int>(AccessTools.Property(t, newProp.Property)));
            }

            return result;
        }
        #endregion

        #region ChaFileFace
        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileFaceGenerator()
        {
            const string prefix = nameof(ChaFileFace);

            var baseProperties = new List<CategoryProperty>
            {
                //these might be very wrong, please advise
                new CategoryProperty(CategoryNo.bo_head, "headId"),
                new CategoryProperty(CategoryNo.mt_face_detail, "detailId"),
                new CategoryProperty(CategoryNo.mt_eyebrow, "eyebrowId"),
                new CategoryProperty(CategoryNo.mt_nose, "noseId"),
                new CategoryProperty(CategoryNo.mt_eye_hi_up, "hlUpId"),
                new CategoryProperty(CategoryNo.mt_eye_hi_down, "hlDownId"),
                new CategoryProperty(CategoryNo.mt_eye_white, "whiteId"),
                new CategoryProperty(CategoryNo.mt_eyeline_up, "eyelineUpId"),
                new CategoryProperty(CategoryNo.mt_eyeline_down, "eyelineDownId"),
                new CategoryProperty(CategoryNo.mt_mole, "moleId"),
                new CategoryProperty(CategoryNo.mt_lipline, "lipLineId")
            };

            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileFace), baseProperties, prefix);

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_eye, "Pupil1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileFace)obj).pupil[0].id = value; },
                    (obj) =>          ((ChaFileFace)obj).pupil[0].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_eye, "Pupil2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileFace)obj).pupil[1].id = value; },
                    (obj) =>          ((ChaFileFace)obj).pupil[1].id));

            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileFacePropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileFaceGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileFaceProperties => _chaFileFacePropertiesLazy;
        #endregion

        #region ChaFileBody
        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileBodyGenerator()
        {
            const string prefix = nameof(ChaFileBody);

            var baseProperties = new List<CategoryProperty>
            {
                new CategoryProperty(CategoryNo.mt_body_detail, "detailId"),
                new CategoryProperty(CategoryNo.mt_sunburn, "sunburnId"),
                new CategoryProperty(CategoryNo.mt_nip, "nipId"),
                new CategoryProperty(CategoryNo.mt_underhair, "underhairId"),
            };

            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileBody), baseProperties, prefix);
            

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_body_paint, "PaintID1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintId[0] = value; },
                    (obj) =>          ((ChaFileBody)obj).paintId[0]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_body_paint, "PaintID2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintId[1] = value; },
                    (obj) =>          ((ChaFileBody)obj).paintId[1]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bodypaint_layout, "PaintLayoutID1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintLayoutId[0] = value; },
                    (obj) =>          ((ChaFileBody)obj).paintLayoutId[0]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bodypaint_layout, "PaintLayoutID2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintLayoutId[1] = value; },
                    (obj) =>          ((ChaFileBody)obj).paintLayoutId[1]));

            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileBodyPropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileBodyGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileBodyProperties => _chaFileBodyPropertiesLazy;
        #endregion

        #region ChaFileHair
        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileHairGenerator()
        {
            const string prefix = nameof(ChaFileHair);

            var baseProperties = new List<CategoryProperty>
            {
                new CategoryProperty(CategoryNo.mt_hairgloss, "glossId")
            };

            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileHair), baseProperties, prefix);

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_b, "HairBack", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.back].id = value; },
                    (obj) =>          ((ChaFileHair)obj).parts[(int)HairKind.back].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_f, "HairFront", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.front].id = value; },
                    (obj) =>          ((ChaFileHair)obj).parts[(int)HairKind.front].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_s, "HairSide", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.side].id = value; },
                    (obj) =>          ((ChaFileHair)obj).parts[(int)HairKind.side].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_o, "HairOption", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.option].id = value; },
                    (obj) =>          ((ChaFileHair)obj).parts[(int)HairKind.option].id));

            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileHairPropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileHairGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileHairProperties => _chaFileHairPropertiesLazy;
        #endregion

        #region ChaFileMakeup
        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileMakeupGenerator()
        {
            const string prefix = nameof(ChaFileMakeup);

            var baseProperties = new List<CategoryProperty>
            {
                new CategoryProperty(CategoryNo.mt_eyeshadow, "eyeshadowId"),
                new CategoryProperty(CategoryNo.mt_cheek, "cheekId"),
                new CategoryProperty(CategoryNo.mt_lip, "lipId"),
            };

            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileMakeup), baseProperties, prefix);
            

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_face_paint, "PaintID1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileMakeup)obj).paintId[0] = value; },
                    (obj) =>          ((ChaFileMakeup)obj).paintId[0]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_face_paint, "PaintID2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileMakeup)obj).paintId[1] = value; },
                    (obj) =>          ((ChaFileMakeup)obj).paintId[1]));
            

            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileMakeupPropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileMakeupGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileMakeupProperties => _chaFileMakeupPropertiesLazy;
        #endregion

        #region ChaFileClothes
        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileClothesGenerator()
        {
            const string prefix = nameof(ChaFileClothes);

            var generatedProperties = new Dictionary<CategoryProperty, StructValue<int>>();
            
            //main parts
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_top, "ClothesTop", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.top].id));
            
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_bot, "ClothesBot", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_bra, "ClothesBra", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shorts, "ClothesShorts", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_gloves, "ClothesGloves", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_panst, "ClothesPants", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_socks, "ClothesSocks", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shoes, "ClothesShoesInner", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shoes, "ClothesShoesOuter", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].id = value; },
                    (obj) =>          ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].id));

            
            
            //sub parts
            //jacket sub
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_a, "ClothesJacketSubA", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA]));
            
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_b, "ClothesJacketSubB", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB]));
            
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_c, "ClothesJacketSubC", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC]));

            
            //sailor sub
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_a, "ClothesSailorSubA", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA]));
            
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_b, "ClothesSailorSubB", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB]));
            
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_c, "ClothesSailorSubC", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC] = value; },
                    (obj) =>          ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC]));


            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileClothesPropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileClothesGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileClothesProperties => _chaFileClothesPropertiesLazy;
        #endregion

        #region ChaFileAccessory.PartsInfo
        private static int AccessoryLimit = 200;

        private static Dictionary<CategoryProperty, StructValue<int>> _chaFileAccessoryPartsInfoGenerator()
        {
            string prefix = $"{nameof(ChaFileAccessory)}.{nameof(ChaFileAccessory.PartsInfo)}";

            var baseProperties = new List<CategoryProperty>
            {
                new CategoryProperty(CategoryNo.ao_none , "id", prefix),
                new CategoryProperty(CategoryNo.ao_hair , "id", prefix),
                new CategoryProperty(CategoryNo.ao_head , "id", prefix),
                new CategoryProperty(CategoryNo.ao_face , "id", prefix),
                new CategoryProperty(CategoryNo.ao_neck , "id", prefix),
                new CategoryProperty(CategoryNo.ao_body , "id", prefix),
                new CategoryProperty(CategoryNo.ao_waist , "id", prefix),
                new CategoryProperty(CategoryNo.ao_leg , "id", prefix),
                new CategoryProperty(CategoryNo.ao_arm , "id", prefix),
                new CategoryProperty(CategoryNo.ao_hand , "id", prefix),
                new CategoryProperty(CategoryNo.ao_kokan , "id", prefix)
            };

            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileAccessory.PartsInfo), baseProperties, prefix);

            return generatedProperties;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _chaFileAccessoryPartsInfoPropertiesLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_chaFileAccessoryPartsInfoGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileAccessoryPartsInfoProperties => _chaFileAccessoryPartsInfoPropertiesLazy;
        #endregion

        #region Collated
        private static Dictionary<CategoryProperty, StructValue<int>> _collatedGenerator()
        {
            var collated = new Dictionary<CategoryProperty, StructValue<int>>();

            ChaFileFaceProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));
            ChaFileBodyProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));
            ChaFileHairProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));
            
            ChaFileClothesProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));
            ChaFileMakeupProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));
            ChaFileAccessoryPartsInfoProperties.ToList().ForEach(x => collated.Add(x.Key, x.Value));

            return collated;
        }

        private static readonly Lazy<Dictionary<CategoryProperty, StructValue<int>>> _collatedLazy =
            Lazy<Dictionary<CategoryProperty, StructValue<int>>>.Create(_collatedGenerator);

        public static Dictionary<CategoryProperty, StructValue<int>> CollatedStructValues => _collatedLazy.Instance;
        #endregion
    }
}
