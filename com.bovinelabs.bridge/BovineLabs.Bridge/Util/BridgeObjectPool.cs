// <copyright file="BridgeObjectPool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using System;
    using UnityEngine;
    using UnityEngine.Pool;
    using Object = UnityEngine.Object;

    public class BridgeObjectPool : ObjectPool<GameObject>
    {
        public BridgeObjectPool(Func<GameObject> createFunc, int maxSize = 10000)
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUGS
            : base(createFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, true, 10, maxSize)
#else
            : base(createFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, false, 10, maxSize)
#endif
        {
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
