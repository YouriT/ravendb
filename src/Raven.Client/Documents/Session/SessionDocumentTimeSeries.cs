﻿//-----------------------------------------------------------------------
// <copyright file="SessionDocumentTimeSeries.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Raven.Client.Documents.Session.TimeSeries;
using Raven.Client.Util;

namespace Raven.Client.Documents.Session
{
    public class SessionDocumentTimeSeries : ISessionDocumentTimeSeries
    {
        private readonly AsyncSessionDocumentTimeSeries _asyncSessionTimeSeries;

        public SessionDocumentTimeSeries(InMemoryDocumentSessionOperations session, string documentId)
        {
            _asyncSessionTimeSeries = new AsyncSessionDocumentTimeSeries(session, documentId);
        }

        public SessionDocumentTimeSeries(InMemoryDocumentSessionOperations session, object entity)
        {
            _asyncSessionTimeSeries = new AsyncSessionDocumentTimeSeries(session, entity);
        }

        public void Append(string timeseries, DateTime timestamp, string tag, IEnumerable<double> values)
        {
            _asyncSessionTimeSeries.Append(timeseries, timestamp, tag, values);
        }

        public IEnumerable<TimeSeriesEntry> Get(string timeseries, DateTime from, DateTime to, int start = 0, int pageSize = 0)
        {
            return AsyncHelpers.RunSync(() => _asyncSessionTimeSeries.GetAsync(timeseries, from, to, start, pageSize));
        }

        public void Remove(string timeseries, DateTime from, DateTime to)
        {
            _asyncSessionTimeSeries.Remove(timeseries, from, to);
        }

        public void Remove(string timeseries, DateTime at)
        {
            _asyncSessionTimeSeries.Remove(timeseries, at);
        }
    }
}