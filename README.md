# TrainingManagement

Plateforme modulaire de gestion des formations construite avec .NET 10, ASP.NET Core MVC, Razor Views, Entity Framework Core et ASP.NET Core Identity.

## Prérequis

- SDK .NET 10 (`dotnet --version`)
- Git
- SQLite est embarqué pour le développement
- SQL Server est prévu pour la production

## Démarrage rapide

```powershell
dotnet tool restore
dotnet restore TrainingManagement.sln
dotnet build TrainingManagement.sln
dotnet run --project src/TrainingManagement.Web/TrainingManagement.Web.csproj --launch-profile http
```

Ouvrir ensuite `http://localhost:5012`.

Au premier démarrage en environnement `Development`, l’application crée la base SQLite, applique les migrations et exécute un seed idempotent.

## Comptes de développement

Administrateur :

- E-mail : `admin@training.local`
- Mot de passe : `Admin123!`

Formateur :

- E-mail : `trainer@training.local`
- Mot de passe : `Trainer123!`

Ces comptes sont exclusivement destinés au développement local. Aucun secret de production ne doit être ajouté au dépôt.

## Structure

```text
TrainingManagement.sln
src/
├── TrainingManagement.Domain/          # Entités, énumérations et règles métier
├── TrainingManagement.Application/     # Contrats, modèles applicatifs et résultats de services
├── TrainingManagement.Infrastructure/  # EF Core, Identity, migrations, services et seed
└── TrainingManagement.Web/             # MVC, Razor, ViewModels, Areas et assets
tests/
└── TrainingManagement.Tests/           # Tests automatisés
```

Les dépendances vont vers le cœur : `Application` dépend de `Domain`, `Infrastructure` dépend de `Application` et `Domain`, et `Web` compose l’application.

## Base de données et migrations

En développement, `appsettings.Development.json` utilise :

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

Migrations existantes :

- `InitialIdentity`
- `AddCategoriesAndTrainings`
- `AddModulesLessonsAndContents`
- `AddAssessmentsQuestionsAndAnswers`

En production, le fournisseur sélectionné est SQL Server. Les migrations ne sont jamais appliquées automatiquement : elles doivent faire partie d’une procédure de déploiement contrôlée.

## Configuration de production

Définir au minimum :

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<chaîne SQL Server>
SeedAdmin__Email=<adresse administrateur>
SeedAdmin__Password=<mot de passe fort fourni par le gestionnaire de secrets>
SeedAdmin__FirstName=<prénom>
SeedAdmin__LastName=<nom>
SeedTrainer__Email=<adresse du formateur de démonstration, Development uniquement>
SeedTrainer__Password=<mot de passe du formateur, Development uniquement>
SeedTrainer__FirstName=<prénom>
SeedTrainer__LastName=<nom>
```

Sur Railway, ces valeurs devront être fournies comme variables du service et jamais placées dans Git ou dans une image Docker.

## Authentification et autorisation

- Toute inscription publique crée uniquement un compte `Learner`.
- Les rôles centralisés sont `Admin`, `Trainer` et `Learner`.
- Les espaces Admin, Trainer et Learner exigent leur rôle respectif.
- Les mots de passe sont hachés et gérés par Identity.
- Tous les formulaires POST utilisent les jetons antiforgery.
- Après connexion, l’utilisateur est redirigé vers son espace selon son rôle.

## Catégories et formations

L’administration permet le CRUD des catégories, leur activation, la gestion des formations, leur association à une catégorie et à un formateur, ainsi que publication, dépublication, archivage, recherche, filtres, tri et pagination.

Relations principales :

- `Category` possède plusieurs `Training` avec suppression `Restrict`.
- Une formation possède une catégorie obligatoire et active.
- Une formation peut référencer un utilisateur actif ayant le rôle `Trainer`.
- La suppression éventuelle d’un formateur met `TrainerId` à `null`.
- Les slugs des catégories et formations sont normalisés et uniques.
- Les prix utilisent une précision SQL `decimal(18,2)`.

Routes :

```text
/Admin/Categories
/Admin/Trainings
/Trainings
/Trainings/{slug}
```

Une formation ne peut être publiée que si son titre et sa description sont renseignés, sa catégorie est active et sa durée est strictement positive. La publication renseigne `PublishedAt` en UTC. Une formation archivée ne peut plus être modifiée ou publiée et n’apparaît jamais dans le catalogue public.

## Modules, leçons et contenus pédagogiques

La hiérarchie pédagogique ajoutée à l’étape 3 est :

```text
Training
└── TrainingModule
    └── Lesson
        └── LessonContent
```

- Un module appartient à une formation et possède un slug unique dans cette formation.
- Une leçon appartient à un module et possède un slug unique dans ce module.
- Un contenu appartient à une leçon.
- L’ordre est unique dans chaque parent et peut être modifié avec les actions Monter/Descendre.
- Les relations utilisent `Restrict` afin d’empêcher les suppressions en cascade accidentelles.
- Un module ou une leçon contenant des enfants ne peut pas être supprimé physiquement ; l’archivage est privilégié.

### Types de contenu

`LessonContentType` accepte :

- `Text` : texte Razor encodé, sans interprétation HTML ;
- `Video` : URL HTTPS, avec intégration sûre pour YouTube et Vimeo ;
- `Audio` : URL HTTPS ;
- `Pdf` : URL HTTPS ;
- `ExternalLink` : URL HTTP ou HTTPS.

Les schémas dangereux tels que `javascript:` ou `data:` sont rejetés côté serveur. Un contenu non publié n’est jamais visible publiquement.

### Règles de publication et aperçu

- Un module ne peut être publié que si sa formation n’est pas archivée.
- Une leçon ne peut être publiée que si son module est publié, si elle contient au moins un contenu et si ses parents ne sont pas archivés.
- Une leçon marquée `IsPreview` est accessible aux visiteurs lorsqu’elle est publiée avec son module et sa formation.
- Une leçon non disponible en aperçu affiche : `Cette leçon est réservée aux apprenants inscrits.`
- L’administrateur dispose d’un aperçu qui inclut les éléments non publiés avec une indication visuelle.
- Le formateur ne voit en lecture seule que les formations qui lui sont affectées.

### Routes d’administration

Rôle `Admin` requis :

```text
/Admin/TrainingModules?trainingId={trainingId}
/Admin/TrainingModules/Create?trainingId={trainingId}
/Admin/TrainingModules/Details/{id}
/Admin/TrainingModules/Edit/{id}
/Admin/Lessons?moduleId={moduleId}
/Admin/Lessons/Create?moduleId={moduleId}
/Admin/Lessons/Details/{id}
/Admin/Lessons/Edit/{id}
/Admin/Lessons/Preview/{id}
/Admin/LessonContents?lessonId={lessonId}
/Admin/LessonContents/Create?lessonId={lessonId}
/Admin/LessonContents/Edit/{id}
```

Les actions de publication, dépublication, archivage, suppression et changement d’ordre sont exclusivement en POST avec antiforgery.

### Routes formateur

Rôle `Trainer` requis, accès en lecture seule :

```text
/Trainer/Trainings
/Trainer/Trainings/Details/{id}
```

Une tentative d’accès à une formation affectée à un autre formateur est refusée.

### Routes publiques

```text
/Trainings
/Trainings/{slug}
/Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons/{lessonSlug}
```

Le détail public d’une formation affiche uniquement le programme publié et non archivé. Le contenu complet d’une leçon est affiché seulement si elle est publiée et disponible en aperçu ; le futur contrôle des inscriptions remplacera cette règle.

## Données de démonstration

Le seed `Development`, idempotent, crée :

- quatre catégories ;
- trois formations couvrant brouillon/publiée et gratuit/payant ;
- le compte formateur et son affectation ;
- plusieurs modules publiés et non publiés ;
- plusieurs leçons, dont une leçon d’aperçu ;
- des contenus texte, vidéo, audio, PDF et lien externe, publiés et non publiés.

## Tests

```powershell
dotnet test TrainingManagement.sln
```

La suite couvre notamment Identity et les rôles, les catégories et formations, l’unicité des slugs et des ordres, les règles de publication et d’archivage, le déplacement des éléments, la validation des URL, les aperçus publics, l’isolation des formateurs, les autorisations et l’idempotence du seed.

## Exercices, questions et quiz

L’étape 4 complète la hiérarchie :

```text
Training → TrainingModule → Lesson → Assessment → Question → AnswerOption
```

Une leçon peut contenir plusieurs évaluations de type `Practice` (Entraînement), `Quiz` ou `Exam` (Examen). Les questions acceptent `SingleChoice`, `MultipleChoice`, `TrueFalse` et `ShortAnswer`.

Règles principales :

- le slug et l’ordre d’une évaluation sont uniques dans sa leçon ;
- l’ordre d’une question est unique dans son évaluation ;
- l’ordre d’un choix est unique dans sa question ;
- la note minimale est comprise entre 0 et 100 ;
- les limites de temps et de tentatives, lorsqu’elles existent, sont positives ;
- une évaluation exige une question publiée avant publication ;
- un examen exige au moins une question publiée avec des points ;
- les éléments archivés ne sont jamais publics ;
- `SingleChoice` exige au moins deux choix et exactement une bonne réponse ;
- `MultipleChoice` exige au moins deux choix et au moins une bonne réponse ;
- `TrueFalse` exige exactement les choix Vrai et Faux et une seule bonne réponse ;
- `ShortAnswer` interdit les choix et exige une réponse attendue ;
- la réponse attendue et les indicateurs de correction ne font pas partie des modèles publics.

Routes Admin :

```text
/Admin/Assessments?lessonId={id}
/Admin/Assessments/Details/{id}
/Admin/Assessments/Preview/{id}
/Admin/Questions?assessmentId={id}
/Admin/Questions/Details/{id}
/Admin/AnswerOptions?questionId={id}
```

Toutes les mutations utilisent POST, antiforgery et les services applicatifs.

Routes Trainer, en lecture seule et limitées aux formations affectées :

```text
/Trainer/Assessments?lessonId={id}
/Trainer/Assessments/Details/{id}
```

Route publique :

```text
/Trainings/{trainingSlug}/Modules/{moduleSlug}/Lessons/{lessonSlug}/Assessments/{assessmentSlug}
```

Le visiteur ne voit que les évaluations et questions publiées d’une leçon publique en aperçu. Les choix sont affichés sans bonne réponse et aucune soumission n’est disponible.

Le seed Development crée le quiz publié `Quiz d’introduction à ASP.NET Core`, ses quatre types de questions et leurs choix, ainsi que l’exercice non publié `Exercice pratique MVC`.

Migration de l’étape :

```text
AddAssessmentsQuestionsAndAnswers
```

## Limites actuelles

Cette étape n’implémente pas les tentatives, réponses des apprenants, notes individuelles, inscriptions, progression, certificats, paiements, avatar IA, Anam.ai, HeyGen, Docker ou déploiement Railway. L’évaluation publique est uniquement consultative.

## Prochaine étape recommandée

Ajouter les inscriptions, les tentatives et la progression des apprenants. Cette évolution permettra d’enregistrer les réponses, de calculer les notes et de remplacer le verrou actuel par un contrôle fondé sur une inscription active.
