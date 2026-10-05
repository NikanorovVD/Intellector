using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class AroundIntellector : MonoBehaviour
{
    public bool? Answer;

    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject noButton;

    private void Start()
    {
        Answer = null;
    }

    private void Awake()
    {
        GetAnswer(
            yesAction: () => { Answer = true; },
            noAction: () => { Answer = false; }
        );
    }

    public void GetAnswer(UnityAction yesAction, UnityAction noAction)
    {
        yesButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            yesAction();
        });
        noButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            noAction();
        });
    }
}
