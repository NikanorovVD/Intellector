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
    [SerializeField] GameObject EndGameWindow;
    [SerializeField] GameObject Rematch;
    [SerializeField] NetworkManager networkManager;
    private Text low_text;
    private Text top_text;

    private void Awake()
    {
        Text[] text = EndGameWindow.GetComponentsInChildren<Text>();
        low_text = text[0];
        top_text = text[1];
        networkManager.ExitEvent += () => RematchSetActive(false);
        networkManager.RematchEvent += () => DisplayRematchRequest();
    }

    public void DisplayResult(bool isNetwork, bool? winner, bool playerTeam, EndGameReason reason)
    {
        EndGameWindow.SetActive(true);

        if (isNetwork)
        {
            top_text.text = winner switch
            {
                false => (winner == playerTeam) ? "ВЫ ВЫИГРАЛИ" : "ВЫ ПРОИГРАЛИ",
                true => (winner == playerTeam) ? "ВЫ ВЫИГРАЛИ" : "ВЫ ПРОИГРАЛИ",
                null => "НИЧЬЯ"
            };
        }

        else top_text.text = winner switch
        {
            false => "ПОБЕДИЛИ БЕЛЫЕ",
            true => "ПОБЕДИЛИ ЧЕРНЫЕ",
            null => "НИЧЬЯ"
        };

        low_text.text = reason switch
        {
            EndGameReason.IntellectorCapture => "Интеллектор был взят",
            EndGameReason.IntellectorReachLustRank => "Интеллектор достиг базовой линии",
            EndGameReason.AllPiecesBlocked => "Блокировка",
            EndGameReason.TimesUp => (winner == playerTeam) ? "У противник истекло время" : "Время истекло",
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
        EndGameWindow.SetActive(false);
    }

    private void DisplayRematchRequest()
    {
        low_text.text = "ПРОТИВНИК ПРЕДЛАГАЕТ РЕВАНШ";
    }

    private void RematchSetActive(bool active)
    {
        Rematch.SetActive(active);
    }
}
