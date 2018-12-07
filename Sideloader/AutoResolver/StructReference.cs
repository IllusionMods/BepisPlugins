using Harmony;
using System;
using System.Collections.Generic;
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

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileFaceProperties { get; } = _chaFileFaceGenerator();
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

	    public static Dictionary<CategoryProperty, StructValue<int>> ChaFileBodyProperties { get; } = _chaFileBodyGenerator();

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

	    public static Dictionary<CategoryProperty, StructValue<int>> ChaFileHairProperties { get; } = _chaFileHairGenerator();
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

	    public static Dictionary<CategoryProperty, StructValue<int>> ChaFileMakeupProperties { get; } = _chaFileMakeupGenerator();
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
			}

            return generatedProperties;
        }

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileClothesProperties { get; } = _chaFileClothesGenerator();
        #endregion

        #region ChaFileAccessory.PartsInfo
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

        public static Dictionary<CategoryProperty, StructValue<int>> ChaFileAccessoryPartsInfoProperties { get; } = _chaFileAccessoryPartsInfoGenerator();
        #endregion

        #region Collated
        private static Dictionary<CategoryProperty, StructValue<int>> _collatedGenerator()
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

        public static Dictionary<CategoryProperty, StructValue<int>> CollatedStructValues { get; } = _collatedGenerator();
        #endregion
    }
}