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

    public void DisplayResult(bool isNetwork, bool? winner, bool playerTeam, EndGameReason reason)
    {
        endGameWindow.SetActive(true);

        if (isNetwork)
        {
            topText.text = winner switch
            {
                false => (winner == playerTeam) ? "ВЫ ВЫИГРАЛИ" : "ВЫ ПРОИГРАЛИ",
                true => (winner == playerTeam) ? "ВЫ ВЫИГРАЛИ" : "ВЫ ПРОИГРАЛИ",
                null => "НИЧЬЯ"
            };
        }

        else topText.text = winner switch
        {
            false => "ПОБЕДИЛИ БЕЛЫЕ",
            true => "ПОБЕДИЛИ ЧЕРНЫЕ",
            null => "НИЧЬЯ"
        };

        lowText.text = reason switch
        {
            EndGameReason.IntellectorCapture => "Интеллектор был взят",
            EndGameReason.IntellectorReachLustRank => "Интеллектор достиг базовой линии",
            EndGameReason.AllPiecesBlocked => "Блокировка",
            EndGameReason.TimesUp => (winner == playerTeam) ? "У противника истекло время" : "Время истекло",
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
