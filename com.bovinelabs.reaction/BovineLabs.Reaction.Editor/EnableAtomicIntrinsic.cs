// <copyright file="EnableAtomicIntrinsic.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS
namespace BovineLabs.Reaction.Editor
{
    using UnityEditor;
    using UnityEditor.Build;

    [InitializeOnLoad]
    public static class EnableAtomicIntrinsic
    {
        static EnableAtomicIntrinsic()
        {
            var target = CurrentNamedBuildTarget;
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
            defines += ";UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS";
            PlayerSettings.SetScriptingDefineSymbols(target, defines);
        }

        private static NamedBuildTarget CurrentNamedBuildTarget
        {
            get
            {
#if UNITY_SERVER
                return NamedBuildTarget.Server;
#else
                var buildTarget = EditorUserBuildSettings.activeBuildTarget;
                var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                return namedBuildTarget;
#endif
            }
        }
    }
}
#endif
