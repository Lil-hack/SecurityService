using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public int Coins { get; private set; }
    public int Total { get; private set; }

    GUIStyle scoreStyle;
    GUIStyle hintStyle;
    GUIStyle winStyle;

    void Start()
    {
        Total = FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
    }

    public void AddCoin()
    {
        Coins++;
    }

    void OnGUI()
    {
        EnsureStyles();
        GUI.Label(new Rect(24f, 18f, 480f, 42f), $"Монеты: {Coins} / {Total}", scoreStyle);
        GUI.Label(new Rect(24f, 58f, 640f, 32f), "WASD или стрелки — бегать", hintStyle);

        if (Total > 0 && Coins >= Total)
        {
            GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 64f), "Все монеты собраны!", winStyle);
        }
    }

    void EnsureStyles()
    {
        if (scoreStyle != null)
            return;

        scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };
        scoreStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(scoreStyle)
        {
            fontSize = 16,
            fontStyle = FontStyle.Normal
        };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        winStyle = new GUIStyle(scoreStyle)
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter
        };
        winStyle.normal.textColor = new Color(1f, 0.86f, 0.25f);
    }
}
