using UnityEngine;
using UnityEngine.UI;

public enum EndGameReason
{
    IntellectorCapture,
    IntellectorReachLustRank,
    AllPiecesBlocked,
    TimesUp,
    Exit,
    Resignation,
    DrawByAgreement,
    DrawByRepeatingPosition,
    DrawBy30MovesRule
}

public class EndGame : MonoBehaviour
{
    [SerializeField] private GameObject endGameWindow;
    [SerializeField] private GameObject rematch;
    [SerializeField] private NetworkManager networkManager;
    private Text lowText;
    private Text topText;

    private void Awake()
    {
        Text[] text = endGameWindow.GetComponentsInChildren<Text>();
        lowText = text[0];
        topText = text[1];
        networkManager.ExitEvent += () => RematchSetActive(false);
        networkManager.RematchEvent += () => DisplayRematchRequest();
    }

    public void DisplayResult(bool isNetwork, Side? winner, Side playerTeam, EndGameReason reason)
    {
        endGameWindow.SetActive(true);

        if (isNetwork)
        {
            topText.text = winner == null
                ? "НИЧЬЯ"
                : winner == playerTeam ? "ВЫ ВЫИГРАЛИ" : "ВЫ ПРОИГРАЛИ";
        }
        else
        {
            topText.text = winner switch
            {
                Side.White => "ПОБЕДИЛИ БЕЛЫЕ",
                Side.Black => "ПОБЕДИЛИ ЧЕРНЫЕ",
                null => "НИЧЬЯ"
            };
        }

        lowText.text = reason switch
        {
            EndGameReason.IntellectorCapture => "Интеллектор был взят",
            EndGameReason.IntellectorReachLustRank => "Интеллектор достиг базовой линии",
            EndGameReason.AllPiecesBlocked => "Блокировка",
            EndGameReason.TimesUp => isNetwork
                ? (winner == playerTeam ? "У противника истекло время" : "Время истекло")
                : "Время истекло",
            EndGameReason.Exit => "Противник вышел",
            EndGameReason.Resignation => (winner == playerTeam) ? "Противник сдался" : "Вы сдались",
            EndGameReason.DrawByAgreement => "По договоренности",
            EndGameReason.DrawByRepeatingPosition => "Троекратное повторение позиции",
            EndGameReason.DrawBy30MovesRule => "По правилу 30 ходов",
            _ => string.Empty
        };
    }

    public void Hide()
    {
        endGameWindow.SetActive(false);
    }

    private void DisplayRematchRequest()
    {
        lowText.text = "ПРОТИВНИК ПРЕДЛАГАЕТ РЕВАНШ";
    }

    private void RematchSetActive(bool active)
    {
        rematch.SetActive(active);
    }
}
