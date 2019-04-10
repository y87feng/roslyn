// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.Host), Shared]
    internal sealed class EditAndContinueWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IActiveStatementProvider _activeStatementProviderOpt;
        private readonly IDebuggeeModuleMetadataProvider _debugeeModuleMetadataProviderOpt;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;

        [ImportingConstructor]
        public EditAndContinueWorkspaceServiceFactory(
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            [Import(AllowDefault = true)]IActiveStatementProvider activeStatementProvider,
            [Import(AllowDefault = true)]IDebuggeeModuleMetadataProvider debugeeModuleMetadataProvider)
        {
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
            _activeStatementProviderOpt = activeStatementProvider;
            _debugeeModuleMetadataProviderOpt = debugeeModuleMetadataProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => (_debugeeModuleMetadataProviderOpt == null || _activeStatementProviderOpt == null) ? null :
                new EditAndContinueWorkspaceService(
                    workspaceServices.Workspace,
                    workspaceServices.Workspace.Services.GetRequiredService<IActiveStatementTrackingService>(),
                    workspaceServices.Workspace.Services.GetRequiredService<ICompilationOutputsProviderService>(),
                    _diagnosticService,
                    _diagnosticUpdateSource,
                    _activeStatementProviderOpt,
                    _debugeeModuleMetadataProviderOpt);
    }
}
