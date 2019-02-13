﻿using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using Rebar.Common;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Rebar-specific implementation of <see cref="WireViewModel"/> for providing custom wire visuals.
    /// </summary>
    public class FunctionWireViewModel : WireViewModel
    {
        /// <summary>
        /// Construct a new <see cref="FunctionWireViewModel"/>.
        /// </summary>
        /// <param name="wire">The wire to create a view model for.</param>
        public FunctionWireViewModel(Wire wire) : base(wire)
        {
        }

        #region Commands

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(WireGroupCommand))
                {
                    context.Add(WireBeginsMutableVariableCommand);
                }
            }
        }

        private static readonly ICommandEx WireGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.Group,
            LabelTitle = "Wire",
            UniqueId = "NI.RebarDiagramNodeCommands:TerminalsGroup"
        };

        private static readonly ICommandEx WireBeginsMutableVariableCommand = new ShellSelectionRelayCommand(
            HandleExecuteWireBeginsMutableVariableCommand,
            HandleCanExecuteWireBeginsMutableVariableCommand)
        {
            UIType = UITypeForCommand.CheckBox,
            LabelTitle = "Wire Variable Is Mutable",
            UniqueId = "NI.RebarDiagramNodeCommands:MutableWireVariable",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        private static bool HandleCanExecuteWireBeginsMutableVariableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            IEnumerable<Wire> selectedWires = selection.GetSelectedModels<Wire>();
            if (!selectedWires.Any())
            {
                return true;
            }
            var checkableCommandParameter = (ICheckableCommandParameter)parameter;
            if (selectedWires.All(wire => wire.GetIsFirstVariableWire()))
            {
                bool firstSetting = selectedWires.First().GetWireBeginsMutableVariable();
                bool multipleSettings = selectedWires.Any(wire => wire.GetWireBeginsMutableVariable() != firstSetting);
                checkableCommandParameter.IsChecked = multipleSettings ? null : (bool?)firstSetting;
                return true;
            }
            else
            {
                checkableCommandParameter.IsChecked = selectedWires.Any(wire => wire.GetWireVariable()?.Mutable ?? false);
                return false;
            }
        }

        private static void HandleExecuteWireBeginsMutableVariableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            IEnumerable<Wire> selectedWires = selection.GetSelectedModels<Wire>();
            if (!selectedWires.Any())
            {
                return;
            }
            using (var transaction = selectedWires.First().TransactionManager.BeginTransaction("Set Mutable Terminal Bindings mode", TransactionPurpose.User))
            {
                bool value = ((ICheckableCommandParameter)parameter).IsChecked ?? false;
                foreach (Wire wire in selectedWires)
                {
                    wire.SetWireBeginsMutableVariable(value);
                }
                transaction.Commit();
            }
        }

        #endregion

        #region Wire visuals

        /// <inheritdoc />
        public override WireRenderInfoEnumerable WireRenderInfo
        {
            get
            {
                Variable variable = ((Wire)Model).GetWireVariable();
                if (variable != null && !variable.Type.IsRebarReferenceType())
                {
                    var stockResources = Host.GetSharedExportedValue<StockDiagramUIResources>();
                    ITypeAssetProvider innerTypeAssetProvider = stockResources.GetTypeAssets(null, variable.Type);
                    ITypeAssetProvider outerAssetProvider = variable.Mutable
                        ? (ITypeAssetProvider)new MutableValueTypeAssetProvider(innerTypeAssetProvider, 0)
                        : new ImmutableValueTypeAssetProvider(innerTypeAssetProvider, 0);
                    return outerAssetProvider.GetWireRenderInfo(0);
                }
                return base.WireRenderInfo;
            }
        }

        #endregion
    }
}