using UnityEngine;

public class CollectionOrganizationWorkspace : MonoBehaviour
{
    [SerializeField] private RectTransform collectionArea;
    [SerializeField] private RectTransform organizationArea;
    [SerializeField] private RectTransform sharedCollectedBoard;
    [SerializeField] private InfoCollectionController infoCollectionController;
    [SerializeField] private InfoCollectionTransitionController transitionController;

    private Vector2 collectionAreaStart;
    private Vector2 organizationAreaShown;
    private Vector2 sharedBoardStart;
    private bool cachedPositions;

    public RectTransform CollectionArea => collectionArea;
    public RectTransform OrganizationArea => organizationArea;
    public RectTransform SharedCollectedBoard => sharedCollectedBoard;

    private void Awake()
    {
        CachePositions();
        if (infoCollectionController == null)
        {
            infoCollectionController = GetComponentInParent<InfoCollectionController>();
        }
        if (transitionController == null)
        {
            transitionController = GetComponentInChildren<InfoCollectionTransitionController>(true);
        }
    }

    public void ShowCollectionMode()
    {
        CachePositions();
        if (collectionArea != null)
        {
            collectionArea.gameObject.SetActive(true);
            collectionArea.anchoredPosition = collectionAreaStart;
        }
        if (organizationArea != null)
        {
            organizationArea.gameObject.SetActive(false);
            organizationArea.anchoredPosition = organizationAreaShown;
        }
        if (sharedCollectedBoard != null)
        {
            sharedCollectedBoard.gameObject.SetActive(true);
            sharedCollectedBoard.anchoredPosition = sharedBoardStart;
        }
        if (infoCollectionController != null)
        {
            infoCollectionController.SetCollectedBoardMode(CollectedBoardInteractionMode.Collection);
            infoCollectionController.SetPageButtonsHiddenForTransition(false);
        }
        if (transitionController != null)
        {
            transitionController.ResetLayout();
        }
    }

    public void PrepareCollectionToOrganizationTransition()
    {
        CachePositions();
        if (collectionArea != null)
        {
            collectionArea.gameObject.SetActive(true);
            collectionArea.anchoredPosition = collectionAreaStart;
        }
        if (organizationArea != null)
        {
            organizationArea.gameObject.SetActive(true);
            organizationArea.anchoredPosition = GetOrganizationHiddenPosition();
        }
        if (sharedCollectedBoard != null)
        {
            sharedCollectedBoard.gameObject.SetActive(true);
            sharedCollectedBoard.anchoredPosition = sharedBoardStart;
        }
        if (infoCollectionController != null)
        {
            infoCollectionController.SetCollectedBoardMode(CollectedBoardInteractionMode.Organization);
        }
    }

    public void ShowOrganizationMode()
    {
        CachePositions();
        if (collectionArea != null)
        {
            collectionArea.gameObject.SetActive(false);
            collectionArea.anchoredPosition = collectionAreaStart;
        }
        if (organizationArea != null)
        {
            organizationArea.gameObject.SetActive(true);
            organizationArea.anchoredPosition = organizationAreaShown;
        }
        if (sharedCollectedBoard != null)
        {
            sharedCollectedBoard.gameObject.SetActive(true);
            sharedCollectedBoard.anchoredPosition = sharedBoardStart;
        }
        if (infoCollectionController != null)
        {
            infoCollectionController.SetCollectedBoardMode(CollectedBoardInteractionMode.Organization);
        }
    }

    public Vector2 GetOrganizationShownPosition()
    {
        CachePositions();
        return organizationAreaShown;
    }

    public Vector2 GetOrganizationHiddenPosition()
    {
        CachePositions();
        RectTransform root = transform as RectTransform;
        float offset = root != null ? root.rect.height + 120f : 520f;
        return organizationAreaShown + new Vector2(0f, offset);
    }

    private void CachePositions()
    {
        if (cachedPositions)
        {
            return;
        }

        if (collectionArea != null)
        {
            collectionAreaStart = collectionArea.anchoredPosition;
        }
        if (organizationArea != null)
        {
            organizationAreaShown = organizationArea.anchoredPosition;
        }
        if (sharedCollectedBoard != null)
        {
            sharedBoardStart = sharedCollectedBoard.anchoredPosition;
        }
        cachedPositions = true;
    }
}
