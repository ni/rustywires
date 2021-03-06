﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.LLVM.CodeGen;
using DfirBorderNode = NationalInstruments.Dfir.BorderNode;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Rebar.RebarTarget
{
    internal sealed class AsyncStateGrouper : VisitorTransformBase
    {
        private AsyncStateGroup CreateGroupThatUnconditionallySchedulesSuccessors(string label, Diagram diagram)
        {
            var group = new AsyncStateGroup(
                label,
                new List<Visitation>(),
                new HashSet<AsyncStateGroup>(),
                new UnconditionallySchduleGroupsContinuation());
            _groups.Add(group);
            _groupDiagrams[group] = diagram;
            return group;
        }

        private AsyncStateGroup CreateGroupThatConditionallySchedulesSuccessors(string label, Diagram diagram)
        {
            var group = new AsyncStateGroup(
                label,
                new List<Visitation>(),
                new HashSet<AsyncStateGroup>(),
                new ConditionallyScheduleGroupsContinuation());
            _groups.Add(group);
            _groupDiagrams[group] = diagram;
            return group;
        }

        private void AddVisitationToGroup(AsyncStateGroup group, Visitation visitation)
        {
            ((List<Visitation>)group.Visitations).Add(visitation);
        }

        /// <summary>
        /// The <see cref="AsyncStateGroup"/> in which each <see cref="Node"/> will run.
        /// </summary>
        private readonly Dictionary<Node, AsyncStateGroup> _nodeGroups = new Dictionary<Node, AsyncStateGroup>();

        /// <summary>
        /// The <see cref="Diagram"/> corresponding to each <see cref="AsyncStateGroup"/>.
        /// </summary>
        private readonly Dictionary<AsyncStateGroup, Diagram> _groupDiagrams = new Dictionary<AsyncStateGroup, Diagram>();

        /// <summary>
        /// The <see cref="AsyncStateGroup"/> containing the first code to run for a particular <see cref="Diagram"/>.
        /// </summary>
        private readonly Dictionary<Diagram, AsyncStateGroup> _diagramInitialGroups = new Dictionary<Diagram, AsyncStateGroup>();

        /// <summary>
        /// The <see cref="AsyncStateGroup"/> containing the first code to run for a particular <see cref="Structure"/>.
        /// </summary>
        private readonly Dictionary<Structure, AsyncStateGroup> _structureInitialGroups = new Dictionary<Structure, AsyncStateGroup>();

        /// <summary>
        /// The <see cref="AsyncStateGroup"/> in which the input <see cref="BorderNode"/>s of a <see cref="Structure"/> will run.
        /// </summary>
        private readonly Dictionary<Structure, AsyncStateGroup> _structureInputBorderNodeGroups = new Dictionary<Structure, AsyncStateGroup>();

        /// <summary>
        /// The <see cref="AsyncStateGroup"/> in which the output <see cref="BorderNode"/>s of a <see cref="Structure"/> will run.
        /// </summary>
        private readonly Dictionary<Structure, AsyncStateGroup> _structureOutputBorderNodeGroups = new Dictionary<Structure, AsyncStateGroup>();

        /// <summary>
        /// The total set of <see cref="AsyncStateGroup"/>s.
        /// </summary>
        private readonly List<AsyncStateGroup> _groups = new List<AsyncStateGroup>();

        private readonly Dictionary<Frame, AsyncStateGroup> _frameSkippedBlockGroups = new Dictionary<Frame, AsyncStateGroup>();

        private DfirRoot _dfirRoot;

        public AsyncStateGrouper()
        {
        }

        public IEnumerable<AsyncStateGroup> GetAsyncStateGroups()
        {
            AddFinalGroupIfNecessary();
            SetSkippableGroups();
            return _groups;
        }

        private void AddFinalGroupIfNecessary()
        {
            var groupsWithoutSuccessors = _groups.Where(group => !group.Continuation.Successors.Any()).ToList();
            if (groupsWithoutSuccessors.HasMoreThan(1))
            {
                AsyncStateGroup finalGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                    "terminalGroup",
                    _dfirRoot.BlockDiagram);
                foreach (AsyncStateGroup predecessor in groupsWithoutSuccessors)
                {
                    AddUnconditionalSuccessorGroup(predecessor, finalGroup);
                }
            }
        }

        private enum GroupTraversalState { NotVisited, VisitedAndNotSkippable, Skippable };

        private void SetSkippableGroups()
        {
            AsyncStateGroup rootInitialGroup = _diagramInitialGroups[_dfirRoot.BlockDiagram];
            var groupTraversalStates = new Dictionary<AsyncStateGroup, GroupTraversalState>();
            _groups.ForEach(g => groupTraversalStates[g] = GroupTraversalState.NotVisited);
            Queue<AsyncStateGroup> groupQueue = new Queue<AsyncStateGroup>();
            groupQueue.Enqueue(rootInitialGroup);

            while (groupQueue.Any())
            {
                AsyncStateGroup group = groupQueue.Dequeue();
                if (groupTraversalStates[group] == GroupTraversalState.Skippable)
                {
                    continue;
                }

                bool startsWithPanicOrContinue = group.StartsWithPanicOrContinue;
                bool hasSkippablePredecessor = group.Predecessors.Any(g => g.IsSkippable);
                bool isDiagramInitialGroup = group.BeginsAsDiagramInitialGroup;
                bool isSkippable = !isDiagramInitialGroup && (startsWithPanicOrContinue || hasSkippablePredecessor);
                group.IsSkippable = isSkippable;
                bool traverseSuccessors = isSkippable || groupTraversalStates[group] == GroupTraversalState.NotVisited;
                if (traverseSuccessors)
                {
                    group.Continuation.Successors.ForEach(groupQueue.Enqueue);
                }
                groupTraversalStates[group] = isSkippable ? GroupTraversalState.Skippable : GroupTraversalState.VisitedAndNotSkippable;
            }
        }

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            _dfirRoot = dfirRoot;
            var rootInitialGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                "initialGroup",
                dfirRoot.BlockDiagram);
            _diagramInitialGroups[dfirRoot.BlockDiagram] = rootInitialGroup;
            rootInitialGroup.BeginsAsDiagramInitialGroup = true;
            base.VisitDfirRoot(dfirRoot);
        }

        protected override void VisitBorderNode(DfirBorderNode borderNode)
        {
            // TODO: Iterate Nodes may need to await
            if (borderNode.Direction == Direction.Input)
            {
                AsyncStateGroup initialGroup = _structureInputBorderNodeGroups[borderNode.ParentStructure];
                AddNode(initialGroup, borderNode);
            }
            else
            {
                AsyncStateGroup outputBorderNodeGroup = _structureOutputBorderNodeGroups[borderNode.ParentStructure];
                AddNode(outputBorderNodeGroup, borderNode);
            }
        }

        protected override void VisitNode(Node node)
        {
            HashSet<AsyncStateGroup> nodePredecessors = GetNodePredecessorGroups(node).ToHashSet();
            if (node is AwaitNode)
            {
                CreateNewGroupFromNode(node, nodePredecessors);
                return;
            }
            if (node is PanicOrContinueNode)
            {
                AsyncStateGroup group = CreateNewGroupFromNode(node, nodePredecessors);
                group.StartsWithPanicOrContinue = true;
#if FALSE
                AsyncStateGroup singlePredecessor;
                if (nodePredecessors.TryGetSingleElement(out singlePredecessor))
                {
                    group.FunctionId = singlePredecessor.FunctionId;
                }
#endif
                return;
            }

            AsyncStateGroup nodeGroup = GetGroupJoinOfPredecessorGroups(
                $"node{node.UniqueId}",
                node.ParentDiagram,
                nodePredecessors);
            AddNode(nodeGroup, node);
        }

        private AsyncStateGroup GetGroupJoinOfPredecessorGroups(
            string label,
            Diagram diagram,
            HashSet<AsyncStateGroup> nodePredecessors)
        {
            if (nodePredecessors.Count > 1)
            {
                return CreateNewGroupWithPredecessors(label, diagram, nodePredecessors);
            }
            if (nodePredecessors.Count == 0)
            {
                return _diagramInitialGroups[diagram];
            }
            return nodePredecessors.First();
        }

        protected override void VisitWire(Wire wire)
        {
            AsyncStateGroup sourceGroup = GetTerminalPredecessorGroup(wire, wire.SourceTerminal);
            if (sourceGroup == null)
            {
                throw new InvalidStateException("Wire source terminal should have a group");
            }
            AddVisitationToGroup(sourceGroup, new NodeVisitation(wire));
            _nodeGroups[wire] = sourceGroup;
        }

        protected override void VisitStructure(Structure structure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            var frame = structure as Frame;
            var loop = structure as Loop;
            var optionPatternStructure = structure as OptionPatternStructure;
            var variantMatchStructure = structure as VariantMatchStructure;
            if (frame != null)
            {
                VisitFrame(frame, traversalPoint);
            }
            else if (loop != null)
            {
                VisitLoop(loop, traversalPoint);
            }
            else if (optionPatternStructure != null)
            {
                VisitOptionPatternStructure(optionPatternStructure, nestedDiagram, traversalPoint);
            }
            else if (variantMatchStructure != null)
            {
                VisitVariantMatchStructure(variantMatchStructure, nestedDiagram, traversalPoint);
            }
        }

        private IEnumerable<AsyncStateGroup> GetStructureBorderNodePredecessorGroups(
            Structure structure,
            Diagram onDiagram,
            Direction borderNodeDirection)
        {
            return structure.BorderNodes
                .Where(b => b.Direction == borderNodeDirection)
                .SelectMany(b => GetNodePredecessorGroups(b, onDiagram));
        }

        private void VisitFrame(Frame frame, StructureTraversalPoint traversalPoint)
        {
            var predecessors = new HashSet<AsyncStateGroup>();
            AsyncStateGroup currentGroup = null;
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(frame, frame.ParentDiagram, Direction.Input));
                        if (!frame.DoesStructureExecuteConditionally())
                        {
                            currentGroup = GetGroupJoinOfPredecessorGroups(
                                $"frame{frame.UniqueId}_initialGroup",
                                frame.ParentDiagram,
                                predecessors);
                            _structureInputBorderNodeGroups[frame] = currentGroup;
                            _diagramInitialGroups[frame.Diagram] = currentGroup;
                        }
                        else
                        {
                            AsyncStateGroup frameInitialGroup = CreateGroupThatConditionallySchedulesSuccessors(
                                $"frame{frame.UniqueId}_initialGroup",
                                frame.ParentDiagram);
                            foreach (var predecessor in predecessors)
                            {
                                AddUnconditionalSuccessorGroup(predecessor, frameInitialGroup);
                            }
                            currentGroup = frameInitialGroup;
                            _structureInitialGroups[frame] = frameInitialGroup;
                            _structureInputBorderNodeGroups[frame] = frameInitialGroup;
                            AsyncStateGroup diagramInitialGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                                $"frame{frame.UniqueId}_diagramInitialGroup",
                                frame.Diagram);
                            diagramInitialGroup.BeginsAsDiagramInitialGroup = true;
                            _diagramInitialGroups[frame.Diagram] = diagramInitialGroup;

                            AsyncStateGroup frameSkippedGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                                $"frame{frame.UniqueId}_frameSkippedGroup",
                                null);
                            AddVisitationToGroup(frameSkippedGroup, new FrameSkippedBlockVisitation(frame));
                            _frameSkippedBlockGroups[frame] = frameSkippedGroup;

                            AsyncStateGroup frameTerminalGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                                $"frame{frame.UniqueId}_terminalGroup",
                                frame.ParentDiagram);
                            frameTerminalGroup.SignaledConditionally = true;
                            AddConditionalSuccessorGroups(frameInitialGroup, new HashSet<AsyncStateGroup> { frameSkippedGroup });  // false/0
                            AddUnconditionalSuccessorGroup(frameSkippedGroup, frameTerminalGroup);
                            AddConditionalSuccessorGroups(frameInitialGroup, new HashSet<AsyncStateGroup> { diagramInitialGroup }); // true/1
                            _nodeGroups[frame] = frameTerminalGroup;
                        }
                        break;
                    }
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        currentGroup = _structureInputBorderNodeGroups[frame];
                        break;
                    }
                case StructureTraversalPoint.AfterAllDiagramsAndBeforeRightBorderNodes:
                    {
                        // look at all output border nodes' predecessors and the groups of all nodes with no successors
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(frame, frame.Diagram, Direction.Output));
                        foreach (Node node in frame.Diagram.Nodes)
                        {
                            if (!node.GetDownstreamNodesSameDiagram(false).Any())
                            {
                                predecessors.Add(_nodeGroups[node]);
                            }
                        }
                        currentGroup = GetGroupJoinOfPredecessorGroups(
                            $"frame{frame.UniqueId}_diagramTerminalGroup",
                            frame.Diagram,
                            predecessors);
                        if (!frame.DoesStructureExecuteConditionally())
                        {
                            _nodeGroups[frame] = currentGroup;
                        }
                        else
                        {
                            AsyncStateGroup frameTerminalGroup = _nodeGroups[frame];
                            AddUnconditionalSuccessorGroup(currentGroup, frameTerminalGroup);
                        }
                        _structureOutputBorderNodeGroups[frame] = currentGroup;
                        break;
                    }
                case StructureTraversalPoint.AfterRightBorderNodes:
                    {
                        AsyncStateGroup frameTerminalGroup = _nodeGroups[frame];
                        currentGroup = frameTerminalGroup;

                        // attempt to consolidate groups
                        if (frame.DoesStructureExecuteConditionally())
                        {
                            AsyncStateGroup diagramInitialGroup = _diagramInitialGroups[frame.Diagram],
                                diagramTerminalGroup = _structureOutputBorderNodeGroups[frame];
                            if (diagramInitialGroup == diagramTerminalGroup)
                            {
                                AsyncStateGroup frameInitialGroup = _structureInitialGroups[frame];
                                diagramTerminalGroup.FunctionId = frameInitialGroup.FunctionId;
                                frameTerminalGroup.FunctionId = frameInitialGroup.FunctionId;

                                AsyncStateGroup frameSkippedGroup = _frameSkippedBlockGroups[frame];
                                frameSkippedGroup.FunctionId = frameInitialGroup.FunctionId;
                            }
                        }
                        break;
                    }
            }
            if (currentGroup != null)
            {
                AddVisitationToGroup(currentGroup, new StructureVisitation(frame, frame.Diagram, traversalPoint));
            }
        }

        private void VisitLoop(Loop loop, StructureTraversalPoint traversalPoint)
        {
            AsyncStateGroup currentGroup = null;
            var predecessors = new HashSet<AsyncStateGroup>();
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(loop, loop.ParentDiagram, Direction.Input));
                        AsyncStateGroup loopInitialGroup = GetGroupJoinOfPredecessorGroups(
                            $"loop{loop.UniqueId}_initialGroup",
                            loop.ParentDiagram,
                            predecessors);
                        _structureInitialGroups[loop] = loopInitialGroup;
                        currentGroup = loopInitialGroup;

                        AsyncStateGroup loopInputBorderNodeGroup = CreateGroupThatConditionallySchedulesSuccessors(
                            $"loop{loop.UniqueId}_inputBNGroup",
                            loop.ParentDiagram);
                        loopInputBorderNodeGroup.SignaledConditionally = true;
                        _structureInputBorderNodeGroups[loop] = loopInputBorderNodeGroup;
                        AddUnconditionalSuccessorGroup(loopInitialGroup, loopInputBorderNodeGroup);
                        break;
                    }
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        AsyncStateGroup diagramInitialGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                            $"loop{loop.UniqueId}_diagramInitialGroup",
                            loop.Diagram),
                            loopTerminalGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                                $"loop{loop.UniqueId}_terminalGroup",
                                loop.ParentDiagram),
                            loopInputBorderNodeGroup = _structureInputBorderNodeGroups[loop];
                        diagramInitialGroup.BeginsAsDiagramInitialGroup = true;
                        currentGroup = loopInputBorderNodeGroup;
                        _diagramInitialGroups[loop.Diagram] = diagramInitialGroup;
                        _nodeGroups[loop] = loopTerminalGroup;
                        AddConditionalSuccessorGroups(loopInputBorderNodeGroup, new HashSet<AsyncStateGroup>() { loopTerminalGroup });
                        AddConditionalSuccessorGroups(loopInputBorderNodeGroup, new HashSet<AsyncStateGroup>() { diagramInitialGroup });
                        break;
                    }
                case StructureTraversalPoint.AfterAllDiagramsAndBeforeRightBorderNodes:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(loop, loop.Diagram, Direction.Output));
                        foreach (Node node in loop.Diagram.Nodes)
                        {
                            if (!node.GetDownstreamNodesSameDiagram(false).Any())
                            {
                                predecessors.Add(_nodeGroups[node]);
                            }
                        }
                        AsyncStateGroup diagramTerminalGroup = GetGroupJoinOfPredecessorGroups(
                            $"loop{loop.UniqueId}_diagramTerminalGroup",
                            loop.Diagram,
                            predecessors);
                        _structureOutputBorderNodeGroups[loop] = diagramTerminalGroup;
                        AddUnconditionalSuccessorGroup(diagramTerminalGroup, _structureInputBorderNodeGroups[loop]);
                        break;
                    }
                case StructureTraversalPoint.AfterRightBorderNodes:
                    {
                        AsyncStateGroup diagramTerminalGroup = _structureOutputBorderNodeGroups[loop];
                        currentGroup = diagramTerminalGroup;
                        break;
                    }
            }
            if (currentGroup != null)
            {
                AddVisitationToGroup(currentGroup, new StructureVisitation(loop, loop.Diagram, traversalPoint));
            }
        }

        private void VisitOptionPatternStructure(OptionPatternStructure optionPatternStructure, Diagram diagram, StructureTraversalPoint traversalPoint)
        {
            var predecessors = new HashSet<AsyncStateGroup>();
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(
                            optionPatternStructure,
                            optionPatternStructure.ParentDiagram,
                            Direction.Input));
                        AsyncStateGroup structureInitialGroup = GetGroupJoinOfPredecessorGroups(
                            $"optionPatternStructure{optionPatternStructure.UniqueId}_initialGroup",
                            optionPatternStructure.ParentDiagram,
                            predecessors);
                        _structureInitialGroups[optionPatternStructure] = structureInitialGroup;
                        AsyncStateGroup structureInputBorderNodeGroup = CreateGroupThatConditionallySchedulesSuccessors(
                            $"optionPatternStructure{optionPatternStructure.UniqueId}_inputBNGroup",
                            optionPatternStructure.ParentDiagram);
                        AddUnconditionalSuccessorGroup(structureInitialGroup, structureInputBorderNodeGroup);
                        _structureInputBorderNodeGroups[optionPatternStructure] = structureInputBorderNodeGroup;

                        AddVisitationToGroup(
                            structureInputBorderNodeGroup,
                            new StructureVisitation(optionPatternStructure, null, StructureTraversalPoint.BeforeLeftBorderNodes));

                        AsyncStateGroup structureTerminalGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                            $"optionPatternStructure{optionPatternStructure.UniqueId}_terminalGroup",
                            optionPatternStructure.ParentDiagram);
                        structureTerminalGroup.SignaledConditionally = true;
                        _nodeGroups[optionPatternStructure] = structureTerminalGroup;
                        _structureOutputBorderNodeGroups[optionPatternStructure] = structureTerminalGroup;
                        break;
                    }
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        AsyncStateGroup structureInputBorderNodeGroup = _structureInputBorderNodeGroups[optionPatternStructure];
                        AsyncStateGroup diagramInitialGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                            $"diagram{diagram.UniqueId}_initialGroup",
                            diagram);
                        diagramInitialGroup.BeginsAsDiagramInitialGroup = true;
                        _diagramInitialGroups[diagram] = diagramInitialGroup;
                        AddConditionalSuccessorGroups(structureInputBorderNodeGroup, new HashSet<AsyncStateGroup>() { diagramInitialGroup });
                        AddVisitationToGroup(
                            diagramInitialGroup,
                            new StructureVisitation(optionPatternStructure, diagram, StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram));
                        break;
                    }
                case StructureTraversalPoint.AfterDiagram:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(
                            optionPatternStructure,
                            diagram,
                            Direction.Output));
                        foreach (Node node in diagram.Nodes)
                        {
                            if (!node.GetDownstreamNodesSameDiagram(false).Any())
                            {
                                predecessors.Add(_nodeGroups[node]);
                            }
                        }
                        AsyncStateGroup diagramTerminalGroup = GetGroupJoinOfPredecessorGroups(
                            $"diagram{diagram.UniqueId}_terminalGroup",
                            diagram,
                            predecessors);
                        AsyncStateGroup structureTerminalGroup = _nodeGroups[optionPatternStructure];
                        AddUnconditionalSuccessorGroup(diagramTerminalGroup, structureTerminalGroup);
                        AddVisitationToGroup(
                            diagramTerminalGroup,
                            new StructureVisitation(optionPatternStructure, diagram, StructureTraversalPoint.AfterDiagram));
                        break;
                    }
            }
        }

        private void VisitVariantMatchStructure(VariantMatchStructure variantMatchStructure, Diagram diagram, StructureTraversalPoint traversalPoint)
        {
            var predecessors = new HashSet<AsyncStateGroup>();
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(
                            variantMatchStructure,
                            variantMatchStructure.ParentDiagram,
                            Direction.Input));
                        AsyncStateGroup structureInitialGroup = GetGroupJoinOfPredecessorGroups(
                            $"variantMatchStructure{variantMatchStructure.UniqueId}_initialGroup",
                            variantMatchStructure.ParentDiagram,
                            predecessors);
                        _structureInitialGroups[variantMatchStructure] = structureInitialGroup;
                        AsyncStateGroup structureInputBorderNodeGroup = CreateGroupThatConditionallySchedulesSuccessors(
                            $"variantMatchStructure{variantMatchStructure.UniqueId}_inputBNGroup",
                            variantMatchStructure.ParentDiagram);
                        AddUnconditionalSuccessorGroup(structureInitialGroup, structureInputBorderNodeGroup);
                        _structureInputBorderNodeGroups[variantMatchStructure] = structureInputBorderNodeGroup;

                        AddVisitationToGroup(
                            structureInputBorderNodeGroup,
                            new StructureVisitation(variantMatchStructure, null, StructureTraversalPoint.BeforeLeftBorderNodes));

                        AsyncStateGroup structureTerminalGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                            $"variantMatchStructure{variantMatchStructure.UniqueId}_terminalGroup",
                            variantMatchStructure.ParentDiagram);
                        structureTerminalGroup.SignaledConditionally = true;
                        _nodeGroups[variantMatchStructure] = structureTerminalGroup;
                        _structureOutputBorderNodeGroups[variantMatchStructure] = structureTerminalGroup;
                        break;
                    }
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    {
                        AsyncStateGroup structureInputBorderNodeGroup = _structureInputBorderNodeGroups[variantMatchStructure];
                        AsyncStateGroup diagramInitialGroup = CreateGroupThatUnconditionallySchedulesSuccessors(
                            $"diagram{diagram.UniqueId}_initialGroup",
                            diagram);
                        diagramInitialGroup.BeginsAsDiagramInitialGroup = true;
                        _diagramInitialGroups[diagram] = diagramInitialGroup;
                        AddConditionalSuccessorGroups(structureInputBorderNodeGroup, new HashSet<AsyncStateGroup>() { diagramInitialGroup });
                        AddVisitationToGroup(
                            diagramInitialGroup,
                            new StructureVisitation(variantMatchStructure, diagram, StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram));
                        break;
                    }
                case StructureTraversalPoint.AfterDiagram:
                    {
                        predecessors.AddRange(GetStructureBorderNodePredecessorGroups(
                            variantMatchStructure,
                            diagram,
                            Direction.Output));
                        foreach (Node node in diagram.Nodes)
                        {
                            if (!node.GetDownstreamNodesSameDiagram(false).Any())
                            {
                                predecessors.Add(_nodeGroups[node]);
                            }
                        }
                        AsyncStateGroup diagramTerminalGroup = GetGroupJoinOfPredecessorGroups(
                            $"diagram{diagram.UniqueId}_terminalGroup",
                            diagram,
                            predecessors);
                        AsyncStateGroup structureTerminalGroup = _nodeGroups[variantMatchStructure];
                        AddUnconditionalSuccessorGroup(diagramTerminalGroup, structureTerminalGroup);
                        AddVisitationToGroup(
                            diagramTerminalGroup,
                            new StructureVisitation(variantMatchStructure, diagram, StructureTraversalPoint.AfterDiagram));
                        break;
                    }
            }
        }

        private void AddUnconditionalSuccessorGroup(AsyncStateGroup predecessor, AsyncStateGroup successor)
        {
            ((UnconditionallySchduleGroupsContinuation)predecessor.Continuation).UnconditionalSuccessors.Add(successor);
            ((HashSet<AsyncStateGroup>)successor.Predecessors).Add(predecessor);
        }

        private void AddConditionalSuccessorGroups(AsyncStateGroup predecessor, HashSet<AsyncStateGroup> successors)
        {
            ((ConditionallyScheduleGroupsContinuation)predecessor.Continuation).SuccessorConditionGroups.Add(successors);
            foreach (AsyncStateGroup successor in successors)
            {
                ((HashSet<AsyncStateGroup>)successor.Predecessors).Add(predecessor);
                successor.SignaledConditionally = true;
            }
        }

        private void AddNode(AsyncStateGroup group, Node node)
        {
            AddVisitationToGroup(group, new NodeVisitation(node));
            _nodeGroups[node] = group;
        }

        private AsyncStateGroup CreateNewGroupFromNode(Node node, IEnumerable<AsyncStateGroup> nodePredecessors)
        {
            AsyncStateGroup group = CreateNewGroupWithPredecessors($"node{node.UniqueId}", node.ParentDiagram, nodePredecessors);
            AddNode(group, node);
            return group;
        }

        private AsyncStateGroup CreateNewGroupWithPredecessors(string label, Diagram diagram, IEnumerable<AsyncStateGroup> nodePredecessors)
        {
            var groupBuilder = CreateGroupThatUnconditionallySchedulesSuccessors(label, diagram);
            foreach (AsyncStateGroup predecessor in nodePredecessors)
            {
                AddUnconditionalSuccessorGroup(predecessor, groupBuilder);
            }
            return groupBuilder;
        }

        private IEnumerable<AsyncStateGroup> GetNodePredecessorGroups(Node node)
        {
            return GetNodePredecessorGroups(node, node.ParentDiagram);
        }

        private IEnumerable<AsyncStateGroup> GetNodePredecessorGroups(Node node, Diagram onDiagram)
        {
            foreach (Terminal inputTerminal in node.InputTerminals.Where(terminal => terminal.ParentDiagram == onDiagram))
            {
                AsyncStateGroup inputTerminalPredecessorGroup = GetTerminalPredecessorGroup(node, inputTerminal);
                if (inputTerminalPredecessorGroup != null)
                {
                    yield return inputTerminalPredecessorGroup;
                }
            }
        }

        private AsyncStateGroup GetTerminalPredecessorGroup(Node terminalParent, Terminal terminal)
        {
            Terminal sourceTerminal = terminal.GetImmediateSourceTerminal();
            if (sourceTerminal != null)
            {
                var sourceBorderNode = sourceTerminal.ParentNode as DfirBorderNode;
                if (sourceBorderNode != null)
                {
                    if (sourceBorderNode.Direction == Direction.Input)
                    {
                        // If the source is an input border node, its group is probably the containing structure's initial group.
                        // Instead of that, use the argument node's diagram's initial group.
                        Diagram parentDiagram = (terminalParent is DfirBorderNode ? terminalParent.ParentNode : terminalParent).ParentDiagram;
                        return _diagramInitialGroups[parentDiagram];
                    }
                    else
                    {
                        // TODO: if the source is an output border node, its group is _structureOutputBorderNodeGroups[sourceBorderNode],
                        // but what we really want is the border node's parent structure's terminal group.
                        // TODO TODO TODO test
                        return _nodeGroups[sourceBorderNode.ParentStructure];
                    }
                }
                else
                {
                    return _nodeGroups[sourceTerminal.ParentNode];
                }
            }
            return null;
        }
    }

    internal sealed class AsyncStateGroup
    {
        public AsyncStateGroup(string groupLabel, IEnumerable<Visitation> visitations, IEnumerable<AsyncStateGroup> predecessors, Continuation continuation)
        {
            Label = groupLabel;
            FunctionId = groupLabel;
            Visitations = visitations;
            Predecessors = predecessors;
            Continuation = continuation;
        }

        public string Label { get; }

        public string FunctionId { get; set; }

        public IEnumerable<Visitation> Visitations { get; private set; }

        public void ReplaceVisitations(IEnumerable<Visitation> visitations)
        {
            Visitations = visitations;
        }

        public bool SignaledConditionally { get; set; }

        public IEnumerable<AsyncStateGroup> Predecessors { get; }

        public int MaxFireCount => SignaledConditionally ? 1 : Predecessors.Count();

        public Continuation Continuation { get; }

        public bool IsSkippable { get; set; }

        public bool StartsWithPanicOrContinue { get; set; }

        public bool BeginsAsDiagramInitialGroup { get; set; }

        /// <remarks>
        /// Currently, this has to be initialized to a default value at the beginning of an AsyncStateGroup's code and then updated with
        /// the real value, since the update happens conditionally for a skippable AsyncStateGroup. Once PHI value initializations are
        /// possible, it would be better to use that since this variable typically only needs to be updated once per execution path.
        /// </remarks>
        public VariableReference ContinuationCondition { get; set; }
    }

    internal abstract class Continuation
    {
        public abstract IEnumerable<AsyncStateGroup> Successors { get; }
    }

    internal class UnconditionallySchduleGroupsContinuation : Continuation
    {
        public HashSet<AsyncStateGroup> UnconditionalSuccessors { get; } = new HashSet<AsyncStateGroup>();

        public override IEnumerable<AsyncStateGroup> Successors => UnconditionalSuccessors;
    }

    internal class ConditionallyScheduleGroupsContinuation : Continuation
    {
        public List<HashSet<AsyncStateGroup>> SuccessorConditionGroups = new List<HashSet<AsyncStateGroup>>();

        public override IEnumerable<AsyncStateGroup> Successors => SuccessorConditionGroups.SelectMany(set => set);
    }

    internal abstract class Visitation
    {
    }

    internal interface IVisitationHandler<T> : IDfirNodeVisitor<T>, IDfirStructureVisitor<T>
    {
        T VisitFrameSkippedBlockVisitation(FrameSkippedBlockVisitation visitation);
    }

    internal static class VisitationExtensions
    {
        public static T Visit<T>(this Visitation visitation, object visitor)
        {
            var nodeVisitation = visitation as NodeVisitation;
            var structureVisitation = visitation as StructureVisitation;
            var frameSkippedBlockVisitation = visitation as FrameSkippedBlockVisitation;
            var codeGenElement = visitation as CodeGenElement;
            if (nodeVisitation != null)
            {
                return ((IDfirNodeVisitor<T>)visitor).VisitRebarNode(nodeVisitation.Node);
            }
            else if (structureVisitation != null)
            {
                return ((IDfirStructureVisitor<T>)visitor).VisitRebarStructure(
                    structureVisitation.Structure,
                    structureVisitation.TraversalPoint,
                    structureVisitation.Diagram);
            }
            else if (frameSkippedBlockVisitation != null)
            {
                return ((IVisitationHandler<T>)visitor).VisitFrameSkippedBlockVisitation(frameSkippedBlockVisitation);
            }
            else if (codeGenElement != null)
            {
                return codeGenElement.AcceptVisitor((ICodeGenElementVisitor<T>)visitor);
            }
            throw new NotSupportedException();
        }

        public static bool GroupContainsNode(this AsyncStateGroup group, Node node)
        {
            return group.Visitations.Any(
                v =>
                {
                    var nodeVisitation = v as NodeVisitation;
                    return nodeVisitation != null
                        && nodeVisitation.Node == node;
                });
        }

        public static bool GroupContainsStructureTraversalPoint(this AsyncStateGroup group, Structure structure, Diagram diagram, StructureTraversalPoint traversalPoint)
        {
            return group.Visitations.Any(
                v =>
                {
                    var structureVisitation = v as StructureVisitation;
                    return structureVisitation != null
                        && structureVisitation.Structure == structure
                        && structureVisitation.Diagram == diagram
                        && structureVisitation.TraversalPoint == traversalPoint;
                });
        }
    }

    internal sealed class NodeVisitation : Visitation
    {
        public NodeVisitation(Node node)
        {
            Node = node;
        }

        public Node Node { get; }
    }

    internal sealed class StructureVisitation : Visitation
    {
        public StructureVisitation(Structure structure, Diagram diagram, StructureTraversalPoint traversalPoint)
        {
            Structure = structure;
            Diagram = diagram;
            TraversalPoint = traversalPoint;
        }

        public Structure Structure { get; }

        public Diagram Diagram { get; }

        public StructureTraversalPoint TraversalPoint { get; }
    }

    internal sealed class FrameSkippedBlockVisitation : Visitation
    {
        public FrameSkippedBlockVisitation(Frame frame)
        {
            Frame = frame;
        }

        public Frame Frame { get; }
    }

    internal static class AsyncStateGroupExtensions
    {
#if DEBUG
        public static string PrettyPrintAsyncStateGroups(this IEnumerable<AsyncStateGroup> asyncStateGroups)
        {
            return string.Join("\n", asyncStateGroups.Select(PrettyPrintAsyncStateGroup));
        }

        private static string PrettyPrintAsyncStateGroup(AsyncStateGroup asyncStateGroup)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"Group {asyncStateGroup.Label}\n");
            stringBuilder.Append("Predecessors:");
            foreach (AsyncStateGroup predecessor in asyncStateGroup.Predecessors)
            {
                stringBuilder.Append(" " + predecessor.Label);
            }
            stringBuilder.Append("\n");
            foreach (Visitation visitation in asyncStateGroup.Visitations)
            {
                stringBuilder.Append(PrettyPrintVisitation(visitation));
                stringBuilder.Append("\n");
            }
            stringBuilder.Append(PrettyPrintContinuation(asyncStateGroup.Continuation));
            stringBuilder.Append("\n");
            return stringBuilder.ToString();
        }

        private static string PrettyPrintContinuation(Continuation continuation)
        {
            var unconditionalContinuation = continuation as UnconditionallySchduleGroupsContinuation;
            var conditionalContinuation = continuation as ConditionallyScheduleGroupsContinuation;
            if (unconditionalContinuation != null)
            {
                return $"Successors: {string.Join(" ", unconditionalContinuation.UnconditionalSuccessors.Select(g => g.Label))}";
            }
            if (conditionalContinuation != null)
            {
                IEnumerable<string> conditionGroupStrings = conditionalContinuation.SuccessorConditionGroups
                    .Select((group, i) => $"({i}: {string.Join(" ", group.Select(g => g.Label))})");
                return $"Successors: {string.Join(", ", conditionGroupStrings)}";
            }
            return string.Empty;
        }

        private static string PrettyPrintVisitation(Visitation visitation)
        {
            var nodeVisitation = visitation as NodeVisitation;
            var structureVisitation = visitation as StructureVisitation;
            var codeGenElement = visitation as CodeGenElement;
            if (nodeVisitation != null)
            {
                Node node = nodeVisitation.Node;
                var functionalNode = node as FunctionalNode;
                string nodeString = functionalNode != null ? functionalNode.Signature.GetName() : node.GetType().Name;
                return $"    {nodeString}({node.UniqueId})";
            }
            if (structureVisitation != null)
            {
                Structure structure = structureVisitation.Structure;
                string diagramString = structureVisitation.Diagram != null
                    ? $" Diagram({structureVisitation.Diagram.UniqueId.ToString()})"
                    : string.Empty;
                return $"    {structure.GetType().Name}({structure.UniqueId}){diagramString} {structureVisitation.TraversalPoint}";
            }
            if (codeGenElement != null)
            {
                return $"    {codeGenElement.ToString()}";
            }
            return string.Empty;
        }
#endif
    }
}
