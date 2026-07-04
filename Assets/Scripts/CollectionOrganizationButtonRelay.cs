using UnityEngine;

public class CollectionOrganizationButtonRelay : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;

    private GameFlowController Flow
    {
        get
        {
            if (gameFlowController == null)
            {
                gameFlowController = FindObjectOfType<GameFlowController>();
            }
            return gameFlowController;
        }
    }

    public void CompleteCollection()
    {
        Flow?.CompleteCollection();
    }

    public void ReturnToStudio()
    {
        Flow?.ReturnToStudio();
    }

    public void StartBroadcast()
    {
        Flow?.StartBroadcast();
    }
}
