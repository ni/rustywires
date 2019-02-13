﻿using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class ImmutablePassthroughNode : DfirNode
    {
        private readonly Terminal _inputTerminal, _outputTerminal;

        public ImmutablePassthroughNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            _inputTerminal = CreateTerminal(Direction.Input, immutableReferenceType, "ref in");
            _outputTerminal = CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
        }

        private ImmutablePassthroughNode(Node parentNode, ImmutablePassthroughNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ImmutablePassthroughNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitImmutablePassthroughNode(this);
        }
    }
}