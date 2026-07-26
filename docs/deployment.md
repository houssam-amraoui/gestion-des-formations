# Déploiement Production

## Prérequis

.NET SDK 10.0.302, Docker 28+, un SQL Server 2022/Azure SQL accessible, un domaine HTTPS et un
volume persistant. Aucun déploiement ne doit commencer avant un build Release et une sauvegarde.

```powershell
dotnet restore TrainingManagement.sln
dotnet build TrainingManagement.sln -c Release --no-restore
dotnet test TrainingManagement.sln -c Release --no-build
docker build -t training-management:local .
```

## Exécution locale proche de Production

Copier `.env.example` vers `.env`, remplacer uniquement le mot de passe fictif, puis :

```powershell
docker compose -f compose.production-like.yml up --build
```

Compose démarre SQL Server, attend sa santé, exécute le conteneur de migration une fois, puis
l’application. `http://localhost:8080/health/live` vérifie le processus et `/health/ready` vérifie
SQL Server, le stockage des certificats et les clés Data Protection.

## Déploiement contrôlé

1. Créer et tester la sauvegarde SQL Server.
2. Monter un volume sur `/app/data`.
3. Configurer les variables de `production-configuration.md`.
4. Exécuter une seule instance de `dotnet /app/migrations/TrainingManagement.Migrations.dll`.
5. Démarrer `dotnet TrainingManagement.Web.dll`.
6. Vérifier health, headers, accueil, connexion, rôles, certificat et IA.
7. Désactiver `SeedAdmin__Enabled` après amorçage.

L’application web n’applique jamais les migrations en Production. Les dossiers persistants sont
`/app/data/keys`, `/app/data/certificates` et `/app/data/temp`. L’image fonctionne avec
l’utilisateur Linux `app` et accepte `PORT`.

## Checklist

Avant : tests, image, secrets, SQL Server, sauvegarde, script de migration revu, domaine HTTPS,
volume, clés, stockage, CSP, logs, IA et seed Admin.

Après : liveness/readiness, accueil, catalogue, inscription, connexion, quiz, rôles, PDF,
téléchargement autorisé, IA, logs sans secret, redémarrage, persistance et sauvegarde planifiée.
