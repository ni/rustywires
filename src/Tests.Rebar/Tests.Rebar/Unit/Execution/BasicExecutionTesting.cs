﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.FeatureToggles;
using Rebar;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class BasicExecutionTesting : ExecutionTestBase
    {
        private IDisposable _integerTypesFeatureToggle;

        [TestInitialize]
        public void Initialize()
        {
            _integerTypesFeatureToggle = FeatureToggleSupport.TemporarilyEnableFeature(RebarFeatureToggles.AllIntegerTypes);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _integerTypesFeatureToggle.Dispose();
        }

        [TestMethod]
        public void FunctionWithInt8Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int8, (sbyte)1);
            AssertByteArrayIsInt8(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithNegativeInt8Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int8, (sbyte)-1);
            AssertByteArrayIsInt8(inspectValue, -1);
        }

        [TestMethod]
        public void FunctionWithUInt8Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.UInt8, (byte)1);
            AssertByteArrayIsUInt8(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithInt16Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int16, (short)1);
            AssertByteArrayIsInt16(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithNegativeInt16Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int16, (short)-1);
            AssertByteArrayIsInt16(inspectValue, -1);
        }

        [TestMethod]
        public void FunctionWithUInt16Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.UInt16, (ushort)1);
            AssertByteArrayIsUInt16(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithInt32Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int32, 1);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithNegativeInt32Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int32, -1);
            AssertByteArrayIsInt32(inspectValue, -1);
        }

        [TestMethod]
        public void FunctionWithUInt32Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.UInt32, 1u);
            AssertByteArrayIsUInt32(inspectValue, 1u);
        }

        [TestMethod]
        public void FunctionWithInt64Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int64, 1L);
            AssertByteArrayIsInt64(inspectValue, 1L);
        }

        [TestMethod]
        public void FunctionWithNegativeInt64Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.Int64, -1L);
            AssertByteArrayIsInt64(inspectValue, -1L);
        }

        [TestMethod]
        public void FunctionWithUInt64Constant_Execute_CorrectValue()
        {
            byte[] inspectValue = CreateAndExecuteFunctionWithConstantAndInspect(NITypes.UInt64, 1ul);
            AssertByteArrayIsUInt64(inspectValue, 1u);
        }

        private byte[] CreateAndExecuteFunctionWithConstantAndInspect(NIType constantType, object constantValue)
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Constant constant = ConnectConstantToInputTerminal(inspect.InputTerminals[0], constantType, constantValue, false);
            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);
            return executionInstance.GetLastValueFromInspectNode(inspect);
        }

        [TestMethod]
        public void OutputInt8Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Int8, (sbyte)1);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputUInt8Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.UInt8, (byte)1);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputInt16Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Int16, (short)1);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputUInt16Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.UInt16, (ushort)1);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputInt32Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Int32, 1);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputUInt32Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.UInt32, 1u);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputInt64Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Int64, 1L);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputUInt64Constant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.UInt64, 1ul);
            Assert.AreEqual("1", lastOutput);
        }

        [TestMethod]
        public void OutputTrueBooleanConstant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Boolean, true);
            Assert.AreEqual("true", lastOutput);
        }

        [TestMethod]
        public void OutputFalseBooleanConstant_Execute_CorrectOutputValue()
        {
            string lastOutput = CreateAndExecuteFunctionWithConstantAndOutput(NITypes.Boolean, false);
            Assert.AreEqual("false", lastOutput);
        }

        private string CreateAndExecuteFunctionWithConstantAndOutput(NIType constantType, object constantValue)
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            Constant constant = ConnectConstantToInputTerminal(output.InputTerminals[0], constantType, constantValue, false);
            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);
            return executionInstance.RuntimeServices.LastOutputValue;
        }

        [TestMethod]
        public void FunctionWithInt32AssignedToNewValue_Execute_CorrectFinalValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            FunctionalNode assign = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            Wire.Create(function.BlockDiagram, assign.OutputTerminals[0], inspect.InputTerminals[0]);
            Constant finalValue = ConnectConstantToInputTerminal(assign.InputTerminals[1], NITypes.Int32, 2, false);
            Constant initialValue = ConnectConstantToInputTerminal(assign.InputTerminals[0], NITypes.Int32, true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 2);
        }

        [TestMethod]
        public void FunctionWithTwoI32sExchanged_Execute_CorrectFinalValues()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode exchangeValues = new FunctionalNode(function.BlockDiagram, Signatures.ExchangeValuesType);
            FunctionalNode inspect1 = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[0]),
                inspect2 = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[1]);
            Constant value1 = ConnectConstantToInputTerminal(exchangeValues.InputTerminals[0], NITypes.Int32, 1, true),
                value2 = ConnectConstantToInputTerminal(exchangeValues.InputTerminals[1], NITypes.Int32, 2, true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue1 = executionInstance.GetLastValueFromInspectNode(inspect1),
                inspectValue2 = executionInstance.GetLastValueFromInspectNode(inspect2);
            AssertByteArrayIsInt32(inspectValue1, 2);
            AssertByteArrayIsInt32(inspectValue2, 1);
        }

        [TestMethod]
        public void CreateDroppableValue_Execute_ValueIsDropped()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode fakeDropCreate = CreateFakeDropWithId(function.BlockDiagram, 1);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
        }

        [TestMethod]
        public void CreateDroppableValueAndAssignNewDroppableValue_Execute_BothValuesDropped()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode initialFakeDrop = CreateFakeDropWithId(function.BlockDiagram, 1),
                newFakeDrop = CreateFakeDropWithId(function.BlockDiagram, 2);
            FunctionalNode assign = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            Wire.Create(function.BlockDiagram, initialFakeDrop.OutputTerminals[0], assign.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            Wire.Create(function.BlockDiagram, newFakeDrop.OutputTerminals[0], assign.InputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(2));
        }

        [TestMethod]
        public void SelectReferenceWithTrueSelector_Execute_CorrectSelectedResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode selectReference = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Constant selectorConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[0], NITypes.Boolean, true, false);
            Constant trueValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[1], NITypes.Int32, 1, false);
            Constant falseValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[2], NITypes.Int32, 0, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(selectReference.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void SelectReferenceWithFalseSelector_Execute_CorrectSelectedResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode selectReference = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Constant selectorConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[0], NITypes.Boolean, false, false);
            Constant trueValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[1], NITypes.Int32, 1, false);
            Constant falseValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[2], NITypes.Int32, 0, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(selectReference.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 0);
        }

        [TestMethod]
        public void FunctionWithRangeWithInt32Inputs_Execute_CorrectRangeValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode range = new FunctionalNode(function.BlockDiagram, Signatures.RangeType);
            Constant lowValue = ConnectConstantToInputTerminal(range.InputTerminals[0], NITypes.Int32, 0, false);
            Constant highValue = ConnectConstantToInputTerminal(range.InputTerminals[1], NITypes.Int32, 10, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(range.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            Assert.AreEqual(8, inspectValue.Length);
            Assert.AreEqual(-1, BitConverter.ToInt32(inspectValue, 0));
            Assert.AreEqual(10, BitConverter.ToInt32(inspectValue, 4));
        }

        [TestMethod]
        public void ReborrowMutableReferenceAsImmutableAndOutput_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            var mutableBorrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Mutable, 1, true, true);
            ConnectConstantToInputTerminal(mutableBorrow.InputTerminals[0], NITypes.Int32, 5, true);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(mutableBorrow.OutputTerminals[0]);
            // set mutable reference wire to mutable so that it gets a local allocation
            mutableBorrow.OutputTerminals[0].GetWireIfConnected().SetWireBeginsMutableVariable(true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }
    }
}
