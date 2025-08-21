# DeepNeurosis

## Intégration Continue (CI)

Un workflow GitHub Actions exécute automatiquement les tests Unity:
- Emplacement: `.github/workflows/ci.yml`
- Version Unity: 2022.3.37f1
- Tests: EditMode et PlayMode via `game-ci/unity-test-runner`
- Artefacts: `TestResults/` (logs et rapports)

## Déploiement Continu (CD)

- Build Windows x64 automatisé via `game-ci/unity-builder`.
- Publication automatique d’une Release GitHub lors d’un tag `vX.Y.Z` avec l’archive `DeepNeurosis_Windows.zip` attachée.
