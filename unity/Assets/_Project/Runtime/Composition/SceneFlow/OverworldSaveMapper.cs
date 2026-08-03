using System;
using System.Collections.Generic;
using TimeKey.Application.Overworld;
using TimeKey.Infrastructure.Persistence;

namespace TimeKey.Composition.SceneFlow
{
    public static class OverworldSaveMapper
    {
        public static OverworldSaveDocument ToDocument(OverworldPersistenceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var outcomes = new List<OverworldProcessedOutcomeDocument>(
                snapshot.ProcessedOutcomes.Count);
            foreach (var outcome in snapshot.ProcessedOutcomes)
            {
                var steps = new List<int>(outcome.Steps.Count);
                foreach (var step in outcome.Steps)
                {
                    steps.Add((int)step);
                }

                outcomes.Add(new OverworldProcessedOutcomeDocument(
                    outcome.OutcomeKind,
                    outcome.OutcomeCorrelationId,
                    outcome.Fingerprint,
                    outcome.RoomId,
                    steps,
                    (int)outcome.ChapterAdvanceKind,
                    outcome.CompletedChapter,
                    outcome.NextChapter));
            }

            var journal = new List<OverworldOperationJournalDocument>(
                snapshot.DomainJournal.Count);
            foreach (var entry in snapshot.DomainJournal)
            {
                journal.Add(new OverworldOperationJournalDocument(
                    entry.Sequence,
                    entry.Kind,
                    entry.ExpectedRevision,
                    entry.SourceNodeId,
                    entry.NodeId,
                    entry.Resolution,
                    entry.Failure,
                    entry.BeforeRevision,
                    entry.AfterRevision,
                    entry.ChapterAdvanced));
            }

            return new OverworldSaveDocument(
                OverworldSaveSchema.CurrentVersion,
                snapshot.RunStartKind,
                snapshot.RunId,
                snapshot.RunSeed,
                snapshot.SeedText,
                snapshot.Chapter,
                snapshot.FinalChapter,
                snapshot.MapFingerprint,
                snapshot.MapConfigVersion,
                snapshot.IntermediateLayerCount,
                snapshot.MinimumNodesPerLayer,
                snapshot.MaximumNodesPerLayer,
                snapshot.CurrentNodeId,
                snapshot.ActiveRoomId,
                snapshot.ActiveLaunchCorrelationId,
                snapshot.ChapterCompleted,
                snapshot.ChapterAdvanceCount,
                snapshot.DomainRevision,
                snapshot.OperationSequenceCursor,
                snapshot.PersistenceRevision,
                snapshot.CharacterId,
                snapshot.Era,
                snapshot.Phase,
                snapshot.Timecoins,
                snapshot.DeckStableIds,
                snapshot.VisitedNodeIds,
                snapshot.SettledNodeIds,
                outcomes,
                journal);
        }

        public static OverworldPersistenceSnapshot ToSnapshot(OverworldSaveDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var outcomes = new List<OverworldProcessedOutcomeSnapshot>(
                document.ProcessedOutcomes.Count);
            foreach (var outcome in document.ProcessedOutcomes)
            {
                var steps = new List<OverworldTransactionStepKind>(outcome.Steps.Count);
                foreach (var step in outcome.Steps)
                {
                    steps.Add((OverworldTransactionStepKind)step);
                }

                outcomes.Add(new OverworldProcessedOutcomeSnapshot(
                    outcome.OutcomeKind,
                    outcome.OutcomeCorrelationId,
                    outcome.Fingerprint,
                    outcome.RoomId,
                    steps,
                    (OverworldChapterAdvanceKind)outcome.ChapterAdvanceKind,
                    outcome.CompletedChapter,
                    outcome.NextChapter));
            }

            var journal = new List<OverworldOperationJournalPersistenceSnapshot>(
                document.DomainJournal.Count);
            foreach (var entry in document.DomainJournal)
            {
                journal.Add(new OverworldOperationJournalPersistenceSnapshot(
                    entry.Sequence,
                    entry.Kind,
                    entry.ExpectedRevision,
                    entry.SourceNodeId,
                    entry.NodeId,
                    entry.Resolution,
                    entry.Failure,
                    entry.BeforeRevision,
                    entry.AfterRevision,
                    entry.ChapterAdvanced));
            }

            return new OverworldPersistenceSnapshot(
                document.RunId,
                document.RunStartKind,
                document.RunSeed,
                document.SeedText,
                document.Chapter,
                document.FinalChapter,
                document.Era,
                document.Phase,
                document.Timecoins,
                document.CharacterId,
                document.DeckStableIds,
                document.IntermediateLayerCount,
                document.MinimumNodesPerLayer,
                document.MaximumNodesPerLayer,
                document.MapFingerprint,
                document.CurrentNodeId,
                document.ActiveRoomId,
                document.ActiveLaunchCorrelationId,
                document.ChapterCompleted,
                document.ChapterAdvanceCount,
                document.DomainRevision,
                document.OperationSequenceCursor,
                document.PersistenceRevision,
                document.VisitedNodeIds,
                document.SettledNodeIds,
                outcomes,
                journal);
        }
    }
}
