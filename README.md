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

## Gestion des catégories et formations

L’étape 2 ajoute :

- le CRUD administratif des catégories avec activation, recherche et pagination ;
- la suppression d’une catégorie uniquement lorsqu’elle n’est liée à aucune formation ;
- la gestion des formations avec catégorie, formateur, niveau, prix et statut ;
- les actions de publication, dépublication et archivage ;
- la recherche, les filtres, le tri et la pagination administratifs ;
- un catalogue public contenant uniquement les formations publiées ;
- des pages publiques de détails sans système d’inscription à ce stade.

### Entités et relations

- `Category` possède plusieurs `Training`.
- Une catégorie utilisée est protégée par une relation `Restrict`.
- Une formation possède une catégorie obligatoire et active.
- Une formation peut référencer un utilisateur ayant le rôle `Trainer`.
- La suppression éventuelle d’un formateur met `TrainerId` à `null` sans supprimer ses formations.
- Les slugs des catégories et formations sont normalisés et uniques.
- Les prix utilisent une précision SQL `decimal(18,2)`.

### Routes

Administration, rôle `Admin` requis :

```text
/Admin/Categories
/Admin/Categories/Create
/Admin/Categories/Details/{id}
/Admin/Categories/Edit/{id}
/Admin/Trainings
/Admin/Trainings/Create
/Admin/Trainings/Details/{id}
/Admin/Trainings/Edit/{id}
```

Catalogue public :

```text
/Trainings
/Trainings/{slug}
```

Les changements d’état et suppressions sont exclusivement disponibles en `POST` avec validation antiforgery.

### Règles de publication

Une formation ne peut être publiée que si son titre et sa description sont renseignés, sa catégorie est active et sa durée est strictement positive. La publication renseigne `PublishedAt` en UTC. Une formation archivée ne peut plus être modifiée ou publiée et n’apparaît jamais dans le catalogue public.

Une formation gratuite reçoit toujours un prix égal à zéro. Le formateur sélectionné est revérifié côté serveur et doit être un utilisateur actif possédant le rôle `Trainer`.

### Données de démonstration

En `Development`, le seed idempotent crée :

- Développement Web ;
- Développement Mobile ;
- Intelligence Artificielle ;
- Bases de données ;
- trois formations couvrant les statuts brouillon/publiée et les tarifs gratuit/payant.

Compte formateur :

- E-mail : `trainer@training.local`
- Mot de passe : `Trainer123!`

Configuration correspondante :

```text
SeedTrainer__Email
SeedTrainer__Password
SeedTrainer__FirstName
SeedTrainer__LastName
```

### Migration de l’étape 2

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project src/TrainingManagement.Infrastructure/TrainingManagement.Infrastructure.csproj `
  --startup-project src/TrainingManagement.Web/TrainingManagement.Web.csproj
```

Migration : `AddCategoriesAndTrainings`.

## Prochaine étape recommandée

La prochaine étape peut ajouter les modules et cours d’une formation, avec leur ordre, contenu et règles d’accès, sans introduire encore les quiz, inscriptions, progression ou intégrations IA.
