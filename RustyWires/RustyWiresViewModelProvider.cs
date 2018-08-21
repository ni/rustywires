﻿using NationalInstruments.Shell;
using NationalInstruments.VI.Design;
using NationalInstruments.VI.SourceModel;
using RustyWires.Design;
using RustyWires.SourceModel;

namespace RustyWires
{
    [ExportProvideViewModels(typeof(RustyWiresDiagramEditor))]
    public class RustyWiresViewModelProvider : ViewModelProvider
    {
        public RustyWiresViewModelProvider()
        {
            AddSupportedModel<DropNode>(n => new BasicNodeViewModel(n, "Drop Value"));
            AddSupportedModel<ImmutablePassthroughNode>(n => new BasicNodeViewModel(n, "Immutable Passthrough"));
            AddSupportedModel<MutablePassthroughNode>(n => new BasicNodeViewModel(n, "Mutable Passthrough"));
            AddSupportedModel<SelectReferenceNode>(n => new BasicNodeViewModel(n, "Select Reference"));
            AddSupportedModel<CreateMutableCopyNode>(n => new BasicNodeViewModel(n, "Create Mutable Copy"));
            AddSupportedModel<ExchangeValues>(n => new BasicNodeViewModel(n, "Exchange Values"));
            AddSupportedModel<CreateCell>(n => new BasicNodeViewModel(n, "Create Cell"));
            AddSupportedModel<ImmutableBorrowNode>(n => new BasicNodeViewModel(n, "Immutable Borrow"));
            AddSupportedModel<Freeze>(n => new BasicNodeViewModel(n, "Freeze"));

            AddSupportedModel<SourceModel.Add>(n => new BasicNodeViewModel(n, "Add"));
            AddSupportedModel<SourceModel.Subtract>(n => new BasicNodeViewModel(n, "Subtract"));
            AddSupportedModel<SourceModel.Multiply>(n => new BasicNodeViewModel(n, "Multiply"));
            AddSupportedModel<SourceModel.Divide>(n => new BasicNodeViewModel(n, "Divide"));
            AddSupportedModel<SourceModel.Increment>(n => new BasicNodeViewModel(n, "Increment"));
            // AddSupportedModel<SourceModel.Divide>(n => new BasicNodeViewModel(n, "Divide"));
            AddSupportedModel<AccumulateAdd>(n => new BasicNodeViewModel(n, "Accumulate Add"));
            AddSupportedModel<AccumulateSubtract>(n => new BasicNodeViewModel(n, "Accumulate Subtract"));
            AddSupportedModel<AccumulateMultiply>(n => new BasicNodeViewModel(n, "Accumulate Multiply"));
            AddSupportedModel<AccumulateDivide>(n => new BasicNodeViewModel(n, "Accumulate Divide"));

            AddSupportedModel<BorrowTunnel>(t => new BorrowTunnelViewModel(t));
            AddSupportedModel<UnborrowTunnel>(t => new UnborrowTunnelViewModel(t));
            AddSupportedModel<RustyWiresFlatSequence>(s => new RustyWiresFlatSequenceEditor(s));
            AddSupportedModel<FlatSequenceDiagram>(d => new FlatSequenceDiagramViewModel(d));
            AddSupportedModel<RustyWiresFlatSequenceSimpleTunnel>(t => new FlatSequenceTunnelViewModel(t));
        }
    }
}
