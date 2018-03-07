using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Screencap
{
    internal static class CameraUtils
    {
        public static Camera CopyCamera(Camera source)
        {
            var go = new GameObject();
            Camera renderCam = go.AddComponent<Camera>();
            renderCam.CopyFrom(source);
            CopyComponents(source.gameObject, renderCam.gameObject);

            return renderCam;
        }

        static void CopyComponents(GameObject original, GameObject target)
        {
            foreach (Component component in original.GetComponents<Component>())
            {
                var newComponent = CopyComponent(component, target);

                if (component is MonoBehaviour)
                {
                    var behavior = (MonoBehaviour)component;

                    (newComponent as MonoBehaviour).enabled = behavior.enabled;
                }
            }
        }

        //https://answers.unity.com/questions/458207/copy-a-component-at-runtime.html
        static Component CopyComponent(Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }
    }
}
