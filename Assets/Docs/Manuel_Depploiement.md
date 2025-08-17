# Manuel de déploiement

## Prérequis
- Windows 10/11 (x64)
- GPU compatible DirectX 11/12, driver à jour
- 8 Go RAM (16 Go recommandé)
- Espace disque libre: ~5 Go

## Installation (build Windows)
1. Télécharger l’archive `DeepNeurosis-vX.Y.Z-win64.zip` depuis la page des Releases.
2. (Optionnel mais recommandé) Vérifier l’empreinte SHA256 publiée avec un outil standard Windows (certutil) ou équivalent.
3. Décompresser l’archive dans un dossier de votre choix (ex: `C:\Games\DeepNeurosis`).
4. Lancer `DeepNeurosis.exe`.

## Paramètres en ligne de commande (Unity)
- `-screen-fullscreen 0` lancer en fenêtré
- `-screen-width 1920 -screen-height 1080` forcer la résolution
- `-force-d3d11` forcer DirectX 11 si souci avec DX12

## Sauvegardes (emplacement)
- Windows (Unity persistentDataPath):
  - `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/`
  - Sous-dossier: `FloorsData/`
  - Fichiers: `startRoom.json`, `floor_<index>.json`

Note: le dossier d’installation peut être supprimé sans affecter les sauvegardes qui résident sous LocalLow.

## Désinstallation
1. Supprimer le dossier d’installation.
2. (Optionnel) Supprimer les sauvegardes: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Deep Neurosis/`

## Rollback (revenir à une version antérieure)
1. Fermer le jeu.
2. Supprimer le dossier d’installation courant.
3. Réinstaller la version stable précédente (`vX.Y.Z`).
4. Les sauvegardes étant séparées dans LocalLow, elles seront conservées (pensez à les sauvegarder par prudence).
