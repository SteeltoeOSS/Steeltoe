// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Transaction;

public abstract class AbstractPlatformTransactionManager : IPlatformTransactionManager
{
    public const int SynchronizationAlways = 0;
    public const int SynchronizationOnActualTransaction = 1;
    public const int SynchronizationNever = 2;

    protected readonly ILogger Logger;
    private int _defaultTimeout;

    protected AbstractPlatformTransactionManager(ILogger logger = null)
    {
        this.Logger = logger;
    }

    public virtual int TransactionSynchronization { get; set; }

    public virtual int DefaultTimeout
    {
        get
        {
            return _defaultTimeout;
        }

        set
        {
            if (value < AbstractTransactionDefinition.TimeoutDefault)
            {
                throw new ArgumentException(nameof(DefaultTimeout));
            }

            _defaultTimeout = value;
        }
    }

    public virtual bool NestedTransactionAllowed { get; set; }

    public virtual bool ValidateExistingTransaction { get; set; }

    public virtual bool GlobalRollbackOnParticipationFailure { get; set; }

    public virtual bool FailEarlyOnGlobalRollbackOnly { get; set; }

    public virtual bool RollbackOnCommitFailure { get; set; }

    public virtual ITransactionStatus GetTransaction(ITransactionDefinition definition)
    {
        // Use defaults if no transaction definition given.
        var def = definition ?? AbstractTransactionDefinition.WithDefaults;

        var transaction = DoGetTransaction();

        if (IsExistingTransaction(transaction))
        {
            // Existing transaction found -> check propagation behavior to find out how to behave.
            return HandleExistingTransaction(def, transaction);
        }

        // Check definition settings for new transaction.
        if (def.Timeout < AbstractTransactionDefinition.TimeoutDefault)
        {
            throw new InvalidTimeoutException("Invalid transaction timeout", def.Timeout);
        }

        // No existing transaction found -> check propagation behavior to find out how to proceed.
        if (def.PropagationBehavior == AbstractTransactionDefinition.PropagationMandatory)
        {
            throw new IllegalTransactionStateException(
                "No existing transaction found for transaction marked with propagation 'mandatory'");
        }
        else if (def.PropagationBehavior == AbstractTransactionDefinition.PropagationRequired ||
                 def.PropagationBehavior == AbstractTransactionDefinition.PropagationRequiresNew ||
                 def.PropagationBehavior == AbstractTransactionDefinition.PropagationNested)
        {
            var suspendedResources = Suspend(null);
            Logger?.LogDebug("Creating new transaction with name [{name}] with {def}", def.Name, def);
            try
            {
                var newSynchronization = TransactionSynchronization != SynchronizationNever;
                var status = NewTransactionStatus(def, transaction, true, newSynchronization, suspendedResources);
                DoBegin(transaction, def);
                PrepareSynchronization(status, def);
                return status;
            }
            catch (Exception)
            {
                Resume(null, suspendedResources);
                throw;
            }
        }
        else
        {
            // Create "empty" transaction: no actual transaction, but potentially synchronization.
            if (def.IsolationLevel != AbstractTransactionDefinition.IsolationDefault)
            {
                Logger?.LogWarning("Custom isolation level specified but no actual transaction initiated; " +
                                    "isolation level will effectively be ignored: " + def);
            }

            var newSynchronization = TransactionSynchronization == SynchronizationAlways;
            return PrepareTransactionStatus(def, null, true, newSynchronization, null);
        }
    }

    public virtual void Commit(ITransactionStatus status)
    {
        if (status.IsCompleted)
        {
            throw new IllegalTransactionStateException(
                "Transaction is already completed - do not call commit or rollback more than once per transaction");
        }

        var defStatus = (DefaultTransactionStatus)status;
        if (defStatus.IsLocalRollbackOnly)
        {
            Logger?.LogDebug("Transactional code has requested rollback");
            ProcessRollback(defStatus, false);
            return;
        }

        if (!ShouldCommitOnGlobalRollbackOnly && defStatus.IsGlobalRollbackOnly)
        {
            Logger?.LogDebug("Global transaction is marked as rollback-only but transactional code requested commit");
            ProcessRollback(defStatus, true);
            return;
        }

        ProcessCommit(defStatus);
    }

    public virtual void Rollback(ITransactionStatus status)
    {
        if (status.IsCompleted)
        {
            throw new IllegalTransactionStateException("Transaction is already completed - do not call commit or rollback more than once per transaction");
        }

        var defStatus = (DefaultTransactionStatus)status;
        ProcessRollback(defStatus, false);
    }

    protected virtual DefaultTransactionStatus PrepareTransactionStatus(ITransactionDefinition definition, object transaction, bool newTransaction, bool newSynchronization, object suspendedResources)
    {
        var status = NewTransactionStatus(definition, transaction, newTransaction, newSynchronization, suspendedResources);
        PrepareSynchronization(status, definition);
        return status;
    }

    protected virtual DefaultTransactionStatus NewTransactionStatus(ITransactionDefinition definition, object transaction, bool newTransaction, bool newSynchronization, object suspendedResources)
    {
        var actualNewSynchronization = newSynchronization && !TransactionSynchronizationManager.IsSynchronizationActive();
        return new DefaultTransactionStatus(transaction, newTransaction, actualNewSynchronization, definition.IsReadOnly, suspendedResources, Logger);
    }

    protected virtual void PrepareSynchronization(DefaultTransactionStatus status, ITransactionDefinition definition)
    {
        if (status.IsNewSynchronization)
        {
            TransactionSynchronizationManager.SetActualTransactionActive(status.HasTransaction);
            TransactionSynchronizationManager.SetCurrentTransactionIsolationLevel(definition.IsolationLevel != AbstractTransactionDefinition.IsolationDefault ? definition.IsolationLevel : null);
            TransactionSynchronizationManager.SetCurrentTransactionReadOnly(definition.IsReadOnly);
            TransactionSynchronizationManager.SetCurrentTransactionName(definition.Name);
            TransactionSynchronizationManager.InitSynchronization();
        }
    }

    protected virtual int DetermineTimeout(ITransactionDefinition definition)
    {
        if (definition.Timeout != AbstractTransactionDefinition.TimeoutDefault)
        {
            return definition.Timeout;
        }

        return DefaultTimeout;
    }

    protected virtual SuspendedResourcesHolder Suspend(object transaction)
    {
        if (TransactionSynchronizationManager.IsSynchronizationActive())
        {
            var suspendedSynchronizations = DoSuspendSynchronization();
            try
            {
                object suspendedResources = null;
                if (transaction != null)
                {
                    suspendedResources = DoSuspend(transaction);
                }

                var name = TransactionSynchronizationManager.GetCurrentTransactionName();
                TransactionSynchronizationManager.SetCurrentTransactionName(null);
                var readOnly = TransactionSynchronizationManager.IsCurrentTransactionReadOnly();
                TransactionSynchronizationManager.SetCurrentTransactionReadOnly(false);
                var isolationLevel = TransactionSynchronizationManager.GetCurrentTransactionIsolationLevel();
                TransactionSynchronizationManager.SetCurrentTransactionIsolationLevel(null);
                var wasActive = TransactionSynchronizationManager.IsActualTransactionActive();
                TransactionSynchronizationManager.SetActualTransactionActive(false);
                return new SuspendedResourcesHolder(suspendedResources, suspendedSynchronizations, name, readOnly, isolationLevel, wasActive);
            }
            catch (Exception)
            {
                // doSuspend failed - original transaction is still active...
                DoResumeSynchronization(suspendedSynchronizations);
                throw;
            }
        }
        else if (transaction != null)
        {
            // Transaction active but no synchronization active.
            var suspendedResources = DoSuspend(transaction);
            return new SuspendedResourcesHolder(suspendedResources);
        }
        else
        {
            // Neither transaction nor synchronization active.
            return null;
        }
    }

    protected virtual void Resume(object transaction, SuspendedResourcesHolder resourcesHolder)
    {
        if (resourcesHolder != null)
        {
            var suspendedResources = resourcesHolder.SuspendedResources;
            if (suspendedResources != null)
            {
                DoResume(transaction, suspendedResources);
            }

            var suspendedSynchronizations = resourcesHolder.SuspendedSynchronizations;
            if (suspendedSynchronizations != null)
            {
                TransactionSynchronizationManager.SetActualTransactionActive(resourcesHolder.WasActive);
                TransactionSynchronizationManager.SetCurrentTransactionIsolationLevel(resourcesHolder.IsolationLevel);
                TransactionSynchronizationManager.SetCurrentTransactionReadOnly(resourcesHolder.ReadOnly);
                TransactionSynchronizationManager.SetCurrentTransactionName(resourcesHolder.Name);
                DoResumeSynchronization(suspendedSynchronizations);
            }
        }
    }

    protected virtual void TriggerBeforeCommit(DefaultTransactionStatus status)
    {
        if (status.IsNewSynchronization)
        {
            Logger?.LogTrace("Triggering beforeCommit synchronization");
            TransactionSynchronizationUtils.TriggerBeforeCommit(status.IsReadOnly);
        }
    }

    protected virtual void TriggerBeforeCompletion(DefaultTransactionStatus status)
    {
        if (status.IsNewSynchronization)
        {
            Logger?.LogTrace("Triggering beforeCompletion synchronization");
            TransactionSynchronizationUtils.TriggerBeforeCompletion(Logger);
        }
    }

    protected virtual void InvokeAfterCompletion(List<ITransactionSynchronization> synchronizations, int completionStatus)
    {
        TransactionSynchronizationUtils.InvokeAfterCompletion(synchronizations, completionStatus);
    }

    protected abstract object DoGetTransaction();

    protected virtual bool IsExistingTransaction(object transaction)
    {
        return false;
    }

    protected abstract void DoBegin(object transaction, ITransactionDefinition definition);

    protected virtual object DoSuspend(object transaction)
    {
        throw new TransactionSuspensionNotSupportedException($"Transaction manager [{GetType().Name}] does not support transaction suspension");
    }

    protected virtual void DoResume(object transaction, object suspendedResources)
    {
        throw new TransactionSuspensionNotSupportedException($"Transaction manager [{GetType().Name}] does not support transaction suspension");
    }

    protected virtual void PrepareForCommit(DefaultTransactionStatus status)
    {
    }

    protected virtual void DoSetRollbackOnly(DefaultTransactionStatus status)
    {
        throw new IllegalTransactionStateException(
            "Participating in existing transactions is not supported - when 'isExistingTransaction' " +
            "returns true, appropriate 'doSetRollbackOnly' behavior must be provided");
    }

    protected virtual void RegisterAfterCompletionWithExistingTransaction(object transaction, List<ITransactionSynchronization> synchronizations)
    {
        Logger?.LogDebug("Cannot register Spring after-completion synchronization with existing transaction - " +
                          "processing Spring after-completion callbacks immediately, with outcome status 'unknown'");
        InvokeAfterCompletion(synchronizations, AbstractTransactionSynchronization.StatusUnknown);
    }

    protected virtual void DoCleanupAfterCompletion(object transaction)
    {
    }

    protected abstract void DoCommit(DefaultTransactionStatus status);

    protected abstract void DoRollback(DefaultTransactionStatus status);

    protected virtual bool ShouldCommitOnGlobalRollbackOnly => false;

    protected virtual bool UseSavepointForNestedTransaction => true;

    private void CleanupAfterCompletion(DefaultTransactionStatus status)
    {
        status.IsCompleted = true;
        if (status.IsNewSynchronization)
        {
            TransactionSynchronizationManager.Clear();
        }

        if (status.IsNewTransaction)
        {
            DoCleanupAfterCompletion(status.Transaction);
        }

        if (status.SuspendedResources != null)
        {
            Logger?.LogDebug("Resuming suspended transaction after completion of inner transaction");
            var transaction = status.HasTransaction ? status.Transaction : null;
            Resume(transaction, (SuspendedResourcesHolder)status.SuspendedResources);
        }
    }

    private void TriggerAfterCommit(DefaultTransactionStatus status)
    {
        if (status.IsNewSynchronization)
        {
            Logger?.LogTrace("Triggering afterCommit synchronization");
            TransactionSynchronizationUtils.TriggerAfterCommit();
        }
    }

    private void TriggerAfterCompletion(DefaultTransactionStatus status, int completionStatus)
    {
        if (status.IsNewSynchronization)
        {
            var synchronizations = TransactionSynchronizationManager.GetSynchronizations();
            TransactionSynchronizationManager.ClearSynchronization();
            if (!status.HasTransaction || status.IsNewTransaction)
            {
                Logger?.LogTrace("Triggering afterCompletion synchronization");

                // No transaction or new transaction for the current scope ->
                // invoke the afterCompletion callbacks immediately
                InvokeAfterCompletion(synchronizations, completionStatus);
            }
            else if (synchronizations.Count > 0)
            {
                // Existing transaction that we participate in, controlled outside
                // of the scope of this Spring transaction manager -> try to register
                // an afterCompletion callback with the existing (JTA) transaction.
                RegisterAfterCompletionWithExistingTransaction(status.Transaction, synchronizations);
            }
        }
    }

    private void DoRollbackOnCommitException(DefaultTransactionStatus status, Exception exception)
    {
        try
        {
            if (status.IsNewTransaction)
            {
                Logger?.LogDebug(exception, "Initiating transaction rollback after commit exception");
                DoRollback(status);
            }
            else if (status.HasTransaction && GlobalRollbackOnParticipationFailure)
            {
                Logger?.LogDebug(exception, "Marking existing transaction as rollback-only after commit exception");
                DoSetRollbackOnly(status);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Commit exception overridden by rollback exception");
            TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusUnknown);
            throw;
        }

        TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusRolledBack);
    }

    private void ProcessRollback(DefaultTransactionStatus status, bool unexpected)
    {
        try
        {
            var unexpectedRollback = unexpected;

            try
            {
                TriggerBeforeCompletion(status);

                if (status.HasSavepoint)
                {
                    Logger?.LogDebug("Rolling back transaction to savepoint");
                    status.RollbackToHeldSavepoint();
                }
                else if (status.IsNewTransaction)
                {
                    Logger?.LogDebug("Initiating transaction rollback");
                    DoRollback(status);
                }
                else
                {
                    // Participating in larger transaction
                    if (status.HasTransaction)
                    {
                        if (status.IsLocalRollbackOnly || GlobalRollbackOnParticipationFailure)
                        {
                            Logger?.LogDebug("Participating transaction failed - marking existing transaction as rollback-only");
                            DoSetRollbackOnly(status);
                        }
                        else
                        {
                            Logger?.LogDebug("Participating transaction failed - letting transaction originator decide on rollback");
                        }
                    }
                    else
                    {
                        Logger?.LogDebug("Should roll back transaction but cannot - no transaction available");
                    }

                    // Unexpected rollback only matters here if we're asked to fail early
                    if (!FailEarlyOnGlobalRollbackOnly)
                    {
                        unexpectedRollback = false;
                    }
                }
            }
            catch (Exception)
            {
                TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusUnknown);
                throw;
            }

            TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusRolledBack);

            // Raise UnexpectedRollbackException if we had a global rollback-only marker
            if (unexpectedRollback)
            {
                throw new UnexpectedRollbackException("Transaction rolled back because it has been marked as rollback-only");
            }
        }
        finally
        {
            CleanupAfterCompletion(status);
        }
    }

    private void ProcessCommit(DefaultTransactionStatus status)
    {
        try
        {
            var beforeCompletionInvoked = false;

            try
            {
                var unexpectedRollback = false;
                PrepareForCommit(status);
                TriggerBeforeCommit(status);
                TriggerBeforeCompletion(status);
                beforeCompletionInvoked = true;

                if (status.HasSavepoint)
                {
                    Logger?.LogDebug("Releasing transaction savepoint");
                    unexpectedRollback = status.IsGlobalRollbackOnly;
                    status.ReleaseHeldSavepoint();
                }
                else if (status.IsNewTransaction)
                {
                    Logger?.LogDebug("Initiating transaction commit");
                    unexpectedRollback = status.IsGlobalRollbackOnly;
                    DoCommit(status);
                }
                else if (FailEarlyOnGlobalRollbackOnly)
                {
                    unexpectedRollback = status.IsGlobalRollbackOnly;
                }

                // Throw UnexpectedRollbackException if we have a global rollback-only
                // marker but still didn't get a corresponding exception from commit.
                if (unexpectedRollback)
                {
                    throw new UnexpectedRollbackException("Transaction silently rolled back because it has been marked as rollback-only");
                }
            }
            catch (UnexpectedRollbackException)
            {
                // can only be caused by doCommit
                TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusRolledBack);
                throw;
            }
            catch (TransactionException ex)
            {
                // can only be caused by doCommit
                if (RollbackOnCommitFailure)
                {
                    DoRollbackOnCommitException(status, ex);
                }
                else
                {
                    TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusUnknown);
                }

                throw;
            }
            catch (Exception ex)
            {
                if (!beforeCompletionInvoked)
                {
                    TriggerBeforeCompletion(status);
                }

                DoRollbackOnCommitException(status, ex);
                throw;
            }

            // Trigger afterCommit callbacks, with an exception thrown there
            // propagated to callers but the transaction still considered as committed.
            try
            {
                TriggerAfterCommit(status);
            }
            finally
            {
                TriggerAfterCompletion(status, AbstractTransactionSynchronization.StatusCommitted);
            }
        }
        finally
        {
            CleanupAfterCompletion(status);
        }
    }

    private void DoResumeSynchronization(List<ITransactionSynchronization> suspendedSynchronizations)
    {
        TransactionSynchronizationManager.InitSynchronization();
        foreach (var synchronization in suspendedSynchronizations)
        {
            synchronization.Resume();
            TransactionSynchronizationManager.RegisterSynchronization(synchronization);
        }
    }

    private List<ITransactionSynchronization> DoSuspendSynchronization()
    {
        var suspendedSynchronizations = TransactionSynchronizationManager.GetSynchronizations();
        foreach (var synchronization in suspendedSynchronizations)
        {
            synchronization.Suspend();
        }

        TransactionSynchronizationManager.ClearSynchronization();
        return suspendedSynchronizations;
    }

    private void ResumeAfterBeginException(object transaction, SuspendedResourcesHolder suspendedResources, Exception beginEx)
    {
        try
        {
            Resume(transaction, suspendedResources);
        }
        catch (Exception)
        {
            var exMessage = "Inner transaction begin exception overridden by outer transaction resume exception";
            Logger?.LogError(beginEx, exMessage);
            throw;
        }
    }

    private ITransactionStatus HandleExistingTransaction(ITransactionDefinition definition, object transaction)
    {
        if (definition.PropagationBehavior == AbstractTransactionDefinition.PropagationNever)
        {
            throw new IllegalTransactionStateException(
                "Existing transaction found for transaction marked with propagation 'never'");
        }

        if (definition.PropagationBehavior == AbstractTransactionDefinition.PropagationNotSupported)
        {
            Logger?.LogDebug("Suspending current transaction");
            var suspendedResources = Suspend(transaction);
            return PrepareTransactionStatus(definition, null, false, TransactionSynchronization == SynchronizationAlways, suspendedResources);
        }

        if (definition.PropagationBehavior == AbstractTransactionDefinition.PropagationRequiresNew)
        {
            Logger?.LogDebug("Suspending current transaction, creating new transaction with name [{name}]", definition.Name);
            var suspendedResources = Suspend(transaction);
            try
            {
                var status = NewTransactionStatus(definition, transaction, true, TransactionSynchronization != SynchronizationNever, suspendedResources);
                DoBegin(transaction, definition);
                PrepareSynchronization(status, definition);
                return status;
            }
            catch (Exception ex)
            {
                ResumeAfterBeginException(transaction, suspendedResources, ex);
                throw;
            }
        }

        if (definition.PropagationBehavior == AbstractTransactionDefinition.PropagationNested)
        {
            if (!NestedTransactionAllowed)
            {
                throw new NestedTransactionNotSupportedException(
                    "Transaction manager does not allow nested transactions by default - " +
                    "specify 'nestedTransactionAllowed' property with value 'true'");
            }

            Logger?.LogDebug("Creating nested transaction with name [{name}]", definition.Name);
            if (UseSavepointForNestedTransaction)
            {
                // Create savepoint within existing Spring-managed transaction,
                // through the SavepointManager API implemented by TransactionStatus.
                // Usually uses JDBC 3.0 savepoints. Never activates Spring synchronization.
                var status = PrepareTransactionStatus(definition, transaction, false, false, null);
                status.CreateAndHoldSavepoint();
                return status;
            }
            else
            {
                // Nested transaction through nested begin and commit/rollback calls.
                // Usually only for JTA: Spring synchronization might get activated here
                // in case of a pre-existing JTA transaction.
                var status = NewTransactionStatus(definition, transaction, true, TransactionSynchronization != SynchronizationNever, null);
                DoBegin(transaction, definition);
                PrepareSynchronization(status, definition);
                return status;
            }
        }

        // Assumably PROPAGATION_SUPPORTS or PROPAGATION_REQUIRED.
        Logger?.LogDebug("Participating in existing transaction");
        if (ValidateExistingTransaction)
        {
            if (definition.IsolationLevel != AbstractTransactionDefinition.IsolationDefault)
            {
                var currentIsolationLevel = TransactionSynchronizationManager.GetCurrentTransactionIsolationLevel();
                if (currentIsolationLevel == null || currentIsolationLevel != definition.IsolationLevel)
                {
                    throw new IllegalTransactionStateException(
                        $"Participating transaction with definition [{definition}] specifies isolation level which is incompatible with existing transaction: ");
                }
            }

            if (!definition.IsReadOnly && TransactionSynchronizationManager.IsCurrentTransactionReadOnly())
            {
                throw new IllegalTransactionStateException(
                    $"Participating transaction with definition [{definition}] is not marked as read-only but existing transaction is");
            }
        }

        var newSynchronization = TransactionSynchronization != SynchronizationNever;
        return PrepareTransactionStatus(definition, transaction, false, newSynchronization, null);
    }

    protected class SuspendedResourcesHolder
    {
        public SuspendedResourcesHolder(object suspendedResources)
        {
            SuspendedResources = suspendedResources;
        }

        public SuspendedResourcesHolder(object suspendedResources, List<ITransactionSynchronization> suspendedSynchronizations, string name, bool readOnly, int? isolationLevel, bool wasActive)
        {
            SuspendedResources = suspendedResources;
            SuspendedSynchronizations = suspendedSynchronizations;
            Name = name;
            ReadOnly = readOnly;
            IsolationLevel = isolationLevel;
            WasActive = wasActive;
        }

        public object SuspendedResources { get; }

        public List<ITransactionSynchronization> SuspendedSynchronizations { get; }

        public string Name { get; }

        public bool ReadOnly { get; }

        public int? IsolationLevel { get; }

        public bool WasActive { get; }
    }
}
