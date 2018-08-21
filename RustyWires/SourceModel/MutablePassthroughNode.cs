﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class MutablePassthroughNode : RustyWiresSimpleNode
    {
        private const string ElementName = "MutablePassthroughNode";

        protected MutablePassthroughNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "reference in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void, "reference out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static MutablePassthroughNode CreateMutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var mutablePassthroughNode = new MutablePassthroughNode();
            mutablePassthroughNode.Init(elementCreateInfo);
            return mutablePassthroughNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 2);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitMutablePassthroughNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
