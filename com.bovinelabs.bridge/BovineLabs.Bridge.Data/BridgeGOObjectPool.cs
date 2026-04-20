// <copyright file="BridgeObjectPool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using System;
    using UnityEngine;
    using UnityEngine.Pool;
    using Object = UnityEngine.Object;

    public class BridgeGOObjectPool : ObjectPool<GameObject>
    {
        public BridgeGOObjectPool(int maxSize = 10000)
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUGS
            : base(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, true, 10, maxSize)
#else
            : base(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, false, 10, maxSize)
#endif
        {
        }

        private static GameObject CreateFunc()
        {
            var go = new GameObject("GameObject");
#if UNITY_EDITOR
            go.hideFlags = BridgeObjectConfig.Flags;

            if (Application.isPlaying)
#endif
            {
                Object.DontDestroyOnLoad(go);
            }

            return go;
        }

        private static void ActionOnGet(GameObject go)
        {
            go.SetActive(true);
        }

        private static void ActionOnRelease(GameObject go)
        {
            go.SetActive(false);
        }

        private static void ActionOnDestroy(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
            }
            else
#endif
            {
                Object.Destroy(go);
            }
        }
    }
}
