// <copyright file="StatesToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.Systems
{
    using BovineLabs.Anchor.Binding;
    using BovineLabs.Anchor.Toolbar;
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Nerve.Data.States;
    using BovineLabs.Nerve.Debug.ViewModels;
    using BovineLabs.Nerve.Debug.Views;
    using BovineLabs.Nerve.States;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    [WorldSystemFilter(Worlds.ClientLocal | Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct StatesToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<StatesToolbarView, StatesToolbarViewModel, StatesToolbarViewModel.Data> toolbar;
        private NativeList<int> valueCache;

        // <state index, index in dropdown>
        private NativeHashMap<byte, uint> gameStateValues;

        private BitArray256 previousGameValue;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<StatesToolbarView, StatesToolbarViewModel, StatesToolbarViewModel.Data>(ref state, "States");
        }

        /// <inheritdoc />
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();

            ref var binding = ref this.toolbar.Binding;

            var gameStateSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<GameStateSystem>();
            var states = state.WorldUnmanaged.GetUnsafeSystemRef<GameStateSystem>(gameStateSystem).States;

            this.gameStateValues = CreateChoicesK(states, out var gameChoices);

            binding.StateItems = gameChoices;
            binding.StateValues = new NativeList<int>(Allocator.Persistent);
            this.valueCache = new NativeList<int>(Allocator.Persistent);
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
            ref var binding = ref this.toolbar.Binding;

            binding.StateItems.Dispose();
            binding.StateValues.Dispose();
            this.gameStateValues.Dispose();
            this.valueCache.Dispose();

            this.toolbar.Unload();
        }

        private static NativeHashMap<byte, uint> CreateChoicesK(
            NativeParallelHashMap<byte, ComponentType>.ReadOnly states, out NativeArray<FixedString64Bytes> choices)
        {
            var values = new NativeHashMap<byte, uint>(states.Count(), Allocator.Persistent);
            choices = new NativeArray<FixedString64Bytes>(states.Count(), Allocator.Persistent);

            var index = 0;
            using var e = states.GetEnumerator();
            while (e.MoveNext())
            {
                var name = e.Current.Value.GetManagedType().Name;
                name = name.TrimStart("State").ToSentence();

                choices[index] = name;
                values[e.Current.Key] = (uint)index;
                index++;
            }

            return values;
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            BitArray256 gameState;

            if (state.WorldUnmanaged.IsServiceWorld())
            {
                if (!SystemAPI.TryGetSingleton<ServiceState>(out var serviceState))
                {
                    return;
                }

                gameState = serviceState.Value;
            }
            else
            {
                if (!SystemAPI.TryGetSingleton<ClientState>(out var clientState))
                {
                    return;
                }

                gameState = clientState.Value;
            }

            ref var binding = ref this.toolbar.Binding;
            var uiChanged = !binding.StateValues.AsArray().ArraysEqual(this.valueCache.AsArray());

            this.valueCache.Clear();
            var newValue = default(BitArray256);
            newValue |= this.GetRemap(gameState.Data1, 0, this.gameStateValues, this.valueCache);
            newValue |= this.GetRemap(gameState.Data2, 64, this.gameStateValues, this.valueCache);
            newValue |= this.GetRemap(gameState.Data3, 128, this.gameStateValues, this.valueCache);
            newValue |= this.GetRemap(gameState.Data4, 196, this.gameStateValues, this.valueCache);

            if (newValue != this.previousGameValue)
            {
                binding.StateValues.Clear();
                binding.StateValues.AddRange(this.valueCache.AsArray());
                binding.Notify(nameof(StatesToolbarViewModel.StateValues));
                this.previousGameValue = newValue;
            }
            else if (uiChanged)
            {
                this.valueCache.Clear();
                this.valueCache.AddRange(binding.StateValues.AsArray());

                var newState = default(BitArray256);
                foreach (var v in this.valueCache)
                {
                    newState[(uint)v] = true;
                }

                if (state.WorldUnmanaged.IsServiceWorld())
                {
                    SystemAPI.SetSingleton(new ServiceState { Value = newState });
                }
                else
                {
                    SystemAPI.SetSingleton(new ClientState { Value = newState });
                }
            }
        }

        private BitArray256 GetRemap(ulong value, byte offset, NativeHashMap<byte, uint> map, NativeList<int> values)
        {
            var output = default(BitArray256);

            while (value != 0)
            {
                var index = math.tzcnt(value);
                var shifted = (uint)(1 << index);
                value ^= shifted;
                var offsetIndex = (byte)(index + offset);

                if (map.TryGetValue(offsetIndex, out var actualIndex))
                {
                    output[actualIndex] = true;
                    values.Add((int)actualIndex);
                }
            }

            return output;
        }

        // private void UpdateSelection(ref SystemState state, ref CameraToolbarBindings.Data binding)
        // {
        //     if (!SystemAPI.TryGetSingleton<CameraState>(out var cameraState))
        //     {
        //         return;
        //     }
        //
        //     var newSelection = this.cameraStates.IndexOf(cameraState.Value);
        //
        //     // Case of selection changing
        //     if (newSelection != this.lastSelection)
        //     {
        //         binding.Selection = newSelection;
        //         this.lastSelection = newSelection;
        //     }
        //     // Case of binding changing
        //     else if (newSelection != binding.Selection)
        //     {
        //         this.lastSelection = binding.Selection;
        //
        //         byte newCameraState = 0;
        //
        //         if (binding.Selection >= 0 && binding.Selection < this.cameraStates.Length)
        //         {
        //             newCameraState = this.cameraStates[binding.Selection];
        //         }
        //
        //         SystemAPI.SetSingleton(new CameraState { Value = newCameraState });
        //     }
        // }
    }
}
