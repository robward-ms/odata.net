//---------------------------------------------------------------------
// <copyright file="ValidationKinds.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData
{
    using System;

    /// <summary>
    /// Validation kinds used in ODataMessageReaderSettings and ODataMessageWriterSettings.
    /// </summary>
    [Flags]
    public enum ValidationKinds
    {
        /// <summary>
        /// No validations.
        /// </summary>
        None = 0,

        /// <summary>
        /// Disallow duplicate properties in ODataResource (i.e., properties with the same name).
        /// If no duplication can be guaranteed, this flag can be turned off for better performance.
        /// </summary>
        ThrowOnDuplicatePropertyNames = 1,

        /// <summary>
        /// Do not support for undeclared property for non open type.
        /// </summary>
        ThrowOnUndeclaredPropertyForNonOpenType = 2,

        /// <summary>
        /// Validates that the type in input must exactly match the model.
        /// If the input can be guaranteed to be valid, this flag can be turned off for better performance.
        /// </summary>
        ThrowIfTypeConflictsWithMetadata = 4,

        /// <summary>
        /// Validates that top level properties cannot be null. If a top level property is null,
        /// the response should be 204 (NoContent) per the spec: 
        /// 11.2.3 Requesting Individual Properties
        /// ...
        /// If the property is single-valued and has the null value, the service responds with 204 No Content.
        /// ...
        /// </summary>
        ThrowOnTopLevelNullProperty = 8,

        /// <summary>
        /// Enable all validations.
        /// </summary>
        All = ~0
    }
}
