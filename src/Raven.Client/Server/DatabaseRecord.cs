﻿using System;
using System.Collections.Generic;
using Raven.Client.Documents.Exceptions.Indexes;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Replication;
using Raven.Client.Documents.Replication.Messages;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Documents.Transformers;
using Raven.Client.Server.Expiration;
using Raven.Client.Server.PeriodicExport;
using Raven.Client.Server.Versioning;


namespace Raven.Client.Documents
{
    // The DatabaseRecord resides in EVERY server/node inside the cluster regardless if the db is actually within the node 
    public class DatabaseRecord
    {
        public DatabaseRecord()
        {
        }

        public DatabaseRecord(string databaseName)
        {
            DatabaseName = databaseName;
        }

        public string DatabaseName;

        public bool Disabled;

        public bool Encrypted;

        public Dictionary<string, DeletionInProgressStatus> DeletionInProgress;

        public string DataDirectory;
        
        public DatabaseTopology Topology;

        public ConflictSolver ConflictSolverConfig = new ConflictSolver();

        public Dictionary<string, IndexDefinition> Indexes;

        public Dictionary<string, AutoIndexDefinition> AutoIndexes;

        //todo: see how we can protect this
        public Dictionary<string, TransformerDefinition> Transformers;

        public Dictionary<string, string> Settings;

        public Dictionary<string, string> SecuredSettings { get; set; }

        public VersioningConfiguration Versioning { get; set; }

        public ExpirationConfiguration Expiration { get; set; }

        public PeriodicBackupConfiguration PeriodicBackup { get; set; }

        public Dictionary<string, SubscriptionRaftState> Subscriptions;

        public void AddIndex(IndexDefinition definition)
        {
            if (Transformers != null && Transformers.ContainsKey(definition.Name))
            {
                throw new IndexOrTransformerAlreadyExistException($"Tried to create an index with a name of {definition.Name}, but a transformer under the same name exists");
            }

            var lockMode = IndexLockMode.Unlock;

            IndexDefinition existingDefinition;
            if (Indexes.TryGetValue(definition.Name, out existingDefinition))
            {
                if (existingDefinition.LockMode != null)
                    lockMode = existingDefinition.LockMode.Value;

                var result = existingDefinition.Compare(definition);
                if (result != IndexDefinitionCompareDifferences.All)
                {
                    result &= ~IndexDefinitionCompareDifferences.Etag;

                    if (result == IndexDefinitionCompareDifferences.LockMode &&
                        definition.LockMode == null)
                        return;

                    if (result == IndexDefinitionCompareDifferences.None)
                        return;
                }
            }

            if (lockMode == IndexLockMode.LockedIgnore)
                return;

            if (lockMode == IndexLockMode.LockedError)
            {
                throw new IndexOrTransformerAlreadyExistException($"Cannot edit existing index {definition.Name} with lock mode {lockMode}");
            }

            Indexes[definition.Name] = definition;
        }

        public void AddIndex(AutoIndexDefinition definition)
        {
            if (Transformers != null && Transformers.ContainsKey(definition.Name))
            {
                throw new IndexOrTransformerAlreadyExistException($"Tried to create an index with a name of {definition.Name}, but a transformer under the same name exists");
            }

            var lockMode = IndexLockMode.Unlock;

            AutoIndexDefinition existingDefinition;
            if (AutoIndexes.TryGetValue(definition.Name, out existingDefinition))
            {
                if (existingDefinition.LockMode != null)
                    lockMode = existingDefinition.LockMode.Value;

                var result = existingDefinition.Compare(definition);
                result &= ~IndexDefinitionCompareDifferences.Etag;

                if (result == IndexDefinitionCompareDifferences.None)
                    return;

                result &= ~IndexDefinitionCompareDifferences.LockMode;
                result &= ~IndexDefinitionCompareDifferences.Priority;

                if (result != IndexDefinitionCompareDifferences.None)
                    throw new NotSupportedException($"Can not update auto-index: {definition.Name}");
            }

            AutoIndexes[definition.Name] = definition;
        }

        public void AddTransformer(TransformerDefinition definition)
        {
            if (Indexes != null && Indexes.ContainsKey(definition.Name))
            {
                throw new IndexOrTransformerAlreadyExistException($"Tried to create a transformer with a name of {definition.Name}, but an index under the same name exists");
            }

            TransformerDefinition existingTransformer;
            var lockMode = TransformerLockMode.Unlock;
            if (Transformers.TryGetValue(definition.Name, out existingTransformer))
            {
                lockMode = existingTransformer.LockMode;

                var result = existingTransformer.Compare(definition);
                result &= ~TransformerDefinitionCompareDifferences.Etag;

                if (result == TransformerDefinitionCompareDifferences.None)
                    return;
            }

            if (lockMode == TransformerLockMode.LockedIgnore)
                return;

            if (lockMode == TransformerLockMode.LockedError)
                throw new IndexOrTransformerAlreadyExistException($"Cannot edit existing transformer {definition.Name} with lock mode {lockMode}");

            Transformers[definition.Name] = definition;
        }

        public void DeleteIndex(string name)
        {
            Indexes?.Remove(name);
            AutoIndexes?.Remove(name);
        }

        public void DeleteTransformer(string name)
        {
            Transformers?.Remove(name);
        }

        public void CreateSubscription(SubscriptionCriteria criteria, long etag, ChangeVectorEntry[] initialChangeVector = null)
        {
            Subscriptions.Add(etag.ToString(), new SubscriptionRaftState()
            {
                Etag = etag,
                ChangeVector = initialChangeVector,
                Criteria = criteria
            });
        }

        public void UpdateSusbscriptionChangeVector(long etag, ChangeVectorEntry[] changeVectorUpdate, string nodeTag)
        {
            var subscriptionRaftState = Subscriptions[etag.ToString()];
            if (this.Topology.IsItMyTask(subscriptionRaftState, nodeTag) == false)
                throw new InvalidOperationException($"Can't update subscription with id {etag} by node {nodeTag}, because it's not it's task to update this subscription");

            // todo: implement change vector comparison here, need to move some extention methods from server to client first
            
            subscriptionRaftState.ChangeVector = changeVectorUpdate;
        }

        public void DeleteSubscription(long etag)
        {
            Subscriptions.Remove(etag.ToString());
        }

        
    }

    public enum DeletionInProgressStatus
    {
        No,
        SoftDelete,
        HardDelete
    }
}
