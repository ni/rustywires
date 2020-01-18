﻿using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public static class DataTypes
    {
        public const string RebarTypeKeyword = "RebarType";

        private static NIType MutableReferenceGenericType { get; }

        private static NIType ImmutableReferenceGenericType { get; }

        private static NIType PolymorphicReferenceGenericType { get; }

        public static NIType CloneInterfaceType { get; }

        public static NIType DropInterfaceType { get; }

        public static NIType DisplayInterfaceType { get; }

        internal static NIType OptionGenericType { get; }

        internal static NIType LockingCellGenericType { get; }

        private static NIType SharedGenericType { get; }

        private static NIType IteratorInterfaceGenericType { get; }

        public static NIType RangeIteratorType { get; }

        private static NIType VectorGenericType { get; }

        public static NIType SliceGenericType { get; }

        public static NIType StringSliceType { get; }

        private static NIType StringSplitIteratorGenericType { get; }

        public static NIType FileHandleType { get; }

        public static NIType FakeDropType { get; }

        public static NIType PromiseInterfaceGenericType { get; }

        internal static NIType WakerType { get; }

        public static NIType YieldPromiseGenericType { get; }

        public static NIType MethodCallPromiseGenericType { get; }

        public static NIType NotifierReaderGenericType { get; }

        public static NIType NotifierWriterGenericType { get; }

        internal static NIType NotifierReaderPromiseGenericType { get; }

        static DataTypes()
        {
            var mutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("MutableReference");
            mutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife");
            mutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            MutableReferenceGenericType = mutableReferenceGenericTypeBuilder.CreateType();

            var immutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("ImmutableReference");
            immutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife");
            immutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            ImmutableReferenceGenericType = immutableReferenceGenericTypeBuilder.CreateType();

            var polymorphicReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("PolymorphicReference");
            polymorphicReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife", "TMut");
            polymorphicReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            PolymorphicReferenceGenericType = polymorphicReferenceGenericTypeBuilder.CreateType();

            var cloneInterfaceBuilder = PFTypes.Factory.DefineReferenceInterface("Clone");
            cloneInterfaceBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            CloneInterfaceType = cloneInterfaceBuilder.CreateType();

            var dropInterfaceBuilder = PFTypes.Factory.DefineReferenceInterface("Drop");
            dropInterfaceBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            DropInterfaceType = dropInterfaceBuilder.CreateType();

            var displayInterfaceBuilder = PFTypes.Factory.DefineReferenceInterface("Display");
            displayInterfaceBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            DisplayInterfaceType = displayInterfaceBuilder.CreateType();

            var optionGenericTypeBuilder = PFTypes.Factory.DefineValueClass("Option");
            optionGenericTypeBuilder.MakeGenericParameters("T");
            optionGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            OptionGenericType = optionGenericTypeBuilder.CreateType();

            var lockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("LockingCell");
            lockingCellGenericTypeBuilder.MakeGenericParameters("T");
            lockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            LockingCellGenericType = lockingCellGenericTypeBuilder.CreateType();

            var sharedGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Shared");
            sharedGenericTypeBuilder.MakeGenericParameters("T");
            sharedGenericTypeBuilder.DefineImplementedInterfaceFromExisting(CloneInterfaceType);
            sharedGenericTypeBuilder.DefineImplementedInterfaceFromExisting(DropInterfaceType);
            sharedGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            SharedGenericType = sharedGenericTypeBuilder.CreateType();

            var iteratorGenericTypeBuilder = PFTypes.Factory.DefineReferenceInterface("Iterator");
            iteratorGenericTypeBuilder.MakeGenericParameters("TItem");
            iteratorGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            IteratorInterfaceGenericType = iteratorGenericTypeBuilder.CreateType();

            var rangeIteratorTypeBuilder = PFTypes.Factory.DefineReferenceClass("RangeIterator");
            var iteratorSpecialization = IteratorInterfaceGenericType.ReplaceGenericParameters(PFTypes.Int32);
            rangeIteratorTypeBuilder.DefineImplementedInterfaceFromExisting(iteratorSpecialization);
            rangeIteratorTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            RangeIteratorType = rangeIteratorTypeBuilder.CreateType();

            var stringSliceTypeBuilder = PFTypes.Factory.DefineValueClass("StringSlice");
            stringSliceTypeBuilder.DefineImplementedInterfaceFromExisting(DisplayInterfaceType);
            stringSliceTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            StringSliceType = stringSliceTypeBuilder.CreateType();

            var stringSplitIteratorTypeBuilder = PFTypes.Factory.DefineValueClass("StringSplitIterator");
            NIType sliceReferenceLifetimeParameter = AddGenericLifetimeTypeParameter(stringSplitIteratorTypeBuilder, "TLife");
            iteratorSpecialization = IteratorInterfaceGenericType.ReplaceGenericParameters(
                StringSliceType.CreateImmutableReference(sliceReferenceLifetimeParameter));
            stringSplitIteratorTypeBuilder.DefineImplementedInterfaceFromExisting(iteratorSpecialization);
            stringSplitIteratorTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            StringSplitIteratorGenericType = stringSplitIteratorTypeBuilder.CreateType();

            var vectorGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Vector");
            vectorGenericTypeBuilder.MakeGenericParameters("T");
            vectorGenericTypeBuilder.DefineImplementedInterfaceFromExisting(DropInterfaceType);
            vectorGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            VectorGenericType = vectorGenericTypeBuilder.CreateType();

            var sliceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Slice");
            sliceGenericTypeBuilder.MakeGenericParameters("TElem");
            sliceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            SliceGenericType = sliceGenericTypeBuilder.CreateType();

            var fileHandleTypeBuilder = PFTypes.Factory.DefineValueClass("FileHandle");
            fileHandleTypeBuilder.DefineImplementedInterfaceFromExisting(DropInterfaceType);
            fileHandleTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            FileHandleType = fileHandleTypeBuilder.CreateType();

            var fakeDropTypeBuilder = PFTypes.Factory.DefineValueClass("FakeDrop");
            fakeDropTypeBuilder.DefineImplementedInterfaceFromExisting(DropInterfaceType);
            FakeDropType = fakeDropTypeBuilder.CreateType();

            var promiseInterfaceGenericTypeBuilder = PFTypes.Factory.DefineReferenceInterface("Promise");
            promiseInterfaceGenericTypeBuilder.MakeGenericParameters("TValue");
            promiseInterfaceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            PromiseInterfaceGenericType = promiseInterfaceGenericTypeBuilder.CreateType();

            var wakerTypeBuilder = PFTypes.Factory.DefineValueClass("Waker");
            WakerType = wakerTypeBuilder.CreateType();

            var yieldPromiseGenericTypeBuilder = PFTypes.Factory.DefineValueClass("YieldPromise");
            var yieldParameters = yieldPromiseGenericTypeBuilder.MakeGenericParameters("T");
            var promiseSpecialization = PromiseInterfaceGenericType.ReplaceGenericParameters(yieldParameters.First().CreateType());
            yieldPromiseGenericTypeBuilder.DefineImplementedInterfaceFromExisting(promiseSpecialization);
            yieldPromiseGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            YieldPromiseGenericType = yieldPromiseGenericTypeBuilder.CreateType();

            var methodCallPromiseGenericTypeBuilder = PFTypes.Factory.DefineValueClass("MethodCallPromise");
            var methodCallPromiseParameters = methodCallPromiseGenericTypeBuilder.MakeGenericParameters("T");
            promiseSpecialization = PromiseInterfaceGenericType.ReplaceGenericParameters(methodCallPromiseParameters.First().CreateType());
            methodCallPromiseGenericTypeBuilder.DefineImplementedInterfaceFromExisting(promiseSpecialization);
            methodCallPromiseGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            MethodCallPromiseGenericType = methodCallPromiseGenericTypeBuilder.CreateType();

            var notifierReaderGenericTypeBuilder = PFTypes.Factory.DefineValueClass("NotifierReader");
            notifierReaderGenericTypeBuilder.MakeGenericParameters("T");
            notifierReaderGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            NotifierReaderGenericType = notifierReaderGenericTypeBuilder.CreateType();

            var notifierWriterGenericTypeBuilder = PFTypes.Factory.DefineValueClass("NotifierWriter");
            notifierWriterGenericTypeBuilder.MakeGenericParameters("T");
            notifierWriterGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            NotifierWriterGenericType = notifierWriterGenericTypeBuilder.CreateType();

            var notifierReaderPromiseGenericTypeBuilder = PFTypes.Factory.DefineValueClass("NotifierReaderPromise");
            var notifierParameters = notifierReaderPromiseGenericTypeBuilder.MakeGenericParameters("T");
            promiseSpecialization = PromiseInterfaceGenericType.ReplaceGenericParameters(notifierParameters.First().CreateType().CreateOption());
            notifierReaderPromiseGenericTypeBuilder.DefineImplementedInterfaceFromExisting(promiseSpecialization);
            notifierReaderPromiseGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            NotifierReaderPromiseGenericType = notifierReaderPromiseGenericTypeBuilder.CreateType();
        }

        private static NIType SpecializeGenericType(NIType genericTypeDefinition, params NIType[] typeParameters)
        {
            var specializationTypeBuilder = genericTypeDefinition.DefineClassFromExisting();
            specializationTypeBuilder.ReplaceGenericParameters(typeParameters);
            return specializationTypeBuilder.CreateType();
        }

        private static bool IsGenericTypeSpecialization(this NIType type, NIType genericTypeDefinition)
        {
            NIType typeGenericTypeDefinition;
            return TryGetGenericTypeDefinition(type, out typeGenericTypeDefinition)
                && typeGenericTypeDefinition == genericTypeDefinition;
        }

        private static bool TryGetGenericTypeDefinition(this NIType type, out NIType genericTypeDefinition)
        {
            genericTypeDefinition = NIType.Unset;
            try
            {
                genericTypeDefinition = type.GetGenericTypeDefinition();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool TryGetGenericParameterOfSpecialization(this NIType type, NIType genericTypeDefinition, int parameterIndex, out NIType parameter)
        {
            if (type.IsGenericTypeSpecialization(genericTypeDefinition))
            {
                parameter = type.GetGenericParameters().ElementAt(parameterIndex);
                return true;
            }
            parameter = NIType.Unset;
            return false;
        }

        public static void SetLifetimeTypeAttribute(NIAttributedBaseBuilder builder)
        {
            builder.AddAttribute("Lifetime", true, true);
        }

        private static NIType AddGenericLifetimeTypeParameter(NIClassBuilder classBuilder, string name)
        {
            var genericTypeParameters = classBuilder.MakeGenericParameters(name);
            var parameterBuilder = genericTypeParameters.ElementAt(0);
            SetLifetimeTypeAttribute((NIAttributedBaseBuilder)parameterBuilder);
            return parameterBuilder.CreateType();
        }

        public static NIType CreateMutableReference(this NIType dereferenceType, NIType lifetimeType = default(NIType))
        {
            return SpecializeGenericType(MutableReferenceGenericType, dereferenceType, lifetimeType);
        }

        public static bool IsMutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(MutableReferenceGenericType);
        }

        public static NIType CreateImmutableReference(this NIType dereferenceType, NIType lifetimeType = default(NIType))
        {
            return SpecializeGenericType(ImmutableReferenceGenericType, dereferenceType, lifetimeType);
        }

        public static bool IsImmutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(ImmutableReferenceGenericType);
        }

        public static bool IsRebarReferenceType(this NIType type)
        {
            return type.IsImmutableReferenceType() || type.IsMutableReferenceType() || type.IsPolymorphicReferenceType();
        }

        public static NIType GetTypeOrReferentType(this NIType type)
        {
            return type.IsRebarReferenceType()
                ? type.GetGenericParameters().ElementAt(0)
                : type;
        }

        public static NIType GetReferentType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(0);
        }

        public static NIType GetReferenceLifetimeType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(1);
        }

        public static NIType CreatePolymorphicReference(this NIType dereferenceType, NIType lifetimeType, NIType mutabilityType)
        {
            return SpecializeGenericType(PolymorphicReferenceGenericType, dereferenceType, lifetimeType, mutabilityType);
        }

        public static bool IsPolymorphicReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(PolymorphicReferenceGenericType);
        }

        public static NIType GetReferenceMutabilityType(this NIType type)
        {
            if (!type.IsPolymorphicReferenceType())
            {
                throw new ArgumentException("Expected a polymorphic reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(2);
        }

        internal static InputReferenceMutability GetInputReferenceMutabilityFromType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            if (type.IsImmutableReferenceType())
            {
                return InputReferenceMutability.AllowImmutable;
            }
            if (type.IsMutableReferenceType())
            {
                return InputReferenceMutability.RequireMutable;
            }
            return InputReferenceMutability.Polymorphic;
        }

        public static bool IsReferenceToSameTypeAs(this NIType type, NIType other)
        {
            if (!type.IsRebarReferenceType() || !other.IsRebarReferenceType())
            {
                return false;
            }
            return type.GetReferentType() == other.GetReferentType();
        }

        public static NIType CreateOption(this NIType valueType)
        {
            return SpecializeGenericType(OptionGenericType, valueType);
        }

        public static bool IsOptionType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(OptionGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is an Option type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Option type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an Option.</param>
        /// <returns>True if the given type was an Option type; false otherwise.</returns>
        public static bool TryDestructureOptionType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(OptionGenericType, 0, out valueType);
        }

        public static NIType CreateLockingCell(this NIType dereferenceType)
        {
            return SpecializeGenericType(LockingCellGenericType, dereferenceType);
        }

        public static bool IsLockingCellType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(LockingCellGenericType);
        }

        public static NIType CreateShared(this NIType dereferenceType)
        {
            return SpecializeGenericType(SharedGenericType, dereferenceType);
        }

        public static bool IsSharedType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(SharedGenericType);
        }

        public static NIType GetUnderlyingTypeFromRebarType(this NIType rebarType)
        {
            if (rebarType.IsRebarReferenceType())
            {
                return rebarType.GetGenericParameters().ElementAt(0);
            }
            return rebarType;
        }

        /// <summary>
        /// If the given <see cref="type"/> is a LockingCell type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an LockingCell type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an LockingCell.</param>
        /// <returns>True if the given type was an LockingCell type; false otherwise.</returns>
        public static bool TryDestructureLockingCellType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(LockingCellGenericType, 0, out valueType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is a Shared type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Shared type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an Shared.</param>
        /// <returns>True if the given type was an Shared type; false otherwise.</returns>
        public static bool TryDestructureSharedType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(SharedGenericType, 0, out valueType);
        }

        public static bool TryGetImplementedIteratorInterface(this NIType type, out NIType iteratorInterface)
        {
            if (type.IsClassOrInterface())
            {
                foreach (NIType implementedInterface in type.GetInterfaces())
                {
                    if (implementedInterface.IsIteratorType())
                    {
                        iteratorInterface = implementedInterface;
                        return true;
                    }
                }
            }
            iteratorInterface = NIType.Unset;
            return false;
        }

        public static bool IsIteratorType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(IteratorInterfaceGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is an Iterator type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Iterator type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an Iterator.</param>
        /// <returns>True if the given type was an Iterator type; false otherwise.</returns>
        public static bool TryDestructureIteratorType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(IteratorInterfaceGenericType, 0, out valueType);
        }

        public static NIType CreateStringSplitIterator(this NIType lifetimeType)
        {
            return SpecializeGenericType(StringSplitIteratorGenericType, lifetimeType);
        }

        public static bool IsStringSplitIteratorType(this NIType type)
        {
            return IsGenericTypeSpecialization(type, StringSplitIteratorGenericType);
        }

        public static NIType CreateVector(this NIType itemType)
        {
            return SpecializeGenericType(VectorGenericType, itemType);
        }

        public static bool IsVectorType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(VectorGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is a Vector type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Vector type.</param>
        /// <param name="itemType">The inner value type of the given type if it is an Vector.</param>
        /// <returns>True if the given type was an Vector type; false otherwise.</returns>
        public static bool TryDestructureVectorType(this NIType type, out NIType itemType)
        {
            return type.TryGetGenericParameterOfSpecialization(VectorGenericType, 0, out itemType);
        }

        public static NIType CreateSlice(this NIType elementType)
        {
            return SpecializeGenericType(SliceGenericType, elementType);
        }

        public static bool IsSlice(this NIType type)
        {
            return IsGenericTypeSpecialization(type, SliceGenericType);
        }

        public static bool TryDestructureSliceType(this NIType type, out NIType elementType)
        {
            return type.TryGetGenericParameterOfSpecialization(SliceGenericType, 0, out elementType);
        }

        public static NIType CreateYieldPromise(this NIType type)
        {
            return SpecializeGenericType(YieldPromiseGenericType, type);
        }

        public static bool TryDestructureYieldPromiseType(this NIType type, out NIType elementType)
        {
            return type.TryGetGenericParameterOfSpecialization(YieldPromiseGenericType, 0, out elementType);
        }

        public static NIType CreateMethodCallPromise(this NIType type)
        {
            return SpecializeGenericType(MethodCallPromiseGenericType, type);
        }

        public static bool TryDestructureMethodCallPromiseType(this NIType type, out NIType outputType)
        {
            return type.TryGetGenericParameterOfSpecialization(MethodCallPromiseGenericType, 0, out outputType);
        }

        public static NIType CreateNotifierReader(this NIType type)
        {
            return SpecializeGenericType(NotifierReaderGenericType, type);
        }

        public static bool TryDestructureNotifierReaderType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(NotifierReaderGenericType, 0, out valueType);
        }

        public static NIType CreateNotifierReaderPromise(this NIType type)
        {
            return SpecializeGenericType(NotifierReaderPromiseGenericType, type);
        }

        public static bool TryDestructureNotifierReaderPromiseType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(NotifierReaderPromiseGenericType, 0, out valueType);
        }

        public static NIType CreateNotifierWriter(this NIType type)
        {
            return SpecializeGenericType(NotifierWriterGenericType, type);
        }

        public static bool TryDestructureNotifierWriterType(this NIType type, out NIType valueType)
        {
            return type.TryGetGenericParameterOfSpecialization(NotifierWriterGenericType, 0, out valueType);
        }

        internal static NIType CreateNotifierSharedDataType(this NIType valueType)
        {
            NIClusterBuilder clusterBuilder = PFTypes.Factory.DefineCluster();
            clusterBuilder.DefineField(WakerType, "waker");
            clusterBuilder.DefineField(valueType, "value");
            clusterBuilder.DefineField(PFTypes.Int32, "state");
            return clusterBuilder.CreateType();
        }

        internal static bool WireTypeMayFork(this NIType wireType)
        {
            if (wireType.IsImmutableReferenceType())
            {
                return true;
            }

            if (wireType.IsNumeric() || wireType.IsBoolean())
            {
                return true;
            }

            NIType optionValueType;
            if (wireType.TryDestructureOptionType(out optionValueType))
            {
                return WireTypeMayFork(optionValueType);
            }

            return false;
        }

        internal static bool IsSupportedIntegerType(this NIType type)
        {
            return type.IsInt32() || (type.IsInteger() && RebarFeatureToggles.IsAllIntegerTypesEnabled);
        }

        internal static bool TypeHasDisplayTrait(this NIType type)
        {
            if (type.IsString() || type == StringSliceType)
            {
                return RebarFeatureToggles.IsStringDataTypeEnabled;
            }
            return type.IsSupportedIntegerType() || type.IsBoolean();
        }

        internal static bool TypeHasDropTrait(this NIType type)
        {
            return type == PFTypes.String || type.IsOrImplements(DropInterfaceType);
        }

        internal static bool TypeHasCloneTrait(this NIType type)
        {
            return type == PFTypes.String || type.IsOrImplements(CloneInterfaceType);
        }

        internal static NIType DefineTupleType(this IEnumerable<NIType> fieldTypes)
        {
            NIClusterBuilder clusterBuilder = PFTypes.Factory.DefineCluster();
            int index = 0;
            foreach (NIType type in fieldTypes)
            {
                clusterBuilder.DefineField(type, $"_{index}");
                ++index;
            }
            return clusterBuilder.CreateType();
        }
    }
}
