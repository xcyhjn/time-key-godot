namespace TimeKey.Infrastructure.Persistence
{
    public enum OverworldSaveLoadStatus
    {
        Missing,
        Valid,
        Migrated,
        Corrupt,
        UnsupportedFutureVersion,
        IoFailure
    }

    public enum OverworldSaveSource
    {
        None,
        Primary,
        Backup
    }

    public sealed class OverworldSaveLoadResult
    {
        internal OverworldSaveLoadResult(
            OverworldSaveLoadStatus status,
            OverworldSaveSource source,
            OverworldSaveDocument document,
            string detail)
        {
            Status = status;
            Source = source;
            Document = document;
            Detail = detail ?? string.Empty;
        }

        public OverworldSaveLoadStatus Status { get; }

        public OverworldSaveSource Source { get; }

        public OverworldSaveDocument Document { get; }

        public string Detail { get; }

        public bool Succeeded =>
            Status == OverworldSaveLoadStatus.Valid ||
            Status == OverworldSaveLoadStatus.Migrated;

        public bool RecoveredFromBackup => Source == OverworldSaveSource.Backup && Succeeded;
    }

    public enum OverworldSaveWriteFailure
    {
        None,
        InvalidDocument,
        TempWrite,
        TempValidation,
        Replace
    }

    public sealed class OverworldSaveWriteResult
    {
        internal OverworldSaveWriteResult(
            OverworldSaveWriteFailure failure,
            string detail)
        {
            Failure = failure;
            Detail = detail ?? string.Empty;
        }

        public bool Succeeded => Failure == OverworldSaveWriteFailure.None;

        public OverworldSaveWriteFailure Failure { get; }

        public string Detail { get; }
    }

    public enum OverworldSaveWriteStage
    {
        BeforeTempWrite,
        BeforeTempValidation,
        BeforeReplace
    }

    public interface IOverworldSaveWriteFaultInjector
    {
        void OnStage(OverworldSaveWriteStage stage, string temporaryPath);
    }
}
