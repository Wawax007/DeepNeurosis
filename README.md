# DeepNeurosis

## Intégration Continue (CI)

Un workflow GitHub Actions exécute automatiquement les tests Unity:
- Emplacement: `.github/workflows/ci.yml`
- Version Unity: 2022.3.37f1 (détectée dans `ProjectSettings/ProjectVersion.txt`)
- Tests: EditMode et PlayMode via `game-ci/unity-test-runner`
- Artefacts: `TestResults/` (logs et rapports)

Prérequis: ajouter le secret de dépôt `UNITY_LICENSE` (contenu de licence Unity pour CI). Suivre la procédure d’activation de licence de game-ci, puis coller la licence dans le secret.

Notes:
- Le fichier `Assets/Docs/ci.yml` est uniquement documentaire et redirige vers le workflow réel.
- Le cache de `Library/` est activé pour accélérer les exécutions.