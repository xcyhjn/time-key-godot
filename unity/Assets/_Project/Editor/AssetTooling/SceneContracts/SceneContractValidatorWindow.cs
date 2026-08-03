using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeKey.Editor.AssetTooling.SceneContracts
{
    public sealed class SceneContractValidatorWindow : EditorWindow
    {
        private const string SilverFontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";

        private TextField _scenePath;
        private Label _summary;
        private ScrollView _diagnostics;
        private Button _copyJson;
        private SceneContractReport _report;

        [MenuItem("Time Key/Tools/Scene Contract Validator")]
        public static void ShowWindow()
        {
            OpenWindow().RunValidation();
        }

        [MenuItem("Time Key/Tools/Validate Combat Scene Contract")]
        public static void ValidateFromMenu()
        {
            var report = SceneContractValidator.ValidateCombatScene();
            Debug.Log(report.ToJson());
            OpenWindow().DisplayReport(report);
        }

        public static SceneContractValidatorWindow OpenWindow()
        {
            var window = GetWindow<SceneContractValidatorWindow>();
            window.titleContent = new GUIContent("Scene Contract Validator");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
            return window;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 16f;
            root.style.paddingRight = 16f;
            root.style.paddingTop = 14f;
            root.style.paddingBottom = 14f;
            var silver = AssetDatabase.LoadAssetAtPath<Font>(SilverFontPath);
            if (silver != null)
            {
                root.style.unityFont = silver;
            }

            var title = new Label("Scene / Prefab 契约校验");
            title.style.fontSize = 22f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;
            root.Add(title);

            var subtitle = new Label("只读诊断稳定节点、序列化引用与 Prefab 来源");
            subtitle.style.color = new Color(0.67f, 0.7f, 0.75f);
            subtitle.style.marginBottom = 12f;
            root.Add(subtitle);

            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.style.marginBottom = 10f;
            _scenePath = new TextField { value = SceneContractValidator.CombatScenePath };
            _scenePath.name = "scene-path";
            _scenePath.style.flexGrow = 1f;
            _scenePath.style.marginRight = 8f;
            controls.Add(_scenePath);

            var validate = new Button(RunValidation) { text = "验证" };
            validate.name = "validate";
            validate.style.width = 76f;
            controls.Add(validate);
            _copyJson = new Button(CopyJson) { text = "复制 JSON" };
            _copyJson.name = "copy-json";
            _copyJson.style.width = 98f;
            _copyJson.style.marginLeft = 6f;
            _copyJson.SetEnabled(false);
            controls.Add(_copyJson);
            root.Add(controls);

            _summary = new Label("尚未执行");
            _summary.name = "summary";
            _summary.style.paddingLeft = 10f;
            _summary.style.paddingRight = 10f;
            _summary.style.paddingTop = 8f;
            _summary.style.paddingBottom = 8f;
            _summary.style.marginBottom = 10f;
            _summary.style.backgroundColor = new Color(0.16f, 0.17f, 0.2f);
            root.Add(_summary);

            _diagnostics = new ScrollView(ScrollViewMode.Vertical);
            _diagnostics.name = "diagnostics";
            _diagnostics.style.flexGrow = 1f;
            _diagnostics.style.borderTopWidth = 1f;
            _diagnostics.style.borderBottomWidth = 1f;
            _diagnostics.style.borderLeftWidth = 1f;
            _diagnostics.style.borderRightWidth = 1f;
            _diagnostics.style.borderTopColor = new Color(0.28f, 0.3f, 0.34f);
            _diagnostics.style.borderBottomColor = new Color(0.28f, 0.3f, 0.34f);
            _diagnostics.style.borderLeftColor = new Color(0.28f, 0.3f, 0.34f);
            _diagnostics.style.borderRightColor = new Color(0.28f, 0.3f, 0.34f);
            root.Add(_diagnostics);

            if (_report != null)
            {
                RenderReport();
            }
        }

        public void DisplayReport(SceneContractReport report)
        {
            _report = report;
            if (_summary != null)
            {
                RenderReport();
            }
        }

        private void RunValidation()
        {
            var path = _scenePath == null
                ? SceneContractValidator.CombatScenePath
                : _scenePath.value;
            DisplayReport(SceneContractValidator.ValidateCombatScene(path));
        }

        private void CopyJson()
        {
            if (_report != null)
            {
                EditorGUIUtility.systemCopyBuffer = _report.ToJson();
            }
        }

        private void RenderReport()
        {
            _summary.text = _report.IsValid
                ? "PASS  错误 0  警告 " + _report.WarningCount + "  通过项 " + _report.InfoCount
                : "FAIL  错误 " + _report.ErrorCount + "  警告 " + _report.WarningCount;
            _summary.style.color = _report.IsValid
                ? new Color(0.58f, 0.86f, 0.66f)
                : new Color(1f, 0.56f, 0.52f);
            _copyJson.SetEnabled(true);
            _diagnostics.Clear();
            for (var index = 0; index < _report.Diagnostics.Count; index++)
            {
                _diagnostics.Add(CreateDiagnosticRow(_report.Diagnostics[index]));
            }
        }

        private static VisualElement CreateDiagnosticRow(SceneContractDiagnostic diagnostic)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = 10f;
            row.style.paddingRight = 10f;
            row.style.paddingTop = 7f;
            row.style.paddingBottom = 7f;
            row.style.borderBottomWidth = 1f;
            row.style.borderBottomColor = new Color(0.23f, 0.24f, 0.27f);

            var severity = new Label(diagnostic.Severity.ToString().ToUpperInvariant());
            severity.style.width = 72f;
            severity.style.unityFontStyleAndWeight = FontStyle.Bold;
            severity.style.color = diagnostic.Severity == SceneContractSeverity.Error
                ? new Color(1f, 0.48f, 0.45f)
                : diagnostic.Severity == SceneContractSeverity.Warning
                    ? new Color(1f, 0.76f, 0.38f)
                    : new Color(0.55f, 0.78f, 0.94f);
            row.Add(severity);

            var detail = new VisualElement();
            detail.style.flexGrow = 1f;
            detail.style.flexShrink = 1f;
            var heading = new Label(diagnostic.ContractId + "  " + diagnostic.ObjectPath);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.whiteSpace = WhiteSpace.Normal;
            detail.Add(heading);
            var message = new Label(diagnostic.Message);
            message.style.whiteSpace = WhiteSpace.Normal;
            message.style.color = new Color(0.76f, 0.78f, 0.82f);
            detail.Add(message);
            if (!string.IsNullOrEmpty(diagnostic.PropertyPath))
            {
                var property = new Label(diagnostic.PropertyPath);
                property.style.whiteSpace = WhiteSpace.Normal;
                property.style.color = new Color(0.58f, 0.61f, 0.67f);
                detail.Add(property);
            }
            row.Add(detail);
            return row;
        }
    }
}
