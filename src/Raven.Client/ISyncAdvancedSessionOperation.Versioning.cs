//-----------------------------------------------------------------------
// <copyright file="ISyncAdvancedSessionOperation.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace Raven.Client
{
    /// <summary>
    ///     Advanced synchronous session operations
    /// </summary>
    public partial interface ISyncAdvancedSessionOperation
    {
        /// <summary>
        /// Returns all previous document revisions for specified document (with paging).
        /// </summary>
        List<T> GetRevisionsFor<T>(string id, int start = 0, int pageSize = 25);
    }
}
