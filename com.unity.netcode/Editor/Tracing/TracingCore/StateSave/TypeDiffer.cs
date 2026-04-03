using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.NetCode.Editor.Tracing
{

    [BurstCompile]
    internal struct TypeDiffer : IDisposable
    {
        NativeArray<TypeDiffer> m_PerFieldDiffers;
        PortableFunctionPointer<DiffDelegate> m_DiffMethod;
        int m_FieldOffset;

        private TypeDiffer(PortableFunctionPointer<DiffDelegate> diffMethod) : this()
        {
            m_DiffMethod = diffMethod;
        }

        public TypeDiffer(ComponentType typeToDiff, Allocator allocator)
        {
            this = ConstructDifferRecursive(typeToDiff.GetManagedType(), allocator);
        }

        public void Dispose()
        {
            if (m_PerFieldDiffers.IsCreated)
            {
                foreach (var typeDiffer in m_PerFieldDiffers)
                {
                    typeDiffer.Dispose();
                }
            }

            m_PerFieldDiffers.Dispose();
        }

        public static FieldInfo[] GetAllFields(Type managedType)
        {
            return managedType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        public static TypeDiffer ConstructDifferRecursive(Type managedType, Allocator allocator)
        {
            TypeDiffer toReturn = default;

            if (managedType.IsPrimitive)
            {
                toReturn = CreatePrimitiveDiffer(managedType);
            }
            else
            {
                var allFields = GetAllFields(managedType);
                if (allFields.Length > 0)
                {
                    toReturn.m_PerFieldDiffers = new NativeArray<TypeDiffer>(allFields.Length, allocator);
                    int currentOffset = 0;
                    for (int i = 0; i < allFields.Length; i++)
                    {
                        var fieldInfo = allFields[i];
                        TypeDiffer differ = default;
                        int fieldSize;
                        fieldSize = UnsafeUtility.SizeOf(fieldInfo.FieldType);
                        if (fieldInfo.FieldType.IsPrimitive)
                        {
                            differ = CreatePrimitiveDiffer(fieldInfo.FieldType);
                        }
                        else
                        {
                            differ = ConstructDifferRecursive(fieldInfo.FieldType, allocator);
                        }

                        differ.m_FieldOffset = currentOffset;
                        currentOffset += fieldSize;

                        toReturn.m_PerFieldDiffers[i] = differ;
                    }
                }
            }

            return toReturn;
        }

        [BurstCompile]
        public readonly unsafe bool ProcessDiff(float epsilon, byte* left, byte* right)
        {
            bool hasDiff = false;
            if (m_DiffMethod.Ptr.IsCreated)
            {
                hasDiff = m_DiffMethod.Ptr.Invoke(left, right, epsilon);
            }
            else
            {
                for (int i = 0; i < this.m_PerFieldDiffers.Length; i++)
                {
                    var differ = m_PerFieldDiffers[i];
                    hasDiff |= differ.ProcessDiff(epsilon, left + differ.m_FieldOffset, right + differ.m_FieldOffset);
                }
            }

            return hasDiff;
        }


        static unsafe TypeDiffer CreatePrimitiveDiffer(Type fieldType)
        {
            // primitive types taken from https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            PortableFunctionPointer<DiffDelegate> boolDiff = new PortableFunctionPointer<DiffDelegate>(DiffBool);
            PortableFunctionPointer<DiffDelegate> byteDiff = new PortableFunctionPointer<DiffDelegate>(DiffByte);
            PortableFunctionPointer<DiffDelegate> sbyteDiff = new PortableFunctionPointer<DiffDelegate>(DiffSbyte);
            PortableFunctionPointer<DiffDelegate> doubleDiff = new PortableFunctionPointer<DiffDelegate>(DiffDouble);
            PortableFunctionPointer<DiffDelegate> floatDiff = new PortableFunctionPointer<DiffDelegate>(DiffFloat);
            PortableFunctionPointer<DiffDelegate> intDiff = new PortableFunctionPointer<DiffDelegate>(DiffInt);
            PortableFunctionPointer<DiffDelegate> uintDiff = new PortableFunctionPointer<DiffDelegate>(DiffUint);
            PortableFunctionPointer<DiffDelegate> nintDiff = new PortableFunctionPointer<DiffDelegate>(DiffNint);
            PortableFunctionPointer<DiffDelegate> longDiff = new PortableFunctionPointer<DiffDelegate>(DiffLong);
            PortableFunctionPointer<DiffDelegate> ulongDiff = new PortableFunctionPointer<DiffDelegate>(DiffUlong);
            PortableFunctionPointer<DiffDelegate> shortDiff = new PortableFunctionPointer<DiffDelegate>(DiffShort);
            PortableFunctionPointer<DiffDelegate> ushortDiff = new PortableFunctionPointer<DiffDelegate>(DiffUshort);
            if (fieldType == typeof(bool) || fieldType == typeof(System.Boolean)) return new TypeDiffer(boolDiff);
            if (fieldType == typeof(byte) || fieldType == typeof(System.Byte)) return new TypeDiffer(byteDiff);
            if (fieldType == typeof(sbyte) || fieldType == typeof(System.SByte)) return new TypeDiffer(sbyteDiff);
            if (fieldType == typeof(double) || fieldType == typeof(System.Double)) return new TypeDiffer(doubleDiff);
            if (fieldType == typeof(float) || fieldType == typeof(System.Single)) return new TypeDiffer(floatDiff);
            if (fieldType == typeof(int) || fieldType == typeof(System.Int32)) return new TypeDiffer(intDiff);
            if (fieldType == typeof(uint) || fieldType == typeof(System.UInt32)) return new TypeDiffer(uintDiff);
            if (fieldType == typeof(nint) || fieldType == typeof(System.IntPtr)) return new TypeDiffer(nintDiff);
            if (fieldType == typeof(long) || fieldType == typeof(System.Int64)) return new TypeDiffer(longDiff);
            if (fieldType == typeof(ulong) || fieldType == typeof(System.UInt64)) return new TypeDiffer(ulongDiff);
            if (fieldType == typeof(short) || fieldType == typeof(System.Int16)) return new TypeDiffer(shortDiff);
            if (fieldType == typeof(ushort) || fieldType == typeof(System.UInt16)) return new TypeDiffer(ushortDiff);
            Debug.LogError($"field type {fieldType} not implemented");
            return default;
        }

         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffBool(byte* a, byte* b, float epsilon) => *(bool*)a != *(bool*)b;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffByte(byte* a, byte* b, float epsilon) => math.abs(*(byte*)b - *(byte*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffSbyte(byte* a, byte* b, float epsilon) => math.abs(*(sbyte*)b - *(sbyte*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffDouble(byte* a, byte* b, float epsilon) => math.abs(*(double*)b - *(double*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffFloat(byte* a, byte* b, float epsilon) => math.abs(*(float*)b - *(float*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffInt(byte* a, byte* b, float epsilon) => math.abs(*(int*)b - *(int*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffUint(byte* a, byte* b, float epsilon) => math.abs(*(uint*)b - *(uint*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffNint(byte* a, byte* b, float epsilon) => math.abs(*(nint*)b - *(nint*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffLong(byte* a, byte* b, float epsilon) => math.abs(*(long*)b - *(long*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffUlong(byte* a, byte* b, float epsilon) => math.abs(*(ulong*)b - *(ulong*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffShort(byte* a, byte* b, float epsilon) => math.abs(*(short*)b - *(short*)a) > epsilon;
         [BurstCompile][AOT.MonoPInvokeCallback(typeof(DiffDelegate))] public static unsafe bool DiffUshort(byte* a, byte* b, float epsilon) => math.abs(*(ushort*)b - *(ushort*)a) > epsilon;
         public unsafe delegate bool DiffDelegate(byte* a, byte* b, float epsilon);

    }

    internal struct AllTypeDiffers : IDisposable
    {
        public struct StaticKey { }

        static readonly SharedStatic<AllTypeDiffers> s_Instance = SharedStatic<AllTypeDiffers>.GetOrCreate<AllTypeDiffers, StaticKey>();

        public NativeHashMap<ComponentType, TypeDiffer> AllDiffers;
        bool m_Initialized;
        public static ref AllTypeDiffers Instance {
            get
            {
                if (!s_Instance.Data.m_Initialized)
                {
                    s_Instance.Data.Initialize();
                }
                return ref s_Instance.Data;
            }
        }

        void Initialize()
        {
            AllDiffers = new(10, Allocator.Persistent);
            m_Initialized = true;
        }

        public void Dispose()
        {
            if(!m_Initialized)
                return;
            foreach (var kvPair in AllDiffers)
            {
                kvPair.Value.Dispose();
            }
            AllDiffers.Dispose();
            m_Initialized = false;
        }

        public void TryAddTypeDiffer(ComponentType type)
        {
            if (!AllDiffers.ContainsKey(type))
                AllDiffers.Add(type, new TypeDiffer(type, Allocator.Persistent));
        }
    }
}
