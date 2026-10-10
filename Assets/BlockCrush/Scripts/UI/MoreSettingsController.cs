using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class MoreSettingsController : MonoBehaviour
    {
        [SerializeField] GameObject panel, informationPanel;
        [SerializeField] Text versionText, informationTitle, informationBody;
        [SerializeField] SettingsPanelController settings;
        [SerializeField] GameAudioService audioService;
        public bool IsOpen => panel.activeSelf;
        public bool InformationOpen => informationPanel.activeSelf;
        public Text VersionText => versionText;
        public string LastAction { get; private set; }
        public void Configure(GameObject popup, GameObject information, Text version, Text title, Text body,
            SettingsPanelController source, GameAudioService audio)
        { panel = popup; informationPanel = information; versionText = version; informationTitle = title; informationBody = body; settings = source; audioService = audio; }
        void Start() => versionText.text = "Version: " + Application.version;
        public void Open()
        { versionText.text = "Version: " + Application.version; panel.SetActive(true); panel.transform.SetAsLastSibling(); audioService.Feedback(); }
        public void Close() { informationPanel.SetActive(false); panel.SetActive(false); settings.Open(); }
        public void Contact() => ShowInformation("Contact Us", "A support email or contact URL has not been configured for this build.");
        public void Share() => ShowInformation("Share With Friends", "A public game link or native sharing provider has not been configured for this build.");
        public void Terms() => ShowInformation("Terms of Service", "Terms of Service content has not been supplied for this build.");
        public void Privacy() => ShowInformation("Privacy Policy", "Privacy Policy content has not been supplied for this build.");
        public void About() => ShowInformation("About Us", "Block Match Blast\nVersion: " + Application.version + "\n\nPlace blocks on the 8 × 8 board. Complete rows and columns to clear them and score points.");
        public void ShowInformation(string title, string body)
        {
            LastAction = title; informationTitle.text = title; informationBody.text = body;
            informationPanel.SetActive(true); informationPanel.transform.SetAsLastSibling(); audioService.Feedback();
            Debug.Log("BlockCrush UI action: " + title);
        }
        public void CloseInformation() { informationPanel.SetActive(false); audioService.Feedback(); }
    }
}
