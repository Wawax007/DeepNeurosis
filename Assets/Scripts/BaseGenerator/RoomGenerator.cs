using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoomGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject wallWithDoorPrefab;
    public GameObject wallWithWindowPrefab;

    private Transform emplacement_N, emplacement_S, emplacement_E, emplacement_W;
    public bool isNorthWallInstantiated = false,
                isSouthWallInstantiated = false,
                isEastWallInstantiated  = false,
                isWestWallInstantiated  = false;

    
    public GameObject extractionConsolePrefab;
    

    [Header("Événement de fin d'initialisation")]
    public UnityEvent onRoomReady;


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

            Debug.Log($"[RoomGenerator:{name}] Recherche anchors... N={emplacement_N != null}, S={emplacement_S != null}, E={emplacement_E != null}, W={emplacement_W != null}");

            waitTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (emplacement_N == null || emplacement_S == null || emplacement_E == null || emplacement_W == null)
        {
            Debug.LogError($"[RoomGenerator:{name}] ERREUR - Un ou plusieurs emplacements de murs introuvables. Initialisation annulée.");
            yield break;
        }

        Debug.Log($"[RoomGenerator:{name}] Tous les emplacements trouvés. Initialisation OK.");
        onRoomReady?.Invoke();

        // ➕ Ajout ici pour placement automatique des props
        var placer = GetComponent<PropPlacer>();
        if (placer != null)
        {
            Debug.Log($"[RoomGenerator:{name}] Placement des props déclenché.");
            placer.PlaceClueProps();
        }
    }

    /// <summary>
    /// On reçoit toutes les infos pour savoir si on doit instancier un mur,
    /// et si oui, s'il y a une porte ou non, etc.
    /// </summary>
    public IEnumerator SetupRoomCoroutine(
        bool northDoor, bool southDoor, bool eastDoor, bool westDoor,
        bool isNorthExternal, bool isSouthExternal, bool isEastExternal, bool isWestExternal,
        bool placeNorthWall, bool placeSouthWall, bool placeEastWall, bool placeWestWall
    )
    {
        Debug.Log($"Configuration de la salle {this.name} avec portes et fenêtres...");
        // Petite attente pour être sûr que tout est bien initialisé
        yield return new WaitForSeconds(0.1f);

        if (emplacement_N == null || emplacement_S == null || emplacement_E == null || emplacement_W == null)
        {
            Debug.LogError("Erreur: Emplacements de murs introuvables pendant SetupRoomCoroutine.");
            yield break;
        }

        // ------------------ NORD ------------------
        if (placeNorthWall && !isNorthWallInstantiated && emplacement_N.childCount == 0)
        {
            isNorthWallInstantiated = true;
            if (northDoor)
            {
                // Porte
                InstantiateWall(wallWithDoorPrefab, emplacement_N.position, emplacement_N.rotation, false, emplacement_N);
            }
            else
            {
                // Mur externe ou pas
                if (isNorthExternal)
                {
                    InstantiateWall(GetRandomExternalWall(), emplacement_N.position, emplacement_N.rotation, true, emplacement_N);
                }
                else
                {
                    InstantiateWall(wallPrefab, emplacement_N.position, emplacement_N.rotation, false, emplacement_N);
                }
            }
        }

        // ------------------ SUD -------------------
        if (placeSouthWall && !isSouthWallInstantiated && emplacement_S.childCount == 0)
        {
            isSouthWallInstantiated = true;
            if (southDoor)
            {
                InstantiateWall(wallWithDoorPrefab, emplacement_S.position, emplacement_S.rotation, false, emplacement_S);
            }
            else
            {
                if (isSouthExternal)
                {
                    InstantiateWall(GetRandomExternalWall(), emplacement_S.position, emplacement_S.rotation, true, emplacement_S);
                }
                else
                {
                    InstantiateWall(wallPrefab, emplacement_S.position, emplacement_S.rotation, false, emplacement_S);
                }
            }
        }

        // ------------------ EST -------------------
        if (placeEastWall && !isEastWallInstantiated && emplacement_E.childCount == 0)
        {
            isEastWallInstantiated = true;
            if (eastDoor)
            {
                InstantiateWall(wallWithDoorPrefab, emplacement_E.position, emplacement_E.rotation, false, emplacement_E);
            }
            else
            {
                if (isEastExternal)
                {
                    InstantiateWall(GetRandomExternalWall(), emplacement_E.position, emplacement_E.rotation, true, emplacement_E);
                }
                else
                {
                    InstantiateWall(wallPrefab, emplacement_E.position, emplacement_E.rotation, false, emplacement_E);
                }
            }
        }

        // ------------------ OUEST -----------------
        if (placeWestWall && !isWestWallInstantiated && emplacement_W.childCount == 0)
        {
            isWestWallInstantiated = true;
            if (westDoor)
            {
                InstantiateWall(wallWithDoorPrefab, emplacement_W.position, emplacement_W.rotation, false, emplacement_W);
            }
            else
            {
                if (isWestExternal)
                {
                    InstantiateWall(GetRandomExternalWall(), emplacement_W.position, emplacement_W.rotation, true, emplacement_W);
                }
                else
                {
                    InstantiateWall(wallPrefab, emplacement_W.position, emplacement_W.rotation, false, emplacement_W);
                }
            }
        }
    }

    /// <summary>
    /// Choix aléatoire : parfois on met un mur fenêtre (70%) ou un mur plein (30%)
    /// </summary>
    private GameObject GetRandomExternalWall()
    {
        return Random.value < 0.7f ? wallWithWindowPrefab : wallPrefab;
    }

    private void InstantiateWall(GameObject chosenWallPrefab, Vector3 position, Quaternion rotation, bool isExternalWall, Transform emplacement)
    {
        // Exemple : si c'est un mur plein (pas fenêtre ni porte) côté N ou E, on applique une rotation Y de 180
        if (chosenWallPrefab == this.wallPrefab && (emplacement == emplacement_N || emplacement == emplacement_E))
        {
            rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y + 180, rotation.eulerAngles.z);
        }

        // Si c'est un mur fenêtre (externe) et qu'on est côté S ou W, on inverse également
        if (isExternalWall && chosenWallPrefab == wallWithWindowPrefab && (emplacement == emplacement_S || emplacement == emplacement_W))
        {
            rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y + 180, rotation.eulerAngles.z);
        }

        // Instanciation
        Instantiate(chosenWallPrefab, position, rotation, transform);
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
