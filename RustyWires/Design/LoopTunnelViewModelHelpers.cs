﻿using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Restricted;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    internal static class LoopTunnelViewModelHelpers
    {
        /// <summary>
        /// Add Shift Register to a loop command
        /// </summary>
        public static readonly ICommandEx LoopAddBorrowTunnelCommand = new ShellSelectionRelayCommand(HandleAddLoopBorrowTunnel, HandleCanAddLoopBorrowTunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddLoopBorrowTunnelCommand",
            LabelTitle = "Add Borrow Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddLoopBorrowTunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return selection.OfType<LoopViewModel>().Any();
        }

        private static void HandleAddLoopBorrowTunnel(object parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            var structureViewModels = selection.OfType<LoopViewModel>().WhereNotNull();
            if (structureViewModels.Any())
            {
                using (var transaction = structureViewModels.First().TransactionManager.BeginTransaction("Add Borrow Tunnels", TransactionPurpose.User))
                {
                    foreach (var structureViewModel in structureViewModels)
                    {
                        SMRect leftRect, rightRect;
                        BorderNodeViewModelHelpers.FindBorderNodePositions(structureViewModel, out leftRect, out rightRect);
                        var model = (Structure)structureViewModel.Model;

                        LoopBorrowTunnel borrowTunnel = model.MakeBorderNode<LoopBorrowTunnel>();
                        LoopTerminateLifetimeTunnel loopTerminateLifetimeTunnel = model.MakeBorderNode<LoopTerminateLifetimeTunnel>();
                        borrowTunnel.TerminateLifetimeTunnel = loopTerminateLifetimeTunnel;
                        loopTerminateLifetimeTunnel.BeginLifetimeTunnel = borrowTunnel;
                        // Set both as rules were not consistently picking right one to adjust to other.
                        borrowTunnel.Top = leftRect.Y;
                        borrowTunnel.Left = leftRect.X;
                        loopTerminateLifetimeTunnel.Top = borrowTunnel.Top;
                        loopTerminateLifetimeTunnel.Left = rightRect.X;
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
