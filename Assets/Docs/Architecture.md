# Architecture logicielle

## Modules principaux (confirmés dans le code)
- Génération/chargement d’étage
  - BaseGenerator: génère la topologie et les props procéduraux; exporte/importe FloorSaveData.
  - InternalPartitionGenerator: calcule des sous-espaces et points de patrouille.
  - PropPlacer: place les props; PropPlacedInstance et PropBlocker pour contraintes.
  - FloorManager: orchestre le changement d’étage, sauvegardes JSON, activation StartRoom/ExtractionPod.
- Ascenseur
  - ElevatorController: fermeture portes, déplacements, sons, appelle FloorManager.GoToFloor.
  - ElevatorPanel/PhysicalFloorButton: boutons physiques.
  - ElevatorPropTracker: garde en scène les objets présents dans l’ascenseur lors des transitions.
- Console d’extraction (puzzle)
  - ExtractionConsoleLogic: sockets Navigation/Security, 3 sélecteurs (Priority/Protocol/Destination), diode, sons.
  - ModuleSocket/ModuleItem/ModuleType: insertion d’objets-modules et ancrage.
  - RotarySelector: incrémente les options, refuse si modules absents ou séquence déjà validée.
  - ValidateButton: déclenche la validation; en cas de succès, ExtractionPodActivator.EnableCapsule().
- IA du mob
  - MobAI: états Patrolling/Suspicious/Chasing/Searching/Attacking; ouvre les portes bloquantes (DoorInteract, NavMeshObstacle carving), pas de téléportation.
- Interactions joueur
  - PlayerInteraction: raycast E pour Interact/ramasser/lâcher; rotation d’objet (R maintenu), zoom molette; surlignage visuel.
  - IInteractable, DoorInteract, LockerDoor, ExtractionInteractable, etc.
- Données/énigmes
  - EnigmaDefinition/EnigmaData (ScriptableObjects): étapes par étage; RunEnigmaManager choisit l’énigme pour la run.

## Sauvegardes (JSON)
- StartRoom: StartRoomSaveData (extincteur pos/rot, fusible, verre cassé + fragments).
- Étages: FloorSaveData
  - floorIndex, rooms (RoomExportData), generatedProps, placedClueProps, hasMob,
  - consoleState: { securityInserted, navigationInserted, selectedPriority, selectedProtocol, selectedDestination, consoleValidated }.
- Emplacement Windows (Unity persistentDataPath):
  - %USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/
  - Dossier d’étage: FloorsData/ avec floor_<index>.json + startRoom.json

## Patterns et principes
- Data-driven via ScriptableObjects (énigmes, indices).
- Séparation orchestration (FloorManager/ElevatorController) vs génération (BaseGenerator).
- Pas d’EventBus global; communication par références/UnityEvents ciblés.
- Préservation d’objets critiques via PropTransferManager lors des unloads.
