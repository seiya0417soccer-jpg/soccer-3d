using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jev自動プレイの開発用UI。
/// 開始・停止操作と状態表示を担当する。
/// Tabキーで開発用パネルの表示を切り替える。
/// </summary>
public sealed class JevPuzzleAutoPlayUI : MonoBehaviour
{
    [Header("開発用UI設定")]

    [SerializeField]
    [Tooltip("表示・非表示を切り替える開発用パネル")]
    private GameObject _panelRoot;

    [SerializeField]
    [Tooltip("開発用パネルの表示切り替えキー")]
    private KeyCode _togglePanelKey = KeyCode.Tab;

    [Header("自動プレイ")]

    [SerializeField]
    [Tooltip("自動プレイの制御を担当するコンポーネント")]
    private JevPuzzleAutoPlayer _autoPlayer;

    [Header("UI参照")]

    [SerializeField]
    [Tooltip("自動プレイを開始するボタン")]
    private Button _startButton;

    [SerializeField]
    [Tooltip("自動プレイを停止するボタン")]
    private Button _stopButton;

    [SerializeField]
    [Tooltip("自動プレイの状態を表示する通常のUI Text")]
    private Text _statusText;

    // Unity Editorまたは開発用ビルドかどうか
    private static bool IsDevelopmentEnvironment =>
        Application.isEditor || Debug.isDebugBuild;

    // 稼働状態を示す明るい黄緑色（#B7F36B）
    private static readonly Color ActiveButtonColor =
        new Color(183f / 255f, 243f / 255f, 107f / 255f, 1f);

    private void Awake()
    {
        // 製品版では開発用UIを非表示にする
        if (!IsDevelopmentEnvironment)
        {
            gameObject.SetActive(false);
            return;
        }

        // 通常プレイを邪魔しないよう、最初はパネルを隠す
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "[Jev AutoPlay UI] Panel Rootが設定されていません。");
        }
    }

    private void OnEnable()
    {
        if (!IsDevelopmentEnvironment)
        {
            return;
        }

        // ボタンのクリックイベントを登録する
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (_stopButton != null)
        {
            _stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        if (_autoPlayer != null)
        {
            // 自動プレイの状態変化を受け取る
            _autoPlayer.StatusChanged += OnStatusChanged;

            UpdateStatusText(_autoPlayer.Status);
        }
        else if (_statusText != null)
        {
            _statusText.text = "自動プレイヤー未設定";
        }

        UpdateButtonStates();
    }

    private void Update()
    {
        if (!IsDevelopmentEnvironment)
        {
            return;
        }

        // Tabキーで開発用パネルの表示を切り替える
        if (Input.GetKeyDown(_togglePanelKey) &&
            _panelRoot != null)
        {
            _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        // 自動プレイの状態に合わせてボタンを更新する
        UpdateButtonStates();
    }

    private void OnDisable()
    {
        // ボタンのイベント登録を解除する
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        if (_stopButton != null)
        {
            _stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }

        if (_autoPlayer != null)
        {
            _autoPlayer.StatusChanged -= OnStatusChanged;
        }
    }

    /// <summary>
    /// 開始ボタンから自動プレイを開始する。
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (_autoPlayer == null)
        {
            return;
        }

        _autoPlayer.StartAutoPlay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 停止ボタンから自動プレイの停止を要求する。
    /// </summary>
    private void OnStopButtonClicked()
    {
        if (_autoPlayer == null)
        {
            return;
        }

        _autoPlayer.StopAutoPlay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 自動プレイの状態変化を受け取り、UIを更新する。
    /// </summary>
    private void OnStatusChanged(JevAutoPlayStatus status)
    {
        UpdateStatusText(status);
        UpdateButtonStates();
    }

    /// <summary>
    /// 自動プレイの状態を日本語で表示する。
    /// </summary>
    private void UpdateStatusText(JevAutoPlayStatus status)
    {
        if (_statusText == null)
        {
            return;
        }

        switch (status)
        {
            case JevAutoPlayStatus.Idle:
                _statusText.text = "待機中";
                break;

            case JevAutoPlayStatus.WaitingForBoard:
                _statusText.text = "盤面準備待ち";
                break;

            case JevAutoPlayStatus.Judging:
                _statusText.text = "Jev判断中...";
                break;

            case JevAutoPlayStatus.Executing:
                _statusText.text = "移動・盤面処理中";
                break;

            case JevAutoPlayStatus.Stopping:
                _statusText.text = "停止処理中...";
                break;

            case JevAutoPlayStatus.Stopped:
                _statusText.text = "停止済み";
                break;

            case JevAutoPlayStatus.Error:
                _statusText.text = "エラー：Consoleを確認してください";
                break;

            default:
                _statusText.text = "状態不明";
                break;
        }
    }

    /// <summary>
    /// 次に操作すべきボタンを強調し、操作可否も更新する。
    /// </summary>
    private void UpdateButtonStates()
    {
        bool isRunning =
            _autoPlayer != null && _autoPlayer.IsRunning;

        bool isBusy =
            _autoPlayer != null && _autoPlayer.IsBusy;

        // 停止中で処理が残っていなければ、開始ボタンを強調する
        bool canStart =
            _autoPlayer != null && !isRunning && !isBusy;

        // 稼働中は停止ボタンを操作できる
        bool canStop = isRunning;

        SetButtonAppearance(
            _startButton,
            canStart,
            canStart);

        SetButtonAppearance(
            _stopButton,
            canStop,
            canStop);
    }

    /// <summary>
    /// ボタンの操作可否と表示色を設定する。
    /// 強調対象は黄緑色、それ以外は白色にする。
    /// </summary>
    private static void SetButtonAppearance(
        Button button,
        bool interactable,
        bool highlighted)
    {
        if (button == null)
        {
            return;
        }

        Color targetColor =
            highlighted ? ActiveButtonColor : Color.white;

        // 操作状態ごとの色を設定する
        ColorBlock colors = button.colors;

        bool colorChanged =
            colors.normalColor != targetColor ||
            colors.highlightedColor != targetColor ||
            colors.pressedColor != targetColor ||
            colors.selectedColor != targetColor ||
            colors.disabledColor != Color.white;

        if (colorChanged)
        {
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.pressedColor = targetColor;
            colors.selectedColor = targetColor;

            // 無効なボタンもグレーにせず、白色を保つ
            colors.disabledColor = Color.white;

            button.colors = colors;
        }

        // 色とは独立して操作可否を設定する
        if (button.interactable != interactable)
        {
            button.interactable = interactable;
        }
    }
}