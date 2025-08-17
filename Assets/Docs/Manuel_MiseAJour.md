# Manuel de mise à jour

## Schéma de version
- Version sémantique recommandée: `MAJOR.MINOR.PATCH`
- Le projet ne stocke pas de champ de version dans les sauvegardes JSON (StartRoomSaveData / FloorSaveData), donc privilégier la compatibilité ascendante.

## Mise à jour standard
1. Sauvegarder (copie) le dossier de sauvegardes:
   - `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/`
2. Télécharger `DeepNeurosis-vX.Y.Z-win64.zip`.
3. Vérifier l’empreinte SHA256 publiée.
4. Remplacer le dossier d’installation par le nouveau contenu.
5. Lancer `DeepNeurosis.exe`.

## Migration des sauvegardes
- Sauvegardes Unity JSON via `JsonUtility` (sans schéma/validation stricte).
- Fichiers:
  - `FloorsData/startRoom.json`
  - `FloorsData/floor_<index>.json`
- En l’absence de champ de version, tout changement de structure doit préserver les champs existants et fournir des valeurs par défaut côté code.
- Bonnes pratiques si le schéma évolue:
  - Ajouter des champs optionnels (nullable) plutôt que renommer/supprimer.
  - Fournir des valeurs par défaut à la désérialisation.
  - Prévoir un correctif de lecture si un champ est manquant.

## Rollback
1. Fermer le jeu.
2. Supprimer le dossier d’installation courant.
3. Réinstaller la version stable précédente `vX.Y.Z`.
4. Restaurer au besoin le dossier de sauvegardes mis de côté à l’étape de backup.
