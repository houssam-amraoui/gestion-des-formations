# TrainingManagement

Fondations d’une plateforme de gestion des formations construite avec ASP.NET Core MVC, Razor Views, Entity Framework Core et ASP.NET Core Identity.

## Prérequis

- SDK .NET 10 (`dotnet --version`)
- Git
- Aucun serveur de base de données n’est requis en développement : SQLite est embarqué.
- SQL Server sera requis pour l’environnement Production.

## Démarrage rapide

```powershell
dotnet tool restore
dotnet restore TrainingManagement.sln
dotnet build TrainingManagement.sln
dotnet run --project src/TrainingManagement.Web/TrainingManagement.Web.csproj --launch-profile http
```

Ouvrir ensuite `http://localhost:5012`.

Au premier démarrage en environnement `Development`, l’application crée la base SQLite, applique les migrations et initialise les rôles et le compte administrateur.

## Compte administrateur de développement

- E-mail : `admin@training.local`
- Mot de passe : `Admin123!`

Ce compte est exclusivement destiné au développement local. Aucun mot de passe de production ne doit être ajouté au dépôt.

## Structure

```text
TrainingManagement.sln
src/
├── TrainingManagement.Domain/          # Entités, constantes et règles métier autonomes
├── TrainingManagement.Application/     # Contrats et logique applicative
├── TrainingManagement.Infrastructure/  # EF Core, Identity, migrations et seed
└── TrainingManagement.Web/             # MVC, Razor, ViewModels, Areas et assets
tests/
└── TrainingManagement.Tests/           # Tests automatisés
```

Les dépendances vont vers le cœur : `Application` dépend de `Domain`, `Infrastructure` dépend de `Application` et `Domain`, et `Web` compose l’application.

## Base de données et migrations

En développement, `appsettings.Development.json` définit :

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=training-management.db"
}
```

La base locale `training-management.db` est ignorée par Git. Les migrations sont appliquées automatiquement uniquement en `Development`.

Commandes EF utiles :

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add NomMigration `
  --project src/TrainingManagement.Infrastructure/TrainingManagement.Infrastructure.csproj `
  --startup-project src/TrainingManagement.Web/TrainingManagement.Web.csproj `
  --output-dir Persistence/Migrations

dotnet tool run dotnet-ef database update `
  --project src/TrainingManagement.Infrastructure/TrainingManagement.Infrastructure.csproj `
  --startup-project src/TrainingManagement.Web/TrainingManagement.Web.csproj

dotnet tool run dotnet-ef migrations list `
  --project src/TrainingManagement.Infrastructure/TrainingManagement.Infrastructure.csproj `
  --startup-project src/TrainingManagement.Web/TrainingManagement.Web.csproj
```

En production, le fournisseur sélectionné est SQL Server. Les migrations ne sont **jamais** appliquées automatiquement : elles doivent faire partie d’une procédure de déploiement contrôlée.

## Configuration de production

Définir au minimum les variables d’environnement suivantes :

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<chaîne SQL Server>
SeedAdmin__Email=<adresse administrateur>
SeedAdmin__Password=<mot de passe fort fourni par le gestionnaire de secrets>
SeedAdmin__FirstName=<prénom>
SeedAdmin__LastName=<nom>
```

ASP.NET Core transforme automatiquement les doubles underscores (`__`) en séparateurs de configuration. Sur Railway, ajouter ces valeurs dans les variables du service ; ne pas les placer dans une image Docker ou dans Git.

## Authentification et autorisation

- Toute inscription publique crée uniquement un compte `Learner`.
- Les rôles centralisés sont `Admin`, `Trainer` et `Learner`.
- Les espaces `/Admin/Dashboard`, `/Trainer/Dashboard` et `/Learner/Dashboard` exigent leur rôle respectif.
- Les mots de passe sont hachés et gérés par Identity.
- Les formulaires POST utilisent les jetons antiforgery.
- Après connexion, l’utilisateur est redirigé vers son Area selon son rôle.

## Tests

```powershell
dotnet test TrainingManagement.sln
```

La suite couvre les constantes de rôles, la redirection par rôle, les validations d’inscription et l’attribution automatique du rôle `Learner`.
