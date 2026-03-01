using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class KoreanDictionary : MonoBehaviour
{
    public static KoreanDictionary Instance { get; private set; }

    private const string API_REQUEST_URL = "https://kli.korean.go.kr/term/api/search.do";
    private const string API_KEY = "";

    private Action<bool, string> onCompleteCheck;

    private Dictionary<char, char> convertInitialMap = new Dictionary<char, char>()
    {
        { '¶ó', '³ª' },
        { '¶ô', '³«' },
        { '¶õ', '³­' },
        { '¶ö', '³¯' },
        { '¶÷', '³²' },
        { '¶ø', '³³' },
        { '¶ù', '³´' },
        { '¶û', '³¶' },
        { '·«', '¾à' },
        { '³Å', '¾à' },
        { '·®', '¾ç' },
        { '³É', '¾ç' },
        { '··', '³Õ' },
        { '·Á', '¿©' },
        { '³à', '¿©' },
        { '·Â', '¿ª' },
        { '³á', '¿ª' },
        { '·Ã', '¿¬' },
        { '³â', '¿¬' },
        { '·Ä', '¿­' },
        { '³ã', '¿­' },
        { '·Å', '¿°' },
        { '³ä', '¿°' },
        { '·Æ', '¿±' },
        { '·É', '¿µ' },
        { '³ç', '¿µ' },
        { '·Î', '³ë' },
        { '·Ï', '³ì' },
        { '·Ð', '³í' },
        { '·Ñ', '³î' },
        { '·Ò', '³ð' },
        { '·Ó', '³ñ' },
        { '·Ô', '³ò' },
        { '·Õ', '³ó' },
        { '·Ú', '³ú' },
        { '·á', '¿ä' },
        { '´¢', '¿ä' },
        { '·æ', '¿ë' },
        { '´¨', '¿ë' },
        { '·ç', '´©' },
        { '·è', '´ª' },
        { '·é', '´«' },
        { '·ê', '´­' },
        { '·ë', '´®' },
        { '·í', '´°' },
        { '·î', '´±' },
        { '·ù', 'À¯' },
        { '´º', 'À¯' },
        { '·ú', 'À°' },
        { '´»', 'À°' },
        { '·û', 'À±' },
        { '·ü', 'À²' },
        { '¸¢', 'À¶' },
        { '¸£', '´À' },
        { '¸¤', '´Á' },
        { '¸¥', '´Â' },
        { '¸¦', '´Ã' },
        { '¸§', '´Æ' },
        { '¸¨', '´Ç' },
        { '¸©', '´È' },
        { '¸ª', '´É' },
        { '·¡', '³»' },
        { '·¢', '³¼' },
        { '·£', '³½' },
        { '·¤', '³¾' },
        { '·¥', '³¿' },
        { '·¦', '³À' },
        { '·§', '³Á' },
        { '·©', '³Ã' },
        { '·Ê', '¿¹' },
        { '³é', '¿¹' },
        { '¸®', 'ÀÌ' },
        { '´Ï', 'ÀÌ' },
        { '¸°', 'ÀÎ' },
        { '´Ñ', 'ÀÎ' },
        { '¸±', 'ÀÏ' },
        { '´Ò', 'ÀÏ' },
        { '¸²', 'ÀÓ' },
        { '´Ô', 'ÀÓ' },
        { '¸³', 'ÀÔ' },
        { '´Õ', 'ÀÔ' },
        { '¸´', 'ÀÕ' },
        { '´Ö', 'ÀÕ' },
        { '¸µ', 'À×' },
        { '´×', 'À×' },
        { '·ý', 'À³' },
        { 'µë', 'À½' }
    };

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// ÇØ´ç ´Ü¾îÀÇ ¸¶Áö¸· ±ÛÀÚ¸¦ µÎÀ½ ¹ýÄ¢À» Àû¿ëÇØ¼­ ÀÎÁ¤µÇ´Â ±ÛÀÚ°¡ ÀÖ´ÂÁö ¹ÝÈ¯ÇÕ´Ï´Ù.
    /// </summary>
    /// <param name="word">¸¶Áö¸· ±ÛÀÚ¿¡ µÎÀ½ ¹ýÄ¢À» Àû¿ëÇÒ ´Ü¾îÀÔ´Ï´Ù.</param>
    /// <returns>µÎÀ½ ¹ýÄ¢À» Àû¿ëÇÒ ¼ö ÀÖÀ¸¸é ÀÎÁ¤µÇ´Â ±ÛÀÚ·Î, ¾Æ´Ï¸é ºó °ø¹éÀ» ¹ÝÈ¯ÇÕ´Ï´Ù.</returns>
    public char ConvertInitial(string word)
    {
        char tail = word[word.Length - 1];

        if (convertInitialMap.TryGetValue(tail, out char convert))
        {
            return convert;
        }

        return ' ';
    }

    private IEnumerator GetRequestAPI(string url, bool several = false)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
#if UNITY_EDITOR
                Debug.Log(www.error);
#endif

                StartCoroutine(GetRequestAPI(url));
            }

            if (!www.isDone)
            {
                yield break;
            }

            string jsonResult = Encoding.UTF8.GetString(www.downloadHandler.data);
#if UNITY_EDITOR
            Debug.Log(jsonResult);
#endif
            string wordFilter = "\"word\" : \"";
            string definitionFilter = "\"definition\" : \"";
            int index = jsonResult.LastIndexOf(wordFilter);
            int definitionIndex = jsonResult.LastIndexOf(definitionFilter);

            if (index == -1)
            {
                onCompleteCheck?.Invoke(false, string.Empty);   // ´Ü¾î°¡ Á¸ÀçÇÏÁö ¾Ê½À´Ï´Ù.

                yield break;
            }

            index += wordFilter.Length;
            definitionIndex += definitionFilter.Length;

            StringBuilder wordBuilder = new StringBuilder();
            StringBuilder definitionBuilder = new StringBuilder();

            for (int i = index; ; i++)
            {
                if (jsonResult[i].Equals('\"'))
                {
                    break;
                }

                wordBuilder.Append(jsonResult[i]);
            }

            for (int i = definitionIndex; ; i++)
            {
                if (jsonResult[i].Equals('\"'))
                {
                    break;
                }

                definitionBuilder.Append(jsonResult[i]);
            }

            onCompleteCheck?.Invoke(true, definitionBuilder.ToString());
        }
    }

    /// <summary>
    /// ÇØ´ç ´Ü¾î°¡ »çÀü¿¡ Á¸ÀçÇÏ´ÂÁö °Ë»çÇÕ´Ï´Ù.
    /// </summary>
    /// <param name="word">°Ë»çÇÒ ´Ü¾îÀÔ´Ï´Ù.</param>
    /// <param name="onCompleteCheck">ÇØ´ç ´Ü¾î°¡ »çÀü¿¡ ÀÖÀ¸¸é true, ¾Æ´Ï¸é false¸¦ ¸Å°³º¯¼ö·Î ÁÖ´Â ÄÝ¹é ÇÔ¼öÀÔ´Ï´Ù. ¶ÇÇÑ ´Ü¾î°¡ Á¸ÀçÇÏ¸é ¼³¸íµµ °°ÀÌ Á¦°øµË´Ï´Ù.</param>
    public void Check(string word, Action<bool, string> onCompleteCheck)
    {
        this.onCompleteCheck = onCompleteCheck;

        string url = $"{API_REQUEST_URL}?key={API_KEY}&apiSearchWord={word}&sort=wt&start=1&num=10";

        StartCoroutine(GetRequestAPI(url));
    }
}
