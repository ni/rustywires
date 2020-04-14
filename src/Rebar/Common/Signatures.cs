﻿using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public static class Signatures
    {
        public static NIType AddGenericDataTypeParameter(NIFunctionBuilder functionBuilder, string name, params NITypeBuilder[] constraints)
        {
            var genericTypeParameters = functionBuilder.MakeGenericParameters(name);
            NITypeBuilder typeBuilder = genericTypeParameters.ElementAt(0);

            // HACK, but there seems to be no other public way to add constraints to an NITypeBuilder
            List<NITypeBuilder> builderConstraints = (List<NITypeBuilder>)((NIGenericTypeBuilder)typeBuilder).Constraints;
            builderConstraints.AddRange(constraints);

            return typeBuilder.CreateType();
        }

        public static NIType AddGenericLifetimeTypeParameter(NIFunctionBuilder functionBuilder, string name)
        {
            var genericTypeParameters = functionBuilder.MakeGenericParameters(name);
            var parameterBuilder = genericTypeParameters.ElementAt(0);
            DataTypes.SetLifetimeTypeAttribute((NIAttributedBaseBuilder)parameterBuilder);
            return parameterBuilder.CreateType();
        }

        private static NIType AddGenericMutabilityTypeParameter(NIFunctionBuilder functionBuilder, string name)
        {
            var genericTypeParameters = functionBuilder.MakeGenericParameters(name);
            var parameterBuilder = genericTypeParameters.ElementAt(0);
            SetMutabilityTypeAttribute((NIAttributedBaseBuilder)parameterBuilder);
            return parameterBuilder.CreateType();
        }

        private static void SetMutabilityTypeAttribute(NIAttributedBaseBuilder builder)
        {
            builder.AddAttribute("Mutability", true, true);
        }

        public static NIFunctionBuilder AddInput(this NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.Required, NIParameterPassingRule.NotAllowed);
            return functionBuilder;
        }

        public static NIFunctionBuilder AddOutput(this NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Optional);
            return functionBuilder;
        }

        public static NIFunctionBuilder AddInputOutput(this NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.Required, NIParameterPassingRule.Optional);
            return functionBuilder;
        }

        static Signatures()
        {
            NITypeBuilder displayTraitConstraintBuilder = PFTypes.Factory.DefineValueInterface("Display");
            NITypeBuilder copyTraitConstraintBuilder = PFTypes.Factory.DefineValueInterface("Copy");
            NITypeBuilder cloneTraitConstraintBuilder = PFTypes.Factory.DefineReferenceInterface("Clone");

            var functionTypeBuilder = PFTypes.Factory.DefineFunction("ImmutPass");
            var tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            ImmutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("MutPass");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            MutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Assign");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "assigneeRef");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            AssignType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Exchange");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            var tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(tLifetimeParameter),
                "exchangeeRef1");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(tLifetimeParameter),
                "exchangeeRef2");
            ExchangeValuesType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateCopy");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData", cloneTraitConstraintBuilder);
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            AddOutput(
                functionTypeBuilder,
                tDataParameter,
                "copy");
            CreateCopyType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Drop");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            DropType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Output");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData", displayTraitConstraintBuilder);
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            OutputType = functionTypeBuilder.CreateType();

            {
                functionTypeBuilder = PFTypes.Factory.DefineFunction("IteratorNext");
                // Technically, the TIterator type should have an Iterator interface constraint related to TItem.
                // This signature is only being used by IterateTunnel currently, which does not need the constraint for type inference;
                // once this becomes a standalone diagram node, the constraint will be necessary for type inference.
                NIType iteratorType = AddGenericDataTypeParameter(functionTypeBuilder, "TIterator");
                NIType itemType = AddGenericDataTypeParameter(functionTypeBuilder, "TItem");
                tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
                AddInputOutput(
                    functionTypeBuilder,
                    iteratorType.CreateMutableReference(tLifetimeParameter),
                    "iteratorRef");
                AddOutput(
                    functionTypeBuilder,
                    itemType.CreateOption(),
                    "item");
                IteratorNextType = functionTypeBuilder.CreateType();
            }

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Inspect");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            InspectType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("FakeDropCreate");
            AddInput(
                functionTypeBuilder,
                PFTypes.Int32,
                "id");
            AddOutput(
                functionTypeBuilder,
                DataTypes.FakeDropType,
                "fakeDrop");
            FakeDropCreateType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("SelectReference");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                PFTypes.Boolean.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "selectorRef");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2");
            var tMutabilityParameter = AddGenericMutabilityTypeParameter(functionTypeBuilder, "TMut");
            var referenceType = tDataParameter.CreatePolymorphicReference(tLifetimeParameter, tMutabilityParameter);
            AddInput(
                functionTypeBuilder,
                referenceType,
                "trueValueRef");
            AddInput(
                functionTypeBuilder,
                referenceType,
                "falseValueRef");
            AddOutput(
                functionTypeBuilder,
                referenceType,
                "selectedValueRef");
            SelectReferenceType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Range");
            AddInput(
                functionTypeBuilder,
                PFTypes.Int32,
                "lowValue");
            AddInput(
                functionTypeBuilder,
                PFTypes.Int32,
                "highValue");
            AddOutput(
                functionTypeBuilder,
                DataTypes.RangeIteratorType,
                "range");
            RangeType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Some");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateOption(),
                "option");
            SomeConstructorType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("None");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateOption(),
                "option");
            NoneConstructorType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("UnwrapOption");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateOption(),
                "option");
            AddOutput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            UnwrapOptionType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("OptionToPanicResult");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateOption(),
                "option");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreatePanicResult(),
                "panicResult");
            OptionToPanicResultType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("StringFromSlice");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "slice");
            AddOutput(
                functionTypeBuilder,
                PFTypes.String,
                "string");
            StringFromSliceType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("StringToSlice");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            AddInput(
                functionTypeBuilder,
                PFTypes.String.CreateImmutableReference(tLifetimeParameter),
                "string");
            AddOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(tLifetimeParameter),
                "slice");
            StringToSliceType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("StringConcat");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "firstSlice");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "secondSlice");
            AddOutput(
                functionTypeBuilder,
                PFTypes.String,
                "combined");
            StringConcatType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("StringAppend");
            AddInputOutput(
                functionTypeBuilder,
                PFTypes.String.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "string");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "slice");
            StringAppendType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("StringSliceToStringSplitIterator");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            AddInput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(tLifetimeParameter),
                "slice");
            AddOutput(
                functionTypeBuilder,
                tLifetimeParameter.CreateStringSplitIterator(),
                "iterator");
            StringSliceToStringSplitIteratorType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorCreate");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateVector(),
                "vector");
            VectorCreateType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorInitialize");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData", copyTraitConstraintBuilder);
            AddInput(functionTypeBuilder, tDataParameter, "element");
            AddInput(functionTypeBuilder, PFTypes.Int32, "size");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateVector(),
                "vector");
            VectorInitializeType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorToSlice");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TElem");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            tMutabilityParameter = AddGenericMutabilityTypeParameter(functionTypeBuilder, "TMut");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateVector().CreatePolymorphicReference(tLifetimeParameter, tMutabilityParameter),
                "vectorRef");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateSlice().CreatePolymorphicReference(tLifetimeParameter, tMutabilityParameter),
                "sliceRef");
            VectorToSliceType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorAppend");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateVector().CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "vectorRef");
            AddInput(functionTypeBuilder, tDataParameter, "element");
            VectorAppendType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorInsert");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateVector().CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "vectorRef");
            AddInputOutput(
                functionTypeBuilder,
                PFTypes.Int32.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "indexRef");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "elementRef");
            VectorInsertType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorRemoveLast");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutput(
                functionTypeBuilder,
                tDataParameter.CreateVector().CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "vectorRef");
            AddOutput(functionTypeBuilder, tDataParameter.CreateOption(), "element");
            VectorRemoveLastType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("SliceIndex");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TElem");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            tMutabilityParameter = AddGenericMutabilityTypeParameter(functionTypeBuilder, "TMut");
            AddInputOutput(
                functionTypeBuilder,
                PFTypes.Int32.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "indexRef");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateSlice().CreatePolymorphicReference(tLifetimeParameter, tMutabilityParameter),
                "sliceRef");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreatePolymorphicReference(tLifetimeParameter, tMutabilityParameter).CreateOption(),
                "elementRef");
            SliceIndexType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateLockingCell");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateLockingCell(),
                "cell");
            CreateLockingCellType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("SharedCreate");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateShared(),
                "shared");
            SharedCreateType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("SharedGetValue");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateShared().CreateImmutableReference(tLifetimeParameter),
                "sharedRef");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(tLifetimeParameter),
                "valueRef");
            SharedGetValueType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("OpenFileHandle");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "filePathRef");
            AddOutput(
                functionTypeBuilder,
                DataTypes.FileHandleType.CreateOption(),
                "fileHandle");
            OpenFileHandleType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("ReadLineFromFileHandle");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.FileHandleType.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "fileHandleRef");
            AddOutput(
                functionTypeBuilder,
                PFTypes.String.CreateOption(),
                "line");
            ReadLineFromFileHandleType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("WriteStringToFileHandle");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.FileHandleType.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "fileHandleRef");
            AddInputOutput(
                functionTypeBuilder,
                DataTypes.StringSliceType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "dataRef");
            WriteStringToFileHandleType = functionTypeBuilder.CreateType();

            {
                functionTypeBuilder = PFTypes.Factory.DefineFunction("Poll");
                NIType promiseType = AddGenericDataTypeParameter(functionTypeBuilder, "TPromise");
                NIType valueType = AddGenericDataTypeParameter(functionTypeBuilder, "TValue");
                AddInputOutput(
                    functionTypeBuilder,
                    promiseType.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                    "promiseRef");
                AddInput(
                    functionTypeBuilder,
                    DataTypes.WakerType,
                    "waker");
                AddOutput(
                    functionTypeBuilder,
                    valueType.CreateOption(),
                    "result");
                PromisePollType = functionTypeBuilder.CreateType();
            }

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Yield");
            AddInputOutput(
                functionTypeBuilder,
                AddGenericDataTypeParameter(functionTypeBuilder, "T").CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "valueRef");
            YieldType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateYieldPromise");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            tLifetimeParameter = AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(tLifetimeParameter),
                "valueRef");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(tLifetimeParameter).CreateYieldPromise(),
                "promise");
            CreateYieldPromiseType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateNotifierPair");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierReader(),
                "reader");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierWriter(),
                "writer");
            CreateNotifierPairType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("GetNotifierValue");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierReader(),
                "reader");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateOption(),
                "value");
            GetNotifierValueType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("GetReaderPromise");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierReader(),
                "reader");
            AddOutput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierReaderPromise(),
                "promise");
            GetReaderPromiseType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("SetNotifierValue");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "T");
            AddInput(
                functionTypeBuilder,
                tDataParameter.CreateNotifierWriter(),
                "writer");
            AddInput(
                functionTypeBuilder,
                tDataParameter,
                "value");
            SetNotifierValueType = functionTypeBuilder.CreateType();
        }

        #region Testing functions

        public static NIType InspectType { get; }

        public static NIType FakeDropCreateType { get; }

        #endregion

        public static NIType ImmutablePassthroughType { get; }

        public static NIType MutablePassthroughType { get; }

        public static NIType AssignType { get; }

        public static NIType ExchangeValuesType { get; }

        public static NIType CreateCopyType { get; }

        // NOTE: this is the type associated with the trait method, not the diagram node.
        public static NIType DropType { get; }

        public static NIType IteratorNextType { get; }

        public static NIType OutputType { get; }

        public static NIType SelectReferenceType { get; }

        public static NIType RangeType { get; }

        public static NIType SomeConstructorType { get; }

        public static NIType NoneConstructorType { get; }

        public static NIType UnwrapOptionType { get; }

        public static NIType OptionToPanicResultType { get; }

        public static NIType StringFromSliceType { get; }

        public static NIType StringToSliceType { get; }

        public static NIType StringConcatType { get; }

        public static NIType StringAppendType { get; }

        public static NIType StringSliceToStringSplitIteratorType { get; }

        public static NIType VectorCreateType { get; }

        public static NIType VectorInitializeType { get; }

        public static NIType VectorToSliceType { get; }

        public static NIType VectorAppendType { get; }

        public static NIType VectorInsertType { get; }

        public static NIType VectorRemoveLastType { get; }

        public static NIType SliceIndexType { get; }

        public static NIType CreateLockingCellType { get; }

        public static NIType SharedCreateType { get; }

        public static NIType SharedGetValueType { get; }

        public static NIType OpenFileHandleType { get; }

        public static NIType WriteStringToFileHandleType { get; }

        public static NIType ReadLineFromFileHandleType { get; }

        public static NIType PromisePollType { get; }

        #region Yield

        public static NIType YieldType { get; }

        public static NIType CreateYieldPromiseType { get; }

        #endregion

        #region Async notifier

        public static NIType CreateNotifierPairType { get; }

        public static NIType GetNotifierValueType { get; }

        public static NIType GetReaderPromiseType { get; }

        public static NIType SetNotifierValueType { get; }

        #endregion

        public static NIType DefinePureUnaryFunction(string name, NIType inputType, NIType outputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "operandRef");
            AddOutput(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefinePureBinaryFunction(string name, NIType inputType, NIType outputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "operand1Ref");
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "operand2Ref");
            AddOutput(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingUnaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife")),
                "operandRef");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingBinaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateMutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "operand1Ref");
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "operand2Ref");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineComparisonFunction(string name)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            NIType inputType = PFTypes.Int32, outputType = PFTypes.Boolean;
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife1")),
                "operand1Ref");
            AddInputOutput(
                functionTypeBuilder,
                inputType.CreateImmutableReference(AddGenericLifetimeTypeParameter(functionTypeBuilder, "TLife2")),
                "operand2Ref");
            AddOutput(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        internal static Signature GetSignatureForNIType(NIType functionSignature)
        {
            List<SignatureTerminal> inputs = new List<SignatureTerminal>(),
                outputs = new List<SignatureTerminal>();
            NIType nonGenericSignature = functionSignature;
            if (nonGenericSignature.IsOpenGeneric())
            {
                NIType[] typeArguments = Enumerable.Range(0, functionSignature.GetGenericParameters().Count)
                    .Select(i => PFTypes.Void)
                    .ToArray();
                nonGenericSignature = nonGenericSignature.ReplaceGenericParameters(typeArguments);
            }
            foreach (var parameterPair in nonGenericSignature.GetParameters().Zip(functionSignature.GetParameters()))
            {
                NIType displayParameter = parameterPair.Key, signatureParameter = parameterPair.Value;
                bool isInput = displayParameter.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isOutput = displayParameter.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isPassthrough = isInput && isOutput;                
                NIType parameterType = displayParameter.GetDataType();
                if (isInput)
                {
                    string name = isOutput ? $"{displayParameter.GetName()}_in" : displayParameter.GetName();
                    inputs.Add(new SignatureTerminal(name, parameterType, signatureParameter.GetDataType(), isPassthrough));
                }
                if (isOutput)
                {
                    string name = isInput ? $"{displayParameter.GetName()}_out" : displayParameter.GetName();
                    outputs.Add(new SignatureTerminal(name, parameterType, signatureParameter.GetDataType(), isPassthrough));
                }
            }

            return new Signature(inputs.ToArray(), outputs.ToArray());
        }

        public static bool IsLifetimeType(this NIType type)
        {
            AttributeValue? attribute = type.TryGetAttributeValue("Lifetime");
            return attribute.HasValue && (bool)attribute.Value.Value;
        }

        public static bool IsMutabilityType(this NIType type)
        {
            AttributeValue? attribute = type.TryGetAttributeValue("Mutability");
            return attribute.HasValue && (bool)attribute.Value.Value;
        }
    }

    public struct Signature
    {
        public Signature(SignatureTerminal[] inputs, SignatureTerminal[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }

        public SignatureTerminal[] Inputs { get; }

        public SignatureTerminal[] Outputs { get; }
    }

    public struct SignatureTerminal
    {
        public SignatureTerminal(string name, NIType displayType, NIType signatureType, bool isPassthrough)
        {
            Name = name;
            DisplayType = displayType;
            SignatureType = signatureType;
            IsPassthrough = isPassthrough;
        }

        public string Name { get; }

        public NIType DisplayType { get; }

        public NIType SignatureType { get; }

        public bool IsPassthrough { get; }
    }
}
