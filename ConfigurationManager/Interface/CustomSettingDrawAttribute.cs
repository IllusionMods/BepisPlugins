// Made by MarC0 / ManlyMarco
// Copyright 2018 GNU General Public License v3.0

using System;
using System.Reflection;
using UnityEngine;

namespace BepInEx
{
    public class CustomSettingDrawAttribute : AcceptableValueBaseAttribute
    {
        /// <summary>
        /// Register a custom field editor drawer that will replace config manager's default field editors
        /// (The part between setting name and the default button).
        /// </summary>
        /// <param name="customFieldDrawMethod">Name of the method that will draw the edit field. 
        /// The method needs to be an instance method with signature <code>void Name ()</code>. Runs in OnGUI.</param>
        public CustomSettingDrawAttribute(string customFieldDrawMethod)
        {
            CustomFieldDrawMethod = customFieldDrawMethod;
        }

        public string CustomFieldDrawMethod { get; set; }

        private Action _invoker;

        internal void Run(object instance)
        {
            if(_invoker == null)
            {
                var method = instance.GetType().GetMethod(CustomFieldDrawMethod,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null || method.ContainsGenericParameters || method.GetParameters().Length > 0)
                {
                    var error = $"Failed to get custom draw method {CustomFieldDrawMethod} from {instance.GetType().Name}. It has to be an instance method with no arguments.";
                    _invoker = () => GUILayout.Label(error, GUILayout.ExpandWidth(true));
                }
                else
                {
                    _invoker = () => method.Invoke(instance, null);
                }
            }
            
            _invoker();
        }
    }
}