using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Apkd;
using System.Linq;

public class PortalCreator : MonoBehaviour
{
    [Inject]
    ReadOnlySet<Portal> portals { get; }

    [S] GameObject viewFinderPrefab { get; }

    Plane plane = new Plane(Vector3.forward, new Vector3());
    private Vector2 dragStart;
    private Vector2 dragEnd;
    private GameObject viewFinder;
    private RaycastHit enterHit;
    private float enter;

    private void Start()
    {
        viewFinder = Instantiate(viewFinderPrefab);
        viewFinder.SetActive(false);
    }

    void DisableClick()
    {
        if (Input.GetMouseButtonDown(1))
            viewFinder.SetActive(false);
    }

    private void OnMouseDown()
    {
        viewFinder.SetActive(true);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out enter))
        {
            dragStart = ray.GetPoint(enter);
            viewFinder.transform.position = dragStart;
        }       
    }

    private void OnMouseDrag()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out enter))
        {
            dragEnd = ray.GetPoint(enter);
        }

        float angle = Mathf.Atan2(dragEnd.y - dragStart.y, dragEnd.x - dragStart.x) * 180 / Mathf.PI;
        viewFinder.transform.rotation = Quaternion.Euler(0, 0, angle);

        DisableClick();

    }

    private void OnMouseUp()
    {
        if (!viewFinder.activeSelf)
            return;

        viewFinder.SetActive(false);
        var portal = portals.Where(x => x.Type == Portal.PortalType.Player).ToArray()[NextIdexOfPortal()];

        portal.transform.parent.position = dragStart;
        portal.transform.parent.rotation = viewFinder.transform.rotation;
    }

    int curretnIndex = 0;
    int NextIdexOfPortal()
    {
        curretnIndex++;

        if (curretnIndex >= portals.Count(x => x.Type == Portal.PortalType.Player))
            curretnIndex = 0;

        return curretnIndex;
    }
}
