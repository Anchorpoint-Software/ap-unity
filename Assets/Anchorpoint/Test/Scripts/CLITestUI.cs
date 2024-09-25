using UnityEngine;
using UnityEngine.UI;
using AnchorPoint.Wrapper;
using AnchorPoint.Constants;

public class CLITestUI : MonoBehaviour
{
    [SerializeField] Text _cliVersionText = null;
    [SerializeField] Text _cliPathText    = null;
    [SerializeField] Text _cwdText        = null;
    [Space]
    [SerializeField] RectTransform _output = null;
    [SerializeField] Text _outputText = null;
    [Space]
    [SerializeField] Button _statusButton     = null;
    [SerializeField] Button _pullButton       = null;
    [SerializeField] Button _commitButton     = null;
    [SerializeField] Button _pushButton       = null;
    [SerializeField] Button _syncButton       = null;
    [SerializeField] Button _userListButton   = null;
    [SerializeField] Button _lockListButton   = null;
    [SerializeField] Button _lockCreateButton = null;
    [SerializeField] Button _lockRemoveButton = null;
    [SerializeField] Button _logFileButton    = null;
    [Space]
    [SerializeField] InputField _commitMessage = null;
    [SerializeField] InputField _syncMessage   = null;
    [Space]
    [SerializeField] Toggle _keepToggle = null;

    private void Start()
    {
        _cliVersionText.text = $"CLI Version: {CLIConstants.CLIVersion}";
        _cliPathText.text    = $"CLI Path: {CLIConstants.CLIPath}";
        _cwdText.text        = $"CWD: {CLIConstants.WorkingDirectory}";

        _outputText.text = string.Empty;

        _statusButton.onClick.AddListener(Status);
        _pullButton.onClick.AddListener(Pull);
        _commitButton.onClick.AddListener(()=>CommitAll(_commitMessage.text));
        _pushButton.onClick.AddListener(Push);
        _syncButton.onClick.AddListener(()=>SyncAll(_syncMessage.text));
        _userListButton.onClick.AddListener(UserList);
        _lockListButton.onClick.AddListener(LockList);
        _lockCreateButton.onClick.AddListener(()=>LockCreate(_keepToggle.isOn));
        _lockRemoveButton.onClick.AddListener(LockRemove);
        _logFileButton.onClick.AddListener(LogFile);
    }

    private void FixedUpdate()
    {
        _outputText.text = CLIWrapper.Output;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_output);
    }

    private void Status()                  => CLIWrapper.Status();

    private void Pull()                    => CLIWrapper.Pull();

    private void CommitAll(string message) => CLIWrapper.CommitAll(message);

    private void Push()                    => CLIWrapper.Push();

    private void SyncAll(string message)   => CLIWrapper.SyncAll(message);

    private void UserList()                => CLIWrapper.UserList();

    private void LockList()                => CLIWrapper.LockList();

    private void LockCreate(bool keep)     => CLIWrapper.LockCreate(keep, "Test_1.txt", "Test_2.txt");

    private void LockRemove()              => CLIWrapper.LockRemove("Test_1.txt", "Test_2.txt");

    private void LogFile()                 => CLIWrapper.LogFile("Test_1.txt");

}
