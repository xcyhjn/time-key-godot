using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimeKey.Infrastructure.Persistence
{
    public sealed class OverworldSaveRepository
    {
        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include
            };

        private readonly IOverworldSaveWriteFaultInjector _faultInjector;

        public OverworldSaveRepository(
            string primaryPath,
            IOverworldSaveWriteFaultInjector faultInjector = null)
        {
            if (string.IsNullOrWhiteSpace(primaryPath))
            {
                throw new ArgumentException("A caller-owned save path is required.", nameof(primaryPath));
            }

            PrimaryPath = Path.GetFullPath(primaryPath);
            BackupPath = PrimaryPath + ".bak";
            TemporaryPath = PrimaryPath + ".tmp";
            _faultInjector = faultInjector ?? NoOpFaultInjector.Instance;
        }

        public string PrimaryPath { get; }

        public string BackupPath { get; }

        public string TemporaryPath { get; }

        public OverworldSaveLoadResult Load()
        {
            var primary = LoadSingle(PrimaryPath, OverworldSaveSource.Primary);
            if (primary.Succeeded)
            {
                return primary;
            }

            var backup = LoadSingle(BackupPath, OverworldSaveSource.Backup);
            if (backup.Succeeded)
            {
                return backup;
            }

            if (primary.Status != OverworldSaveLoadStatus.Missing)
            {
                return primary;
            }

            return backup.Status == OverworldSaveLoadStatus.Missing ? primary : backup;
        }

        public OverworldSaveWriteResult Save(OverworldSaveDocument document)
        {
            if (document == null)
            {
                return Failed(
                    OverworldSaveWriteFailure.InvalidDocument,
                    "A save document is required.");
            }

            string serialized;
            try
            {
                serialized = Serialize(document);
                var initialValidation = Parse(serialized, OverworldSaveSource.None);
                if (initialValidation.Status != OverworldSaveLoadStatus.Valid ||
                    !document.ContentEquals(initialValidation.Document))
                {
                    return Failed(
                        OverworldSaveWriteFailure.InvalidDocument,
                        "The save document did not survive schema validation.");
                }
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                return Failed(OverworldSaveWriteFailure.InvalidDocument, exception.Message);
            }

            var stage = OverworldSaveWriteFailure.TempWrite;
            try
            {
                DeleteIfExists(TemporaryPath);
                _faultInjector.OnStage(
                    OverworldSaveWriteStage.BeforeTempWrite,
                    TemporaryPath);
                WriteAndFlush(TemporaryPath, serialized);

                stage = OverworldSaveWriteFailure.TempValidation;
                _faultInjector.OnStage(
                    OverworldSaveWriteStage.BeforeTempValidation,
                    TemporaryPath);
                var temporaryValidation = LoadSingle(
                    TemporaryPath,
                    OverworldSaveSource.None);
                if (temporaryValidation.Status != OverworldSaveLoadStatus.Valid ||
                    !document.ContentEquals(temporaryValidation.Document))
                {
                    return Failed(
                        OverworldSaveWriteFailure.TempValidation,
                        "The fully flushed temporary save failed schema/content validation.");
                }

                stage = OverworldSaveWriteFailure.Replace;
                _faultInjector.OnStage(
                    OverworldSaveWriteStage.BeforeReplace,
                    TemporaryPath);
                ReplacePrimary();
                return new OverworldSaveWriteResult(
                    OverworldSaveWriteFailure.None,
                    string.Empty);
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                return Failed(stage, exception.Message);
            }
            finally
            {
                TryDeleteTemporary();
            }
        }

        public OverworldSaveWriteResult DeleteAll()
        {
            try
            {
                DeleteIfExists(TemporaryPath);
                DeleteIfExists(PrimaryPath);
                DeleteIfExists(BackupPath);
                return new OverworldSaveWriteResult(
                    OverworldSaveWriteFailure.None,
                    string.Empty);
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                return Failed(OverworldSaveWriteFailure.Replace, exception.Message);
            }
        }

        private void ReplacePrimary()
        {
            if (!File.Exists(PrimaryPath))
            {
                File.Move(TemporaryPath, PrimaryPath);
                return;
            }

            var primary = LoadSingle(PrimaryPath, OverworldSaveSource.Primary);
            var backup = LoadSingle(BackupPath, OverworldSaveSource.Backup);
            var primaryIsRecoverable = primary.Succeeded;
            var backupIsRecoverable = backup.Succeeded;

            if (primaryIsRecoverable)
            {
                File.Replace(TemporaryPath, PrimaryPath, BackupPath);
                return;
            }

            // Do not let a corrupt/future primary replace a valid backup.
            File.Replace(
                TemporaryPath,
                PrimaryPath,
                backupIsRecoverable ? null : BackupPath);
        }

        private static OverworldSaveLoadResult LoadSingle(
            string path,
            OverworldSaveSource source)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return Result(
                        OverworldSaveLoadStatus.Missing,
                        source,
                        null,
                        "Save file is missing.");
                }

                return Parse(File.ReadAllText(path, Encoding.UTF8), source);
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                return Result(
                    OverworldSaveLoadStatus.IoFailure,
                    source,
                    null,
                    exception.Message);
            }
        }

        private static OverworldSaveLoadResult Parse(
            string json,
            OverworldSaveSource source)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException exception)
            {
                return Result(
                    OverworldSaveLoadStatus.Corrupt,
                    source,
                    null,
                    exception.Message);
            }

            var schemaToken = root["schemaVersion"];
            if (schemaToken == null || schemaToken.Type != JTokenType.Integer)
            {
                return Result(
                    OverworldSaveLoadStatus.Corrupt,
                    source,
                    null,
                    "schemaVersion must be a JSON integer.");
            }

            var schemaVersion = schemaToken.Value<int>();
            if (schemaVersion > OverworldSaveSchema.CurrentVersion)
            {
                return Result(
                    OverworldSaveLoadStatus.UnsupportedFutureVersion,
                    source,
                    null,
                    "Save schema " + schemaVersion + " is newer than supported schema " +
                    OverworldSaveSchema.CurrentVersion + ".");
            }

            try
            {
                if (schemaVersion == 0)
                {
                    var legacy = root.ToObject<LegacyV0SaveDocument>(
                        JsonSerializer.Create(JsonSettings));
                    return Result(
                        OverworldSaveLoadStatus.Migrated,
                        source,
                        MigrateV0(legacy),
                        "Migrated the approved V0 fixture to the current schema defaults.");
                }

                if (schemaVersion == 1)
                {
                    root["schemaVersion"] = OverworldSaveSchema.CurrentVersion;
                    if (root["domainJournal"] == null)
                    {
                        root["domainJournal"] = new JArray();
                    }

                    var migrated = root.ToObject<OverworldSaveDocument>(
                        JsonSerializer.Create(JsonSettings));
                    return Result(
                        OverworldSaveLoadStatus.Migrated,
                        source,
                        migrated,
                        "Migrated schema 1 to schema 2 operation-journal defaults.");
                }

                if (schemaVersion != OverworldSaveSchema.CurrentVersion)
                {
                    return Result(
                        OverworldSaveLoadStatus.Corrupt,
                        source,
                        null,
                        "Save schema cannot be negative.");
                }

                var document = root.ToObject<OverworldSaveDocument>(
                    JsonSerializer.Create(JsonSettings));
                return Result(
                    OverworldSaveLoadStatus.Valid,
                    source,
                    document,
                    string.Empty);
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                return Result(
                    OverworldSaveLoadStatus.Corrupt,
                    source,
                    null,
                    exception.Message);
            }
        }

        private static OverworldSaveDocument MigrateV0(LegacyV0SaveDocument legacy)
        {
            if (legacy == null)
            {
                throw new JsonSerializationException("The V0 save document is missing.");
            }

            return new OverworldSaveDocument(
                OverworldSaveSchema.CurrentVersion,
                legacy.RunStartKind,
                legacy.RunId,
                legacy.RunSeed,
                legacy.SeedText,
                legacy.Chapter,
                legacy.FinalChapter,
                legacy.MapFingerprint,
                OverworldSaveSchema.V0DefaultMapConfigVersion,
                legacy.IntermediateLayerCount,
                legacy.MinimumNodesPerLayer,
                legacy.MaximumNodesPerLayer,
                legacy.CurrentNodeId,
                activeRoomId: string.Empty,
                activeLaunchCorrelationId: string.Empty,
                chapterCompleted: false,
                chapterAdvanceCount: 0,
                legacy.DomainRevision,
                operationSequenceCursor: 0,
                persistenceRevision: 0,
                legacy.CharacterId,
                legacy.Era,
                legacy.Phase,
                legacy.Timecoins,
                legacy.DeckStableIds,
                legacy.VisitedNodeIds,
                legacy.SettledNodeIds,
                new OverworldProcessedOutcomeDocument[0]);
        }

        private static string Serialize(OverworldSaveDocument document)
        {
            return JsonConvert.SerializeObject(document, JsonSettings);
        }

        private static void WriteAndFlush(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(
                       path,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(true);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void TryDeleteTemporary()
        {
            try
            {
                DeleteIfExists(TemporaryPath);
            }
            catch (Exception exception) when (IsExpectedPersistenceException(exception))
            {
                // The write result already reports the authoritative failure.
            }
        }

        private static bool IsExpectedPersistenceException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is ArgumentException ||
                   exception is NotSupportedException ||
                   exception is JsonException;
        }

        private static OverworldSaveLoadResult Result(
            OverworldSaveLoadStatus status,
            OverworldSaveSource source,
            OverworldSaveDocument document,
            string detail)
        {
            return new OverworldSaveLoadResult(status, source, document, detail);
        }

        private static OverworldSaveWriteResult Failed(
            OverworldSaveWriteFailure failure,
            string detail)
        {
            return new OverworldSaveWriteResult(failure, detail);
        }

        private sealed class NoOpFaultInjector : IOverworldSaveWriteFaultInjector
        {
            public static readonly NoOpFaultInjector Instance = new NoOpFaultInjector();

            public void OnStage(OverworldSaveWriteStage stage, string temporaryPath)
            {
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private sealed class LegacyV0SaveDocument
        {
            [JsonConstructor]
            public LegacyV0SaveDocument(
                int schemaVersion,
                int runStartKind,
                string runId,
                int runSeed,
                string seedText,
                int chapter,
                int finalChapter,
                string mapFingerprint,
                int intermediateLayerCount,
                int minimumNodesPerLayer,
                int maximumNodesPerLayer,
                string currentNodeId,
                int domainRevision,
                string characterId,
                int era,
                int phase,
                int timecoins,
                IEnumerable<string> deckStableIds,
                IEnumerable<string> visitedNodeIds,
                IEnumerable<string> settledNodeIds)
            {
                if (schemaVersion != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(schemaVersion));
                }

                SchemaVersion = schemaVersion;
                RunStartKind = runStartKind;
                RunId = runId;
                RunSeed = runSeed;
                SeedText = seedText;
                Chapter = chapter;
                FinalChapter = finalChapter;
                MapFingerprint = mapFingerprint;
                IntermediateLayerCount = intermediateLayerCount;
                MinimumNodesPerLayer = minimumNodesPerLayer;
                MaximumNodesPerLayer = maximumNodesPerLayer;
                CurrentNodeId = currentNodeId;
                DomainRevision = domainRevision;
                CharacterId = characterId;
                Era = era;
                Phase = phase;
                Timecoins = timecoins;
                DeckStableIds = deckStableIds;
                VisitedNodeIds = visitedNodeIds;
                SettledNodeIds = settledNodeIds;
            }

            [JsonProperty("schemaVersion")]
            public int SchemaVersion { get; }

            [JsonProperty("runStartKind")]
            public int RunStartKind { get; }

            [JsonProperty("runId")]
            public string RunId { get; }

            [JsonProperty("runSeed")]
            public int RunSeed { get; }

            [JsonProperty("seedText")]
            public string SeedText { get; }

            [JsonProperty("chapter")]
            public int Chapter { get; }

            [JsonProperty("finalChapter")]
            public int FinalChapter { get; }

            [JsonProperty("mapFingerprint")]
            public string MapFingerprint { get; }

            [JsonProperty("intermediateLayerCount")]
            public int IntermediateLayerCount { get; }

            [JsonProperty("minimumNodesPerLayer")]
            public int MinimumNodesPerLayer { get; }

            [JsonProperty("maximumNodesPerLayer")]
            public int MaximumNodesPerLayer { get; }

            [JsonProperty("currentNodeId")]
            public string CurrentNodeId { get; }

            [JsonProperty("domainRevision")]
            public int DomainRevision { get; }

            [JsonProperty("characterId")]
            public string CharacterId { get; }

            [JsonProperty("era")]
            public int Era { get; }

            [JsonProperty("phase")]
            public int Phase { get; }

            [JsonProperty("timecoins")]
            public int Timecoins { get; }

            [JsonProperty("deckStableIds")]
            public IEnumerable<string> DeckStableIds { get; }

            [JsonProperty("visitedNodeIds")]
            public IEnumerable<string> VisitedNodeIds { get; }

            [JsonProperty("settledNodeIds")]
            public IEnumerable<string> SettledNodeIds { get; }
        }
    }
}
