using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ProgressorEnd : MonoBehaviour
{
    public PieceType? Answer;

    [SerializeField] private GameObject dominatorButton;
    [SerializeField] private GameObject agressorButton;
    [SerializeField] private GameObject liberatorButton;
    [SerializeField] private GameObject defensorButton;

    private void Start()
    {
        Answer = null;
    }

    private void Awake()
    {
        GetAnswer(
            () => { Answer = PieceType.Dominator; },
            () => { Answer = PieceType.Agressor; },
            () => { Answer = PieceType.Liberator; },
            () => { Answer = PieceType.Defensor; }
            );
    }

    public void GetAnswer(UnityAction dominatorAction, UnityAction agressorAction, UnityAction liberatorAction, UnityAction defensorAction)
    {
        dominatorButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            dominatorAction();
        });
        agressorButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            agressorAction();
        });
        liberatorButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            liberatorAction();
        });
        defensorButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            defensorAction();
        });
    }
}
