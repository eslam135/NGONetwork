using UnityEngine;

public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance { get; private set; }

    [SerializeField] private KillFeedEntryUI _entryPrefab;
    [SerializeField] private Transform _entriesParent;

    [Header("Stack Settings")]
    [SerializeField] private int _maxMessages = 5;
    [SerializeField] private bool _newestMessageOnTop = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowMessage(string message)
    {
        if (_entryPrefab == null || _entriesParent == null)
        {
            Debug.LogWarning("KillFeedUI is missing references.");
            return;
        }

        KillFeedEntryUI entry = Instantiate(_entryPrefab, _entriesParent);
        entry.Initialize(message);

        if (_newestMessageOnTop)
        {
            entry.transform.SetAsFirstSibling();
        }
        else
        {
            entry.transform.SetAsLastSibling();
        }

        TrimOldMessages();
    }

    private void TrimOldMessages()
    {
        if (_entriesParent.childCount <= _maxMessages)
        {
            return;
        }

        int childIndexToRemove = _newestMessageOnTop
            ? _entriesParent.childCount - 1
            : 0;

        Destroy(_entriesParent.GetChild(childIndexToRemove).gameObject);
    }
}