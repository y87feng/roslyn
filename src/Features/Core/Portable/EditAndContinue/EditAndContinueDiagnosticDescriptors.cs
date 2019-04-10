// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class EditAndContinueDiagnosticDescriptors
    {
        private const int GeneralDiagnosticBaseId = 1000;
        private const int ModuleDiagnosticBaseId = 2000;
        private static readonly int s_diagnosticBaseIndex;

        private static readonly LocalizableResourceString s_rudeEditLocString;
        private static readonly LocalizableResourceString s_encLocString;
        private static readonly LocalizableResourceString s_encDisallowedByProjectLocString;

        private static readonly ImmutableArray<DiagnosticDescriptor> s_descriptors;

        // descriptors for diagnostics reported by the debugger:
        private static Dictionary<int, DiagnosticDescriptor> s_lazyModuleDiagnosticDescriptors;
        private static readonly object s_moduleDiagnosticDescriptorsGuard;

        static EditAndContinueDiagnosticDescriptors()
        {
            s_moduleDiagnosticDescriptorsGuard = new object();

            s_rudeEditLocString = new LocalizableResourceString(nameof(FeaturesResources.RudeEdit), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            s_encLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            s_encDisallowedByProjectLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinueDisallowedByProject), FeaturesResources.ResourceManager, typeof(FeaturesResources));

            var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

            void add(int index, int id, string resourceName, LocalizableResourceString title, DiagnosticSeverity severity)
            {
                if (index >= builder.Count)
                {
                    builder.Count = index + 1;
                }

                builder[index] = new DiagnosticDescriptor(
                    $"ENC{id:D4}",
                    title,
                    messageFormat: new LocalizableResourceString(resourceName, FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                    DiagnosticCategory.EditAndContinue,
                    severity,
                    isEnabledByDefault: true,
                    customTags: DiagnosticCustomTags.EditAndContinue);
            }

            void addRudeEdit(RudeEditKind kind, string resourceName)
                => add(GetDescriptorIndex(kind), (int)kind, resourceName, s_rudeEditLocString, DiagnosticSeverity.Error);

            void addGeneralDiagnostic(EditAndContinueErrorCode code, string resourceName, DiagnosticSeverity severity = DiagnosticSeverity.Error)
                => add(GetDescriptorIndex(code), GeneralDiagnosticBaseId + (int)code, resourceName, s_encLocString, severity);

            //
            // rude edits
            //

            addRudeEdit(RudeEditKind.InsertAroundActiveStatement, nameof(FeaturesResources.Adding_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeleteAroundActiveStatement, nameof(FeaturesResources.Deleting_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeleteActiveStatement, nameof(FeaturesResources.An_active_statement_has_been_removed_from_its_original_method_You_must_revert_your_changes_to_continue_or_restart_the_debugging_session));
            addRudeEdit(RudeEditKind.UpdateAroundActiveStatement, nameof(FeaturesResources.Updating_a_0_statement_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdateExceptionHandlerOfActiveTry, nameof(FeaturesResources.Modifying_a_catch_finally_handler_with_an_active_statement_in_the_try_block_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdateTryOrCatchWithActiveFinally, nameof(FeaturesResources.Modifying_a_try_catch_finally_statement_when_the_finally_block_is_active_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdateCatchHandlerAroundActiveStatement, nameof(FeaturesResources.Modifying_a_catch_handler_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.Update, nameof(FeaturesResources.Updating_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ModifiersUpdate, nameof(FeaturesResources.Updating_the_modifiers_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.VarianceUpdate, nameof(FeaturesResources.Updating_the_variance_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.TypeUpdate, nameof(FeaturesResources.Updating_the_type_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InitializerUpdate, nameof(FeaturesResources.Updating_the_initializer_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.FixedSizeFieldUpdate, nameof(FeaturesResources.Updating_the_size_of_a_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.EnumUnderlyingTypeUpdate, nameof(FeaturesResources.Updating_the_underlying_type_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.BaseTypeOrInterfaceUpdate, nameof(FeaturesResources.Updating_the_base_class_and_or_base_interface_s_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.TypeKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_a_type_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.AccessorKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_an_property_event_accessor_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.MethodKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_a_method_Sub_Function_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeclareAliasUpdate, nameof(FeaturesResources.Updating_the_alias_of_Declare_Statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeclareLibraryUpdate, nameof(FeaturesResources.Updating_the_library_name_of_Declare_Statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.FieldKindUpdate, nameof(FeaturesResources.Updating_a_field_to_an_event_or_vice_versa_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.Renamed, nameof(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.Insert, nameof(FeaturesResources.Adding_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertVirtual, nameof(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertOverridable, nameof(FeaturesResources.Adding_a_MustOverride_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertExtern, nameof(FeaturesResources.Adding_an_extern_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertDllImport, nameof(FeaturesResources.Adding_an_imported_method_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertOperator, nameof(FeaturesResources.Adding_a_user_defined_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertIntoStruct, nameof(FeaturesResources.Adding_0_into_a_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertIntoClassWithLayout, nameof(FeaturesResources.Adding_0_into_a_class_with_explicit_or_sequential_layout_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertGenericMethod, nameof(FeaturesResources.Adding_a_generic_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.Move, nameof(FeaturesResources.Moving_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.Delete, nameof(FeaturesResources.Deleting_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.MethodBodyAdd, nameof(FeaturesResources.Adding_a_method_body_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.MethodBodyDelete, nameof(FeaturesResources.Deleting_a_method_body_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.GenericMethodUpdate, nameof(FeaturesResources.Modifying_a_generic_method_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.GenericMethodTriviaUpdate, nameof(FeaturesResources.Modifying_whitespace_or_comments_in_a_generic_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.GenericTypeUpdate, nameof(FeaturesResources.Modifying_a_method_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.GenericTypeTriviaUpdate, nameof(FeaturesResources.Modifying_whitespace_or_comments_in_0_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.GenericTypeInitializerUpdate, nameof(FeaturesResources.Modifying_the_initializer_of_0_in_a_generic_type_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.PartialTypeInitializerUpdate, nameof(FeaturesResources.Modifying_the_initializer_of_0_in_a_partial_type_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, nameof(FeaturesResources.Adding_a_constructor_to_a_type_with_a_field_or_property_initializer_that_contains_an_anonymous_function_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.RenamingCapturedVariable, nameof(FeaturesResources.Renaming_a_captured_variable_from_0_to_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.StackAllocUpdate, nameof(FeaturesResources.Modifying_0_which_contains_the_stackalloc_operator_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ExperimentalFeaturesEnabled, nameof(FeaturesResources.Modifying_source_with_experimental_language_features_enabled_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.AwaitStatementUpdate, nameof(FeaturesResources.Updating_a_complex_statement_containing_an_await_expression_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingConstructorVisibility, nameof(FeaturesResources.Changing_visibility_of_a_constructor_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.CapturingVariable, nameof(FeaturesResources.Capturing_variable_0_that_hasn_t_been_captured_before_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.NotCapturingVariable, nameof(FeaturesResources.Ceasing_to_capture_variable_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeletingCapturedVariable, nameof(FeaturesResources.Deleting_captured_variable_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingCapturedVariableType, nameof(FeaturesResources.Changing_the_type_of_a_captured_variable_0_previously_of_type_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingCapturedVariableScope, nameof(FeaturesResources.Changing_the_declaration_scope_of_a_captured_variable_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingLambdaParameters, nameof(FeaturesResources.Changing_the_parameters_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingLambdaReturnType, nameof(FeaturesResources.Changing_the_return_type_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ChangingQueryLambdaType, nameof(FeaturesResources.Changing_the_type_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.AccessingCapturedVariableInLambda, nameof(FeaturesResources.Accessing_captured_variable_0_that_hasn_t_been_accessed_before_in_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.NotAccessingCapturedVariableInLambda, nameof(FeaturesResources.Ceasing_to_access_captured_variable_0_in_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertLambdaWithMultiScopeCapture, nameof(FeaturesResources.Adding_0_that_accesses_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.DeleteLambdaWithMultiScopeCapture, nameof(FeaturesResources.Removing_0_that_accessed_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ActiveStatementLambdaRemoved, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_will_prevent_the_debug_session_from_continuing));
            // TODO: change the error message to better explain what's going on
            addRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementDelete, nameof(FeaturesResources.An_active_statement_has_been_removed_from_its_original_method_You_must_revert_your_changes_to_continue_or_restart_the_debugging_session));
            addRudeEdit(RudeEditKind.InsertFile, nameof(FeaturesResources.Adding_a_new_file_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, nameof(FeaturesResources.Updating_async_or_iterator_modifier_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdatingStateMachineMethodMissingAttribute, nameof(FeaturesResources.Attribute_0_is_missing_Updating_an_async_method_or_an_iterator_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.SwitchBetweenLambdaAndLocalFunction, nameof(FeaturesResources.Switching_between_lambda_and_local_function_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.RefStruct, nameof(FeaturesResources.Using_ref_structs_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ReadOnlyStruct, nameof(FeaturesResources.Using_readonly_structs_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ReadOnlyReferences, nameof(FeaturesResources.Using_readonly_references_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, nameof(FeaturesResources.Adding_method_with_explicit_interface_specifier_will_prevernt_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InternalError, nameof(FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_due_to_internal_error));
            addRudeEdit(RudeEditKind.ComplexQueryExpression, nameof(FeaturesResources.Modifying_0_which_contains_an_Aggregate_Group_By_or_Join_query_clauses_will_prevent_the_debug_session_from_continuing));

            // VB specific
            addRudeEdit(RudeEditKind.HandlesClauseUpdate, nameof(FeaturesResources.Updating_the_Handles_clause_of_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ImplementsClauseUpdate, nameof(FeaturesResources.Updating_the_Implements_clause_of_a_0_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.ConstraintKindUpdate, nameof(FeaturesResources.Changing_the_constraint_from_0_to_1_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.InsertHandlesClause, nameof(FeaturesResources.Adding_0_with_the_Handles_clause_will_prevent_the_debug_session_from_continuing));
            addRudeEdit(RudeEditKind.UpdateStaticLocal, nameof(FeaturesResources.Modifying_0_which_contains_a_static_variable_will_prevent_the_debug_session_from_continuing));

            //
            // other Roslyn reported diagnostics:
            //

            s_diagnosticBaseIndex = builder.Count;

            addGeneralDiagnostic(EditAndContinueErrorCode.ErrorReadingFile, nameof(FeaturesResources.ErrorReadingFile));
            addGeneralDiagnostic(EditAndContinueErrorCode.CantApplyChangesUnexpectedError, nameof(FeaturesResources.CantApplyChangesUnexpectedError));
            addGeneralDiagnostic(EditAndContinueErrorCode.ChangesDisallowedWhileStoppedAtException, nameof(FeaturesResources.ChangesDisallowedWhileStoppedAtException));
            addGeneralDiagnostic(EditAndContinueErrorCode.ChangesNotAppliedWhileRunning, nameof(FeaturesResources.ChangesNotAppliedWhileRunning), DiagnosticSeverity.Warning);

            s_descriptors = builder.ToImmutable();
        }

        internal static ImmutableArray<DiagnosticDescriptor> GetDescriptors()
            => s_descriptors.WhereAsArray(d => d != null);

        internal static DiagnosticDescriptor GetDescriptor(RudeEditKind kind)
            => s_descriptors[GetDescriptorIndex(kind)];

        internal static DiagnosticDescriptor GetDescriptor(EditAndContinueErrorCode errorCode)
            => s_descriptors[GetDescriptorIndex(errorCode)];

        internal static DiagnosticDescriptor GetModuleDiagnosticDescriptor(int errorCode)
        {
            lock (s_moduleDiagnosticDescriptorsGuard)
            {
                if (s_lazyModuleDiagnosticDescriptors == null)
                {
                    s_lazyModuleDiagnosticDescriptors = new Dictionary<int, DiagnosticDescriptor>();
                }

                if (!s_lazyModuleDiagnosticDescriptors.TryGetValue(errorCode, out var descriptor))
                {
                    s_lazyModuleDiagnosticDescriptors.Add(errorCode, descriptor = new DiagnosticDescriptor(
                        $"ENC{ModuleDiagnosticBaseId + errorCode:D4}",
                        s_encLocString,
                        s_encDisallowedByProjectLocString,
                        DiagnosticCategory.EditAndContinue,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        customTags: DiagnosticCustomTags.EditAndContinue));
                }

                return descriptor;
            }
        }

        private static int GetDescriptorIndex(RudeEditKind kind)
            => (int)kind;

        private static int GetDescriptorIndex(EditAndContinueErrorCode errorCode)
            => s_diagnosticBaseIndex + (int)errorCode;
    }
}
