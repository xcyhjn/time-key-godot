using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeKey.Editor.AssetTooling.SceneContracts
{
    public enum SceneContractSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    [Serializable]
    public sealed class SceneContractDiagnostic
    {
        [SerializeField] private SceneContractSeverity severity;
        [SerializeField] private string contractId;
        [SerializeField] private string assetPath;
        [SerializeField] private string objectPath;
        [SerializeField] private string propertyPath;
        [SerializeField] private string message;
        [SerializeField] private string remediation;

        internal SceneContractDiagnostic(
            SceneContractSeverity severity,
            string contractId,
            string assetPath,
            string objectPath,
            string propertyPath,
            string message,
            string remediation)
        {
            this.severity = severity;
            this.contractId = contractId ?? string.Empty;
            this.assetPath = assetPath ?? string.Empty;
            this.objectPath = objectPath ?? string.Empty;
            this.propertyPath = propertyPath ?? string.Empty;
            this.message = message ?? string.Empty;
            this.remediation = remediation ?? string.Empty;
        }

        public SceneContractSeverity Severity => severity;
        public string ContractId => contractId;
        public string AssetPath => assetPath;
        public string ObjectPath => objectPath;
        public string PropertyPath => propertyPath;
        public string Message => message;
        public string Remediation => remediation;
    }

    [Serializable]
    public sealed class SceneContractReport
    {
        [SerializeField] private string assetPath;
        [SerializeField] private int errorCount;
        [SerializeField] private int warningCount;
        [SerializeField] private int infoCount;
        [SerializeField] private List<SceneContractDiagnostic> diagnostics;

        internal SceneContractReport(
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            this.assetPath = assetPath ?? string.Empty;
            this.diagnostics = diagnostics ?? new List<SceneContractDiagnostic>();
            for (var index = 0; index < this.diagnostics.Count; index++)
            {
                switch (this.diagnostics[index].Severity)
                {
                    case SceneContractSeverity.Error:
                        errorCount++;
                        break;
                    case SceneContractSeverity.Warning:
                        warningCount++;
                        break;
                    default:
                        infoCount++;
                        break;
                }
            }
        }

        public string AssetPath => assetPath;
        public int ErrorCount => errorCount;
        public int WarningCount => warningCount;
        public int InfoCount => infoCount;
        public bool IsValid => errorCount == 0;
        public IReadOnlyList<SceneContractDiagnostic> Diagnostics => diagnostics;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}
