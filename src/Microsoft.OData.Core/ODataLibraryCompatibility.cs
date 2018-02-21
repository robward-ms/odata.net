//---------------------------------------------------------------------
// <copyright file="ValidationKinds.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData
{
    /// <summary>
    /// Library compatibility levels.
    /// </summary>
    public enum ODataLibraryCompatibility
    {
        /// <summary>
        /// Version 6.x
        /// </summary>
        Version6 = 0,

        /// <summary>
        /// The latest version of the library.
        /// </summary>
        Latest = int.MaxValue
    }
}
