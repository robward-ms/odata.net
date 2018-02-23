//---------------------------------------------------------------------
// <copyright file="ValidationKinds.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.OData
{
    /// <summary>
    /// Library compatibility levels.
    /// </summary>
    /// <remarks>
    /// So allow for comparisons, define the values as xxyyzz where:
    /// xx = Zero-padded major version number
    /// yy = Zero-padded minor version number
    /// zz = Zero-padded revision version number
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ODataLibraryCompatibility
    {
        /// <summary>
        /// Version 6.x
        /// </summary>
        Version6 = 060000,

        /// <summary>
        /// Version 7.4.1
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        Version7_4_1 = 070401,

        /// <summary>
        /// The latest version of the library.
        /// </summary>
        Latest = int.MaxValue
    }
}
