using System;
using System.Text;
using UnityEngine;

public static class Extensions
{
#if EC
  public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component => gameObject == null ? null : gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
#endif
}
