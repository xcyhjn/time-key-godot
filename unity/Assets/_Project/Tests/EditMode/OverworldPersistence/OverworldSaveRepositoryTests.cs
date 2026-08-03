using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Infrastructure.Persistence;
using UnityEngine;

namespace TimeKey.Tests.EditMode.OverworldPersistence
{
    public sealed class OverworldSaveRepositoryTests
    {
        private string _directory;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Tests",
                "EditMode",
                "OverworldPersistence",
                ".test-temp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _savePath = Path.Combine(_directory, "overworld-save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsTheCurrentDocument()
        {
            var repository = new OverworldSaveRepository(_savePath);
            var document = CreateDocument(persistenceRevision: 4);

            var write = repository.Save(document);
            var load = repository.Load();

            Assert.That(write.Succeeded, Is.True, write.Detail);
            Assert.That(load.Status, Is.EqualTo(OverworldSaveLoadStatus.Valid));
            Assert.That(load.Source, Is.EqualTo(OverworldSaveSource.Primary));
            AssertDocument(load.Document, document);
            Assert.That(File.Exists(repository.TemporaryPath), Is.False);
        }

        [Test]
        public void Serialization_NormalizesSetLikeCollectionsDeterministically()
        {
            var firstPath = Path.Combine(_directory, "first.json");
            var secondPath = Path.Combine(_directory, "second.json");
            var first = CreateDocument(
                visited: new[] { "node-z", "node-a", "node-m" },
                settled: new[] { "node-m", "node-a" },
                currentNodeId: "node-z");
            var second = CreateDocument(
                visited: new[] { "node-m", "node-z", "node-a" },
                settled: new[] { "node-a", "node-m" },
                currentNodeId: "node-z");

            Assert.That(new OverworldSaveRepository(firstPath).Save(first).Succeeded, Is.True);
            Assert.That(new OverworldSaveRepository(secondPath).Save(second).Succeeded, Is.True);

            Assert.That(
                File.ReadAllText(firstPath),
                Is.EqualTo(File.ReadAllText(secondPath)));
            CollectionAssert.AreEqual(
                new[] { "node-a", "node-m", "node-z" },
                first.VisitedNodeIds);
            CollectionAssert.AreEqual(
                new[] { "outcome-a", "outcome-z" },
                OutcomeIds(first.ProcessedOutcomes));
        }

        [Test]
        public void Document_DefensivelyCopiesEveryInputCollection()
        {
            var deck = new List<string> { "card-a", "card-b" };
            var visited = new List<string> { "node-a", "node-z" };
            var settled = new List<string> { "node-a" };
            var outcomes = new List<OverworldProcessedOutcomeDocument>
            {
                new OverworldProcessedOutcomeDocument("Combat", "outcome-a", "fingerprint-a")
            };
            var document = CreateDocument(
                deck,
                visited,
                settled,
                outcomes,
                currentNodeId: "node-z");

            deck[0] = "mutated";
            visited.Clear();
            settled.Clear();
            outcomes.Clear();

            CollectionAssert.AreEqual(new[] { "card-a", "card-b" }, document.DeckStableIds);
            CollectionAssert.AreEqual(new[] { "node-a", "node-z" }, document.VisitedNodeIds);
            CollectionAssert.AreEqual(new[] { "node-a" }, document.SettledNodeIds);
            Assert.That(document.ProcessedOutcomes.Count, Is.EqualTo(1));
            Assert.That(document.DeckStableIds, Is.Not.InstanceOf<List<string>>());
            Assert.That(document.ProcessedOutcomes,
                Is.Not.InstanceOf<List<OverworldProcessedOutcomeDocument>>());
        }

        [Test]
        public void Load_WhenPrimaryAndBackupAreMissing_ReturnsMissing()
        {
            var result = new OverworldSaveRepository(_savePath).Load();

            Assert.That(result.Status, Is.EqualTo(OverworldSaveLoadStatus.Missing));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Document, Is.Null);
        }

        [Test]
        public void Load_MalformedJson_ReturnsCorrupt()
        {
            File.WriteAllText(_savePath, "{not-json");

            var result = new OverworldSaveRepository(_savePath).Load();

            Assert.That(result.Status, Is.EqualTo(OverworldSaveLoadStatus.Corrupt));
            Assert.That(result.Document, Is.Null);
        }

        [Test]
        public void Load_FutureSchema_ReturnsUnsupportedFutureVersion()
        {
            File.WriteAllText(_savePath, "{\"schemaVersion\":99}");

            var result = new OverworldSaveRepository(_savePath).Load();

            Assert.That(
                result.Status,
                Is.EqualTo(OverworldSaveLoadStatus.UnsupportedFutureVersion));
            Assert.That(result.Document, Is.Null);
        }

        [Test]
        public void Load_ApprovedV0Fixture_ReturnsMigratedCurrentDocument()
        {
            File.WriteAllText(_savePath, LegacyV0Json);

            var result = new OverworldSaveRepository(_savePath).Load();

            Assert.That(result.Status, Is.EqualTo(OverworldSaveLoadStatus.Migrated));
            Assert.That(
                result.Document.SchemaVersion,
                Is.EqualTo(OverworldSaveSchema.CurrentVersion));
            Assert.That(
                result.Document.MapConfigVersion,
                Is.EqualTo(OverworldSaveSchema.V0DefaultMapConfigVersion));
            Assert.That(result.Document.HasActiveRoom, Is.False);
            Assert.That(result.Document.OperationSequenceCursor, Is.EqualTo(0));
            Assert.That(result.Document.PersistenceRevision, Is.EqualTo(0));
            Assert.That(result.Document.ProcessedOutcomes, Is.Empty);
        }

        [Test]
        public void Load_Schema1Document_ReturnsMigratedSchema2Document()
        {
            var repository = new OverworldSaveRepository(_savePath);
            var original = CreateDocument(persistenceRevision: 6, runId: "schema-1-run");
            Assert.That(repository.Save(original).Succeeded, Is.True);

            var currentJson = File.ReadAllText(_savePath);
            var schema1Json = currentJson
                .Replace("\"schemaVersion\": 2", "\"schemaVersion\": 1")
                .Replace(",\r\n  \"domainJournal\": []", string.Empty);
            Assert.That(schema1Json, Is.Not.EqualTo(currentJson));
            Assert.That(schema1Json, Does.Not.Contain("domainJournal"));
            File.WriteAllText(_savePath, schema1Json);

            var result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(OverworldSaveLoadStatus.Migrated));
            Assert.That(result.Document.SchemaVersion,
                Is.EqualTo(OverworldSaveSchema.CurrentVersion));
            Assert.That(result.Document.DomainJournal, Is.Empty);
            AssertDocument(result.Document, original);
        }

        [Test]
        public void Load_CorruptPrimary_RecoversThePreviousValidBackup()
        {
            var repository = new OverworldSaveRepository(_savePath);
            var first = CreateDocument(persistenceRevision: 1, runId: "run-first");
            var second = CreateDocument(persistenceRevision: 2, runId: "run-second");
            Assert.That(repository.Save(first).Succeeded, Is.True);
            Assert.That(repository.Save(second).Succeeded, Is.True);
            File.WriteAllText(repository.PrimaryPath, "{broken");

            var result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(OverworldSaveLoadStatus.Valid));
            Assert.That(result.RecoveredFromBackup, Is.True);
            Assert.That(result.Document.RunId, Is.EqualTo("run-first"));
        }

        [Test]
        public void Save_TempWriteFailure_LeavesPreviousSaveReadableAndNoTemp()
        {
            AssertWriteFailurePreservesPrior(
                OverworldSaveWriteStage.BeforeTempWrite,
                OverworldSaveWriteFailure.TempWrite,
                corruptTemporary: false);
        }

        [Test]
        public void Save_TempValidationFailure_LeavesPreviousSaveReadableAndNoTemp()
        {
            AssertWriteFailurePreservesPrior(
                OverworldSaveWriteStage.BeforeTempValidation,
                OverworldSaveWriteFailure.TempValidation,
                corruptTemporary: true);
        }

        [Test]
        public void Save_ReplaceFailure_LeavesPreviousSaveReadableAndNoTemp()
        {
            AssertWriteFailurePreservesPrior(
                OverworldSaveWriteStage.BeforeReplace,
                OverworldSaveWriteFailure.Replace,
                corruptTemporary: false);
        }

        [Test]
        public void Save_FuturePrimaryDoesNotOverwriteAValidBackup()
        {
            var repository = new OverworldSaveRepository(_savePath);
            var backupDocument = CreateDocument(
                persistenceRevision: 1,
                runId: "backup-run");
            Assert.That(repository.Save(backupDocument).Succeeded, Is.True);
            Assert.That(repository.Save(CreateDocument(
                persistenceRevision: 2,
                runId: "second-run")).Succeeded, Is.True);
            File.WriteAllText(repository.PrimaryPath, "{\"schemaVersion\":88}");

            var write = repository.Save(CreateDocument(
                persistenceRevision: 3,
                runId: "third-run"));
            var backup = new OverworldSaveRepository(repository.BackupPath).Load();

            Assert.That(write.Succeeded, Is.True, write.Detail);
            Assert.That(backup.Status, Is.EqualTo(OverworldSaveLoadStatus.Valid));
            Assert.That(backup.Document.RunId, Is.EqualTo("backup-run"));
        }

        private void AssertWriteFailurePreservesPrior(
            OverworldSaveWriteStage stage,
            OverworldSaveWriteFailure expectedFailure,
            bool corruptTemporary)
        {
            var initialRepository = new OverworldSaveRepository(_savePath);
            var previous = CreateDocument(
                persistenceRevision: 1,
                runId: "previous-run");
            Assert.That(initialRepository.Save(previous).Succeeded, Is.True);

            var failingRepository = new OverworldSaveRepository(
                _savePath,
                new StageFaultInjector(stage, corruptTemporary));
            var write = failingRepository.Save(CreateDocument(
                persistenceRevision: 2,
                runId: "partial-new-run"));
            var load = initialRepository.Load();

            Assert.That(write.Failure, Is.EqualTo(expectedFailure));
            Assert.That(load.Succeeded, Is.True, load.Detail);
            Assert.That(load.Document.RunId, Is.EqualTo("previous-run"));
            Assert.That(load.Document.PersistenceRevision, Is.EqualTo(1));
            Assert.That(File.Exists(failingRepository.TemporaryPath), Is.False);
        }

        private static OverworldSaveDocument CreateDocument(
            IReadOnlyList<string> deck = null,
            IReadOnlyList<string> visited = null,
            IReadOnlyList<string> settled = null,
            IReadOnlyList<OverworldProcessedOutcomeDocument> outcomes = null,
            int persistenceRevision = 1,
            string runId = "run-1",
            string currentNodeId = "node-z")
        {
            return new OverworldSaveDocument(
                OverworldSaveSchema.CurrentVersion,
                runStartKind: 0,
                runId,
                runSeed: 12345,
                seedText: "12345",
                chapter: 1,
                finalChapter: 3,
                mapFingerprint: "map-fingerprint",
                mapConfigVersion: 1,
                intermediateLayerCount: 4,
                minimumNodesPerLayer: 2,
                maximumNodesPerLayer: 3,
                currentNodeId,
                activeRoomId: string.Empty,
                activeLaunchCorrelationId: string.Empty,
                chapterCompleted: false,
                chapterAdvanceCount: 0,
                domainRevision: 2,
                operationSequenceCursor: 9,
                persistenceRevision,
                characterId: "silver-character",
                era: 1,
                phase: 2,
                timecoins: 17,
                deck ?? new[] { "card-b", "card-a" },
                visited ?? new[] { "node-z", "node-a" },
                settled ?? new[] { "node-a" },
                outcomes ?? new[]
                {
                    new OverworldProcessedOutcomeDocument("Shop", "outcome-z", "fingerprint-z"),
                    new OverworldProcessedOutcomeDocument("Combat", "outcome-a", "fingerprint-a")
                });
        }

        private static void AssertDocument(
            OverworldSaveDocument actual,
            OverworldSaveDocument expected)
        {
            Assert.That(actual.RunId, Is.EqualTo(expected.RunId));
            Assert.That(actual.RunSeed, Is.EqualTo(expected.RunSeed));
            Assert.That(actual.MapConfigVersion, Is.EqualTo(expected.MapConfigVersion));
            Assert.That(actual.CurrentNodeId, Is.EqualTo(expected.CurrentNodeId));
            Assert.That(actual.DomainRevision, Is.EqualTo(expected.DomainRevision));
            Assert.That(
                actual.OperationSequenceCursor,
                Is.EqualTo(expected.OperationSequenceCursor));
            Assert.That(actual.PersistenceRevision, Is.EqualTo(expected.PersistenceRevision));
            CollectionAssert.AreEqual(expected.DeckStableIds, actual.DeckStableIds);
            CollectionAssert.AreEqual(expected.VisitedNodeIds, actual.VisitedNodeIds);
            CollectionAssert.AreEqual(expected.SettledNodeIds, actual.SettledNodeIds);
            CollectionAssert.AreEqual(
                OutcomeIds(expected.ProcessedOutcomes),
                OutcomeIds(actual.ProcessedOutcomes));
        }

        private static string[] OutcomeIds(
            IReadOnlyList<OverworldProcessedOutcomeDocument> outcomes)
        {
            var result = new string[outcomes.Count];
            for (var index = 0; index < outcomes.Count; index++)
            {
                result[index] = outcomes[index].OutcomeCorrelationId;
            }

            return result;
        }

        private sealed class StageFaultInjector : IOverworldSaveWriteFaultInjector
        {
            private readonly OverworldSaveWriteStage _failureStage;
            private readonly bool _corruptTemporary;

            public StageFaultInjector(
                OverworldSaveWriteStage failureStage,
                bool corruptTemporary)
            {
                _failureStage = failureStage;
                _corruptTemporary = corruptTemporary;
            }

            public void OnStage(OverworldSaveWriteStage stage, string temporaryPath)
            {
                if (stage != _failureStage)
                {
                    return;
                }

                if (_corruptTemporary)
                {
                    File.WriteAllText(temporaryPath, "{invalid-temporary");
                    return;
                }

                throw new IOException("Injected " + stage + " failure.");
            }
        }

        private const string LegacyV0Json = @"{
  ""schemaVersion"": 0,
  ""runStartKind"": 0,
  ""runId"": ""legacy-run"",
  ""runSeed"": 77,
  ""seedText"": ""77"",
  ""chapter"": 1,
  ""finalChapter"": 3,
  ""mapFingerprint"": ""legacy-map"",
  ""intermediateLayerCount"": 4,
  ""minimumNodesPerLayer"": 2,
  ""maximumNodesPerLayer"": 3,
  ""currentNodeId"": ""node-entry"",
  ""domainRevision"": 0,
  ""characterId"": ""silver-character"",
  ""era"": 1,
  ""phase"": 1,
  ""timecoins"": 0,
  ""deckStableIds"": [""card-a""],
  ""visitedNodeIds"": [""node-entry""],
  ""settledNodeIds"": [""node-entry""]
}";
    }
}
