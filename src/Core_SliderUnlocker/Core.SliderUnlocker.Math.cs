﻿using System.Collections.Generic;
using UnityEngine;
using static AnimationKeyInfo;

namespace SliderUnlocker
{
    public static class SliderMath
    {
        public static Vector3 CalculateScale(List<AnmKeyInfo> list, float rate)
        {
            Vector3 min = list[0].scl;
            Vector3 max = list[list.Count - 1].scl;

            return min + (max - min) * rate;
        }

        public static Vector3 CalculatePosition(List<AnmKeyInfo> list, float rate)
        {
            Vector3 min = list[0].pos;
            Vector3 max = list[list.Count - 1].pos;

            return min + (max - min) * rate;
        }

        public static Vector3 CalculateRotation(List<AnmKeyInfo> list, float rate)
        {
            Vector3 rot1 = list[0].rot;
            Vector3 rot2 = list[1].rot;
            Vector3 rot3 = list[list.Count - 1].rot;

            Vector3 vector = rot2 - rot1;
            Vector3 vector2 = rot3 - rot1;

            bool xFlag = vector.x >= 0f;
            bool yFlag = vector.y >= 0f;
            bool zFlag = vector.z >= 0f;

            if (vector2.x > 0f && !xFlag)
            {
                vector2.x -= 360f;
            }
            else if (vector2.x < 0f && xFlag)
            {
                vector2.x += 360f;
            }

            if (vector2.y > 0f && !yFlag)
            {
                vector2.y -= 360f;
            }
            else if (vector2.y < 0f && yFlag)
            {
                vector2.y += 360f;
            }

            if (vector2.z > 0f && !zFlag)
            {
                vector2.z -= 360f;
            }
            else if (vector2.z < 0f && zFlag)
            {
                vector2.z += 360f;
            }

            if (rate < 0f)
            {
                return rot1 - vector2 * Mathf.Abs(rate);
            }

            return rot3 + vector2 * Mathf.Abs(rate - 1f);
        }

        public static Vector3 SafeCalculateRotation(Vector3 original, string name, List<AnmKeyInfo> list, float rate)
        {
            // Fix rotation outside 0-100
            if (name.Contains("cf_s_Mune") ||                                   // [AI, PH, HS] breasts
                name.Contains("cf_s_Mouth") ||                                  // [AI, PH, HS] mouth corner
                name.Contains("cf_s_LegLow") ||                                 // [AI, PH, HS] ankles, calves
                name.Contains("cf_s_MayuTip") ||                                // [PH, HS] eyebrows
                name.Contains("thigh") && name.Contains("01") ||                // [KK, EC] thighs
                name.StartsWith("cf_a_bust") && name.EndsWith("_size"))         // [KK, EC] breasts
                return original;

            return CalculateRotation(list, rate);
        }

        public static float Lerp(float min, float max, float value)
        {
            return value * (max - min) + min;
        }

        public static float InverseLerp(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }
    }
}
