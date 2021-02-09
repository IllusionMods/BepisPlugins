#if KK || EC
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static ChaFileDefine;
using CategoryNo = ChaListDefine.CategoryNo;

namespace Sideloader.AutoResolver
{
    internal struct CategoryProperty
    {
        internal CategoryNo Category;
        internal string Property;

        internal string Prefix;

        internal CategoryProperty(CategoryNo category, string property, string prefix = "")
        {
            Category = category;
            Property = property;

            Prefix = prefix;
        }

        public override string ToString() => Prefix != "" ? $"{Prefix}.{Property}" : Property;
    }

    internal static class StructReference
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
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileFaceGenerator()
        {
            const string prefix = nameof(ChaFileFace);

            var baseProperties = new List<CategoryProperty>
            {
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
                    (obj) => ((ChaFileFace)obj).pupil[0].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_eye, "Pupil2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileFace)obj).pupil[1].id = value; },
                    (obj) => ((ChaFileFace)obj).pupil[1].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_eye_gradation, "PupilGradient1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileFace)obj).pupil[0].gradMaskId = value; },
                    (obj) => ((ChaFileFace)obj).pupil[0].gradMaskId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_eye_gradation, "PupilGradient2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileFace)obj).pupil[1].gradMaskId = value; },
                    (obj) => ((ChaFileFace)obj).pupil[1].gradMaskId));

            return generatedProperties;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileFaceProperties { get; } = ChaFileFaceGenerator();
#endregion

#region ChaFileBody
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileBodyGenerator()
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
                    (obj) => ((ChaFileBody)obj).paintId[0]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_body_paint, "PaintID2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintId[1] = value; },
                    (obj) => ((ChaFileBody)obj).paintId[1]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bodypaint_layout, "PaintLayoutID1", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintLayoutId[0] = value; },
                    (obj) => ((ChaFileBody)obj).paintLayoutId[0]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bodypaint_layout, "PaintLayoutID2", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileBody)obj).paintLayoutId[1] = value; },
                    (obj) => ((ChaFileBody)obj).paintLayoutId[1]));

            return generatedProperties;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileBodyProperties { get; } = ChaFileBodyGenerator();

#endregion

#region ChaFileHair
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileHairGenerator()
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
                    (obj) => ((ChaFileHair)obj).parts[(int)HairKind.back].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_f, "HairFront", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.front].id = value; },
                    (obj) => ((ChaFileHair)obj).parts[(int)HairKind.front].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_s, "HairSide", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.side].id = value; },
                    (obj) => ((ChaFileHair)obj).parts[(int)HairKind.side].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.bo_hair_o, "HairOption", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileHair)obj).parts[(int)HairKind.option].id = value; },
                    (obj) => ((ChaFileHair)obj).parts[(int)HairKind.option].id));

            return generatedProperties;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileHairProperties { get; } = ChaFileHairGenerator();
#endregion

#region ChaFileMakeup
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileMakeupGenerator()
        {
#if KK
            const string prefix = nameof(ChaFileMakeup);
#elif EC
            const string prefix = nameof(ChaFileFace.ChaFileMakeup);
#endif

            var baseProperties = new List<CategoryProperty>
            {
                new CategoryProperty(CategoryNo.mt_eyeshadow, "eyeshadowId"),
                new CategoryProperty(CategoryNo.mt_cheek, "cheekId"),
                new CategoryProperty(CategoryNo.mt_lip, "lipId"),
            };

#if KK
            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileMakeup), baseProperties, prefix);
#elif EC
            var generatedProperties = GeneratePropertyInfoDictionary(typeof(ChaFileFace.ChaFileMakeup), baseProperties, prefix);
#endif


            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_face_paint, "PaintID1", prefix),
                new StructValue<int>(
#if KK
                    (obj, value) => { ((ChaFileMakeup)obj).paintId[0] = value; },
                    (obj) => ((ChaFileMakeup)obj).paintId[0]));
#elif EC
                    (obj, value) => { ((ChaFileFace.ChaFileMakeup)obj).paintId[0] = value; },
                    (obj) => ((ChaFileFace.ChaFileMakeup)obj).paintId[0]));
#endif

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_face_paint, "PaintID2", prefix),
                new StructValue<int>(
#if KK
                    (obj, value) => { ((ChaFileMakeup)obj).paintId[1] = value; },
                    (obj) => ((ChaFileMakeup)obj).paintId[1]));
#elif EC
                    (obj, value) => { ((ChaFileFace.ChaFileMakeup)obj).paintId[1] = value; },
                    (obj) => ((ChaFileFace.ChaFileMakeup)obj).paintId[1]));
#endif

            return generatedProperties;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileMakeupProperties { get; } = ChaFileMakeupGenerator();
#endregion

#region ChaFileClothes
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileClothesGenerator()
        {
            const string prefix = nameof(ChaFileClothes);

            var generatedProperties = new Dictionary<CategoryProperty, StructValue<int>>();

            //main parts
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_top, "ClothesTop", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.top].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_bot, "ClothesBot", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_bra, "ClothesBra", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shorts, "ClothesShorts", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_gloves, "ClothesGloves", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_panst, "ClothesPants", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_socks, "ClothesSocks", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].id));

#if KK
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shoes, "ClothesShoesInner", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].id));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shoes, "ClothesShoesOuter", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].id));
#elif EC
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.co_shoes, "ClothesShoes", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].id = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].id));
#endif


            //sub parts
            //jacket sub
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_a, "ClothesJacketSubA", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_b, "ClothesJacketSubB", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_jacket_c, "ClothesJacketSubC", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC]));


            //sailor sub
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_a, "ClothesSailorSubA", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsA]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_b, "ClothesSailorSubB", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsB]));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.cpo_sailor_c, "ClothesSailorSubC", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC] = value; },
                    (obj) => ((ChaFileClothes)obj).subPartsId[(int)ClothesSubKind.partsC]));

#if KK
            //Emblems
            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesTopEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesBotEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesBraEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesShortsEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesGlovesEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesPantsEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesSocksEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesShoesInnerEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].emblemeId));

            generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, "ClothesShoesOuterEmblem", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].emblemeId = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].emblemeId));

            //Emblems 2
            //Check if Emblems2 exists. It was added later on in Koikatsu and may not exist for users running older versions of the game.
            if (typeof(ChaFileClothes.PartsInfo).GetProperties(AccessTools.all).Any(p => p.Name == "emblemeId2"))
            {
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesTopEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesBotEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesBraEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesShortsEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesGlovesEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesPantsEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesSocksEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesShoesInnerEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].emblemeId2 = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].emblemeId2));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, "ClothesShoesOuterEmblem2", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].emblemeId = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].emblemeId2));
            }
#elif EC
            //Emblems
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_emblem, $"ClothesTopEmblem{index}", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId[index] = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.top].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesBotEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesBraEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesShortsEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesGlovesEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesPantsEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesSocksEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].emblemeId[index]));

                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_emblem, $"ClothesShoesEmblem{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].emblemeId[index] = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].emblemeId[index]));
            }
#endif

            //Patterns
            for (int i = 0; i < 4; i++)
            {
                //we declare a separate value here instead of reusing `i` since closures and for variables don't mix very well
                //see this link for more info (it's specifically about foreach, but it applies here too)
                //https://stackoverflow.com/questions/12112881/has-foreachs-use-of-variables-been-changed-in-c-sharp-5
                int index = i;

                //top
                generatedProperties.Add(
                new CategoryProperty(CategoryNo.mt_pattern, $"ClothesTopPattern{index}", prefix),
                new StructValue<int>(
                    (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.top].colorInfo[index].pattern = value; },
                    (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.top].colorInfo[index].pattern));

                //bot
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesBotPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bot].colorInfo[index].pattern));

                //bra
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesBraPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.bra].colorInfo[index].pattern));

                //shorts
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesShortsPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shorts].colorInfo[index].pattern));

                //gloves
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesGlovesPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.gloves].colorInfo[index].pattern));

                //pants
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesPantsPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.panst].colorInfo[index].pattern));

                //socks
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesSocksPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.socks].colorInfo[index].pattern));

#if KK
                //shoes inner
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesShoesInnerPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_inner].colorInfo[index].pattern));

                //shoes outer
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesShoesOuterPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes_outer].colorInfo[index].pattern));
#elif EC
                //shoes
                generatedProperties.Add(
                    new CategoryProperty(CategoryNo.mt_pattern, $"ClothesShoesPattern{index}", prefix),
                    new StructValue<int>(
                        (obj, value) => { ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].colorInfo[index].pattern = value; },
                        (obj) => ((ChaFileClothes)obj).parts[(int)ClothesKind.shoes].colorInfo[index].pattern));
#endif
            }

            return generatedProperties;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileClothesProperties { get; } = ChaFileClothesGenerator();
#endregion

#region ChaFileAccessory.PartsInfo
        private static Dictionary<CategoryProperty, StructValue<int>> ChaFileAccessoryPartsInfoGenerator()
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

        internal static Dictionary<CategoryProperty, StructValue<int>> ChaFileAccessoryPartsInfoProperties { get; } = ChaFileAccessoryPartsInfoGenerator();
#endregion

#region Collated
        private static Dictionary<CategoryProperty, StructValue<int>> CollatedGenerator()
        {
            var collated = new Dictionary<CategoryProperty, StructValue<int>>();

            foreach (var kv in ChaFileFaceProperties) collated.Add(kv.Key, kv.Value);
            foreach (var kv in ChaFileBodyProperties) collated.Add(kv.Key, kv.Value);
            foreach (var kv in ChaFileHairProperties) collated.Add(kv.Key, kv.Value);

            foreach (var kv in ChaFileClothesProperties) collated.Add(kv.Key, kv.Value);
            foreach (var kv in ChaFileMakeupProperties) collated.Add(kv.Key, kv.Value);
            foreach (var kv in ChaFileAccessoryPartsInfoProperties) collated.Add(kv.Key, kv.Value);

            return collated;
        }

        internal static Dictionary<CategoryProperty, StructValue<int>> CollatedStructValues { get; } = CollatedGenerator();
#endregion
    }
}
#endif