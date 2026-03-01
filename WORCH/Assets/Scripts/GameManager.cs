using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private List<string> wordChain = new List<string>();

    [SerializeField]
    private TextMeshProUGUI currentWordText;
    [SerializeField]
    private TMP_InputField wordInputField;

    [SerializeField]
    private Transform wordBlockParent;
    [SerializeField]
    private WordBlock wordBlockPrefab;
    [SerializeField, Range(1, 1000)]
    private int wordBlockMakeCount = 30;
    private int currentWordBlockIndex;
    private List<WordBlock> wordBlockPooling = new List<WordBlock>();

    private Queue<IEnumerator> routineQueue = new Queue<IEnumerator>();

    private const string WORCH_MODULE_PATH = "";

    private void Awake()
    {
        Setup();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            while (routineQueue.Count > 0)
            {
                yield return routineQueue.Dequeue();
            }

            yield return null;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Return) || wordInputField.text == string.Empty)
        {
            return;
        }

        string word = wordInputField.text;

        wordInputField.text = string.Empty;

        wordInputField.ActivateInputField();

        UpdateWord(word);
    }

    private void Setup()
    {
        for (int i = 0; i < wordBlockMakeCount; i++)
        {
            MakeWordBlock();
        }

        FileSystemWatcher watcher = new FileSystemWatcher(WORCH_MODULE_PATH, "*.out");

        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += OnWordFileChanged;
        watcher.Created += OnWordFileChanged;
        watcher.Deleted += OnWordFileChanged;
        watcher.Renamed += OnWordFileChanged;
        watcher.Error += (sender, e) =>
#if UNITY_EDITOR
        Debug.Log(e.GetException());
#else
        TryStartRoutine(SetCurrentWordTextTimer("<color=red>파일 처리 문제 발생</color>", 2f));
#endif
        watcher.IncludeSubdirectories = false;
        watcher.EnableRaisingEvents = true;
    }

    private void MakeWordBlock()
    {
        WordBlock instance = Instantiate(wordBlockPrefab, wordBlockParent);

        instance.gameObject.SetActive(false);

        wordBlockPooling.Add(instance);
    }

    private void OnWordFileChanged(object sender, FileSystemEventArgs e)
    {
#if UNITY_EDITOR
        Debug.Log($"{e.FullPath} 파일 변경됨");
#endif
        byte[] data = File.ReadAllBytes(e.FullPath);
        string word = Encoding.UTF8.GetString(data);
#if UNITY_EDITOR
        Debug.Log(word);
#endif
        TryStartRoutine(ProcessWord(word));
    }

    private IEnumerator ProcessWord(string word)
    {
        UpdateWord(word);

        yield return null;
    }

    private void UpdateWord(string word)
    {
        if (!CheckWord(word))
        {
            return;
        }

        // 사전에 단어가 있는지 검사합니다.
        KoreanDictionary.Instance?.Check(word, (exist, description) =>
        {
            if (!exist)
            {
                TryStartRoutine(SetCurrentWordTextTimer("<color=red>없는 말</color>", 1f));

                return;
            }

            char c = word[word.Length - 1];

            currentWordText.text = $"{c}";

            // 두음 법칙으로 시작할 수 있는지 표기합니다.
            char convert = KoreanDictionary.Instance.ConvertInitial(word);

            if (convert != ' ')
            {
                currentWordText.text = $"{c}({convert})";
            }

            wordChain.Add(word);

            if (currentWordBlockIndex >= wordBlockPooling.Count)
            {
                MakeWordBlock();
            }

            WordBlock block = wordBlockPooling[currentWordBlockIndex++];

            block.Setup(word, description);

            block.gameObject.SetActive(true);

            TryStartRoutine(SetCurrentWordTextTimer(word, 1f, true));
        });
    }

    private bool CheckWord(string word)
    {
        // 한 글자 단어는 무효로 처리합니다.
        if (word.Length == 1)
        {
            TryStartRoutine(SetCurrentWordTextTimer("<color=red>무효</color>", 1f));

            return false;
        }

        // 단어가 이미 쓰였는지 검사합니다.
        if (wordChain.Contains(word))
        {
            TryStartRoutine(SetCurrentWordTextTimer("<color=red>이미 쓰임</color>", 1f));

            return false;
        }

        // 현재 단어의 마지막 글자로 시작하는지 검사합니다.
        if (wordChain.Count >= 1)
        {
            string currentWord = wordChain[wordChain.Count - 1];
            char convert = KoreanDictionary.Instance.ConvertInitial(currentWord);
            char c = currentWord[currentWord.Length - 1];
            bool equal = c == word[0];  // 현재 단어의 마지막 글자 == 입력한 단어의 첫 글자인가?
            bool initialConsonant = convert == word[0]; // 두음 법칙을 적용해 같은지 검사한 결과입니다.

            if (!equal && !initialConsonant)
            {
                TryStartRoutine(SetCurrentWordTextTimer("<color=red>무효</color>", 1f));

                return false;
            }
        }

        return true;
    }

    private void TryStartRoutine(IEnumerator routine)
    {
        routineQueue.Enqueue(routine);
    }

    private IEnumerator SetCurrentWordTextTimer(string value, float duration, bool typingEffect = false)
    {
        string origin = currentWordText.text;

        if (!typingEffect)
        {
            currentWordText.text = value;

            yield return new WaitForSeconds(duration);

            currentWordText.text = origin;

            yield break;
        }

        float t = duration / value.Length;

        StringBuilder builder = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            builder.Append(value[i]);

            currentWordText.text = builder.ToString();

            yield return new WaitForSeconds(t);
        }

        yield return new WaitForSeconds(duration);

        currentWordText.text = origin;
    }
}
