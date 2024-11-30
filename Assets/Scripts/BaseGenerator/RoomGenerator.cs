using System.Collections;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject wallWithDoorPrefab;
    public GameObject wallWithWindowPrefab;

    private Transform emplacement_N, emplacement_S, emplacement_E, emplacement_W;
    public bool isNorthWallInstantiated = false, isSouthWallInstantiated = false, isEastWallInstantiated = false, isWestWallInstantiated = false;

    void Start()
    {
        Debug.Log($"Démarrage de la salle {this.name}. Initialisation des emplacements...");
        StartCoroutine(InitializeRoom());
    }

    IEnumerator InitializeRoom()
    {
        float waitTime = 0;
        while ((emplacement_N == null || emplacement_S == null || emplacement_E == null || emplacement_W == null) && waitTime < 1f)
        {
            emplacement_N = transform.Find("Emplacement_N");
            emplacement_S = transform.Find("Emplacement_S");
            emplacement_E = transform.Find("Emplacement_E");
            emplacement_W = transform.Find("Emplacement_W");

            waitTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (emplacement_N == null || emplacement_S == null || emplacement_E == null || emplacement_W == null)
        {
            Debug.LogError("Erreur: Un ou plusieurs emplacements de murs sont introuvables.");
            yield break;
        }

        Debug.Log("Tous les emplacements sont prêts.");
    }

    public IEnumerator SetupRoomCoroutine(bool northDoor, bool southDoor, bool eastDoor, bool westDoor, bool isNorthExternal, bool isSouthExternal, bool isEastExternal, bool isWestExternal)
    {
        Debug.Log("Début de la configuration de la salle avec portes et fenêtres.");
        yield return new WaitForSeconds(0.1f);

        if (emplacement_N == null || emplacement_S == null || emplacement_E == null || emplacement_W == null)
        {
            Debug.LogError("Erreur: Un ou plusieurs emplacements de murs sont nulls.");
            yield break;
        }

        // Génération des murs/portes/fenêtres de chaque côté de la salle
        if (!isNorthWallInstantiated && emplacement_N.childCount == 0)
        {
            isNorthWallInstantiated = true;
            InstantiateWall(
                northDoor ? wallWithDoorPrefab : (isNorthExternal ? GetRandomExternalWall() : wallPrefab),
                emplacement_N.position, emplacement_N.rotation, isNorthExternal);
        }

        if (!isSouthWallInstantiated && emplacement_S.childCount == 0)
        {
            isSouthWallInstantiated = true;
            InstantiateWall(
                southDoor ? wallWithDoorPrefab : (isSouthExternal ? GetRandomExternalWall() : wallPrefab),
                emplacement_S.position, emplacement_S.rotation, isSouthExternal);
        }

        if (!isEastWallInstantiated && emplacement_E.childCount == 0)
        {
            isEastWallInstantiated = true;
            InstantiateWall(
                eastDoor ? wallWithDoorPrefab : (isEastExternal ? GetRandomExternalWall() : wallPrefab),
                emplacement_E.position, emplacement_E.rotation, isEastExternal);
        }

        if (!isWestWallInstantiated && emplacement_W.childCount == 0)
        {
            isWestWallInstantiated = true;
            InstantiateWall(
                westDoor ? wallWithDoorPrefab : (isWestExternal ? GetRandomExternalWall() : wallPrefab),
                emplacement_W.position, emplacement_W.rotation, isWestExternal);
        }
    }

    private GameObject GetRandomExternalWall()
    {
        return Random.value < 0.7f ? wallWithWindowPrefab : wallPrefab;
    }

    private void InstantiateWall(GameObject wallPrefab, Vector3 position, Quaternion rotation, bool isExternalWall)
    {
        // Si le mur est externe (avec fenêtre) et positionné au Nord ou au Sud, on applique une rotation de 180°
        if (isExternalWall && wallPrefab == wallWithWindowPrefab)
        {
            rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y + 180, rotation.eulerAngles.z);
        }

        Instantiate(wallPrefab, position, rotation, transform);
    }

    public bool IsWallInstantiated(int wallIndex)
    {
        switch (wallIndex)
        {
            case 0: return isNorthWallInstantiated;
            case 1: return isSouthWallInstantiated;
            case 2: return isEastWallInstantiated;
            case 3: return isWestWallInstantiated;
            default: return false;
        }
    }
}
