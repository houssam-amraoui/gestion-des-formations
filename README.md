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

Apprenant :

- E-mail : `learner@training.local`
- Mot de passe : `Learner123!`

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
- `AddEnrollmentsAttemptsAndProgress`

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

## Limites de l'étape 4

Cette étape n’implémente pas les tentatives, réponses des apprenants, notes individuelles, inscriptions, progression, certificats, paiements, avatar IA, Anam.ai, HeyGen, Docker ou déploiement Railway. L’évaluation publique est uniquement consultative.

## Suite prévue après l'étape 4

Ajouter les inscriptions, les tentatives et la progression des apprenants. Cette évolution permettra d’enregistrer les réponses, de calculer les notes et de remplacer le verrou actuel par un contrôle fondé sur une inscription active.

## Inscriptions, tentatives et progression

L’étape 5 ajoute la chaîne de suivi apprenant :

```text
ApplicationUser
├── Enrollment → Training
│   └── LessonProgress → Lesson
└── AssessmentAttempt → Assessment
    └── AttemptQuestion
        └── LearnerAnswer
```

### Inscriptions et accès

- Une inscription est unique par apprenant et formation.
- Une formation gratuite publiée peut être rejointe directement par un apprenant.
- Une formation payante nécessite une création ou une validation par un administrateur.
- Seules les inscriptions `Active` ou `Completed` ouvrent le contenu privé.
- Une inscription peut passer par `Pending`, `Active`, `Suspended`, `Cancelled` et `Completed`.
- L’annulation et la suspension retirent immédiatement l’accès sans supprimer l’historique.

Routes Admin :

```text
/Admin/Enrollments
/Admin/Enrollments/Create
/Admin/Enrollments/Details/{id}
/Admin/Attempts
/Admin/Attempts/Details/{id}
```

Routes Learner :

```text
/Learner/Dashboard
/Learner/Trainings
/Learner/Trainings/Details/{id}
/Learner/Lessons/Details/{id}
/Learner/Assessments/Details/{id}
/Learner/Attempts/Details/{id}
/Learner/Attempts/Result/{id}
/Learner/Attempts/History
```

Routes Trainer, en lecture seule et limitées aux formations affectées :

```text
/Trainer/LearnerProgress
/Trainer/AssessmentResults
/Trainer/AssessmentResults/Details/{id}
```

L’auto-inscription gratuite utilise `POST /Trainings/{id}/Enroll`.

### Progression des leçons

Le premier accès crée ou met à jour un `LessonProgress`. Une leçon peut être marquée terminée depuis l’espace Learner. Le pourcentage d’une inscription est recalculé à partir des leçons publiées et non archivées ; il reste compris entre 0 et 100 et termine automatiquement l’inscription à 100 %.

### Tentatives et notation

- Une seule tentative en cours est reprise pour un même apprenant et une même évaluation.
- Le nombre maximal de tentatives et la limite de temps sont vérifiés côté serveur.
- L’ordre éventuellement mélangé, les énoncés, les choix, les points et les réponses de référence sont copiés dans des snapshots immuables.
- Les réponses peuvent être enregistrées avant soumission.
- Une soumission est définitive et protégée contre les doubles envois.
- Une tentative expirée est clôturée et notée avec les réponses déjà enregistrées.
- `SingleChoice`, `MultipleChoice` et `TrueFalse` exigent une correspondance exacte.
- `ShortAnswer` compare la réponse après normalisation des espaces et sans tenir compte de la casse.
- Aucun crédit partiel n’est attribué dans cette version.
- Les corrections sont visibles par l’apprenant uniquement après clôture et si l’évaluation les autorise.
- Les résultats ne sont accessibles qu’à leur propriétaire, à un administrateur ou au formateur affecté.

Les opérations sensibles utilisent des transactions et les tentatives possèdent un jeton de concurrence.

### Données de démonstration

Le seed Development crée de manière idempotente :

- `learner@training.local` avec le rôle `Learner` ;
- une inscription active à `ASP.NET Core MVC — Fondamentaux` ;
- des accès et progressions de leçons pour tester le tableau de bord ;
- les évaluations et questions de démonstration décrites à l’étape 4.

Variables de configuration :

```text
SeedLearner__Email
SeedLearner__Password
SeedLearner__FirstName
SeedLearner__LastName
```

Migration de l’étape :

```text
AddEnrollmentsAttemptsAndProgress
```

## Limites actuelles

Cette étape ne couvre pas encore les certificats, classements, paiements, notifications, réinitialisation administrative d’une tentative, correction manuelle avancée des réponses courtes, avatar IA, Anam.ai, HeyGen, Docker ou déploiement Railway.

## Prochaine étape recommandée

Ajouter les certificats et les notifications, puis enrichir le suivi avec une correction manuelle des réponses courtes et des rapports exportables. Les paiements devront être traités séparément avec un fournisseur et des webhooks idempotents.

## Certificats, complétion et statistiques

L’étape 6 ajoute une règle de complétion centralisée, l’émission de certificats PDF vérifiables,
des tableaux de bord analytiques par rôle et des exports CSV.

### Règles de complétion

Chaque formation peut configurer :

- `RequireAllLessonsCompleted` : toutes les leçons publiées et non archivées doivent être terminées ;
- `RequireAllMandatoryAssessmentsPassed` : toutes les évaluations obligatoires, publiées et non archivées doivent être réussies ;
- `MinimumAverageScore` : moyenne minimale optionnelle, comprise entre 0 et 100 ;
- `CertificateEnabled` : autorise l’émission automatique du certificat ;
- `CertificateValidityMonths` : durée de validité optionnelle, strictement positive ;
- `CertificateTemplateName` : nom du modèle conservé pour une évolution future.

Une évaluation peut être marquée `IsMandatory`. La meilleure tentative terminée est utilisée pour
chaque évaluation applicable. Lorsqu’aucune évaluation ne s’applique, la contrainte de moyenne est
considérée satisfaite. La finalisation est idempotente : elle place l’inscription à 100 %, renseigne
`CompletedAt`, puis émet au maximum un certificat par inscription.

### Certificats

Un certificat conserve des instantanés du nom de l’apprenant, du titre de la formation et du
formateur. Son numéro et son code de vérification sont uniques et générés avec une source
cryptographiquement sûre. Les états sont `Active`, `Revoked` et `Expired`.

Les PDF sont générés côté serveur avec PDFsharp et QRCoder. Ils incluent le numéro, les informations
de formation, la date d’émission et un QR code pointant vers la vérification publique. Les fichiers
sont placés dans un stockage privé hors de `wwwroot` (`App_Data/Certificates` par défaut) et ne sont
servis qu’après un contrôle d’autorisation. La révocation exige un motif ; la réactivation conserve
l’historique de révocation disponible.

Routes publiques :

```text
GET  /Certificates/Verify
POST /Certificates/Verify
GET  /Certificates/Verify/{verificationCode}
```

La page publique n’expose ni email, ni identifiant utilisateur, ni chemin de fichier. Elle porte
également une directive `noindex, nofollow`.

Routes Admin :

```text
/Admin/Certificates
/Admin/Certificates/Details/{id}
/Admin/Certificates/Download/{id}
/Admin/Certificates/Generate/{enrollmentId}
/Admin/Certificates/Regenerate/{id}
/Admin/Certificates/Revoke/{id}
/Admin/Certificates/Reactivate/{id}
/Admin/Analytics
/Admin/Analytics/Trainings/{trainingId}
```

Routes Learner, limitées aux certificats du compte connecté :

```text
/Learner/Certificates
/Learner/Certificates/Details/{id}
/Learner/Certificates/Download/{id}
/Learner/Statistics
```

Routes Trainer, en lecture seule et limitées aux formations affectées :

```text
/Trainer/Certificates
/Trainer/Certificates/Details/{id}
/Trainer/Analytics
/Trainer/Analytics/Trainings/{trainingId}
```

### Statistiques et exports

Les périodes disponibles sont les 7, 30 ou 90 derniers jours, l’année en cours et une période
personnalisée de 366 jours maximum. Les indicateurs couvrent notamment les inscriptions, la
progression moyenne, la complétion, le taux de réussite aux évaluations, les certificats, les
formations populaires et les points d’abandon par leçon.

Définitions principales :

- taux de complétion = inscriptions terminées / inscriptions de la population sélectionnée ;
- progression moyenne = moyenne de `ProgressPercentage` des inscriptions sélectionnées ;
- taux de réussite = tentatives terminées et réussies / tentatives terminées ;
- abandon d’une leçon = progressions commencées mais non terminées / progressions commencées.

Exports Admin :

```text
/Admin/Exports/Enrollments
/Admin/Exports/Progress
/Admin/Exports/AssessmentResults
/Admin/Exports/Certificates
/Admin/Exports/TrainingAnalytics/{id}
```

Exports Trainer, automatiquement limités à ses formations :

```text
/Trainer/Exports/Progress
/Trainer/Exports/AssessmentResults
/Trainer/Exports/TrainingAnalytics/{id}
```

Les CSV sont encodés en UTF-8 avec BOM, utilisent le point-virgule comme séparateur, échappent les
guillemets et neutralisent les cellules commençant par `=`, `+`, `-` ou `@` afin de réduire les
risques d’injection de formule.

### Configuration

Exemple de configuration :

```json
{
  "Application": {
    "Name": "TrainingManagement",
    "PublicBaseUrl": "https://example.com"
  },
  "CertificateStorage": {
    "BasePath": "App_Data/Certificates"
  }
}
```

En production, fournir au minimum :

```text
Application__PublicBaseUrl
CertificateStorage__BasePath
ConnectionStrings__DefaultConnection
```

Le dossier de certificats et la base SQLite de développement sont ignorés par Git. Pour une
production multi-instance, remplacer le stockage local par un stockage objet privé tout en
conservant `ICertificateStorageService`.

Packages ajoutés :

```text
PDFsharp 6.2.4
QRCoder 1.8.0
```

Migration de l’étape :

```text
AddCertificatesAndCompletionRules
RemoveNonPortableTrainingCheckConstraints
```

La seconde migration additive retire deux contraintes SQL générées initialement qui ne sont pas
portables lors d’une reconstruction de table SQLite. Les règles correspondantes restent appliquées
par les ViewModels et les services métier.

Commandes :

```powershell
dotnet ef migrations list --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web
dotnet ef database update --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web
```

### Données de démonstration

En Development, le seed idempotent termine l’inscription de
`learner@training.local` à `ASP.NET Core MVC — Fondamentaux`, ajoute une tentative réussie à
l’évaluation obligatoire si nécessaire, puis génère un certificat actif et son PDF. Le numéro et
le code de vérification sont volontairement générés à chaque nouvelle base et ne sont pas fixés
dans le dépôt.

### Limites actuelles et prochaine étape

Cette étape ne fournit pas de modèles PDF personnalisables par l’administrateur, de stockage objet,
d’envoi d’email, de notifications, de paiement, de certificat signé cryptographiquement, de
classement, ni d’intégration IA. Les statistiques sont calculées à la demande et conviennent au
volume actuel ; des agrégats persistés pourront être ajoutés à grande échelle.

La prochaine étape recommandée est d’ajouter les notifications et l’envoi contrôlé des certificats,
puis d’introduire un stockage objet privé et des tâches en arrière-plan. Les paiements et
intégrations IA doivent rester des modules séparés.

## Formateur IA conversationnel

L’étape 7 ajoute un module de tutorat lié aux leçons, conçu autour d’abstractions indépendantes des
fournisseurs. Le mode `Mock`, activé uniquement en Development, fonctionne sans réseau et fournit
des réponses déterministes pour développer et tester le parcours complet. Un adaptateur Anam limité
à la création sécurisée d’un jeton de session avatar est également présent ; sa clé reste côté
serveur et n’est jamais enregistrée en base, journalisée ou rendue dans une vue.

La chaîne fonctionnelle est :

```text
Training → AiTrainerProfile
Lesson + Enrollment + ApplicationUser → AiConversationSession
AiConversationSession → AiConversationMessage
AiConversationSession → AiProviderUsageRecord
ApplicationUser → AiUserConsent
```

Un seul profil IA est autorisé par formation. Une session conserve un identifiant `Guid`, une durée
et un nombre de messages bornés, un statut (`Starting`, `Active`, `Completed`, `Expired`, `Failed`
ou `Cancelled`) et un historique ordonné. Les lectures utilisent des projections sans suivi et les
relations vers les données pédagogiques sont restrictives ; seuls les messages et métriques
techniques appartenant à une session suivent sa suppression.

### Sécurité et confidentialité

- l’apprenant doit avoir une inscription active ou terminée pour une leçon privée ;
- le formateur est limité aux formations qui lui sont affectées ;
- les routes Admin, Trainer et Learner exigent leurs rôles respectifs ;
- toutes les mutations utilisent POST et antiforgery ;
- les démarrages, messages, audios et actualisations ont des politiques de rate limiting dédiées ;
- le texte est limité, les demandes de secrets et injections de prompt évidentes sont bloquées ;
- le contexte de leçon est délimité comme source non fiable et tronqué à une taille configurable ;
- les audios acceptés sont WebM, WAV ou MP3, avec taille maximale contrôlée ;
- un consentement versionné et révocable est requis avant tout traitement audio Learner ;
- aucun fichier audio temporaire n’est créé : le flux est transmis directement au fournisseur ;
- l’historique peut être anonymisé après la durée de conservation configurée ;
- les erreurs externes sont transformées en messages sûrs, sans exposer de clé ni réponse brute.

Le mode texte reste disponible si l’avatar ou la synthèse vocale échoue. En Production, l’IA est
désactivée par défaut et une validation de configuration interdit d’activer silencieusement le
fournisseur `Mock`.

### Fournisseurs

Les contrats disponibles sont `IAiProvider`, `IAiAvatarProvider`,
`IAiLanguageModelProvider`, `IAiSpeechToTextProvider` et `IAiTextToSpeechProvider`.
`IAiProviderFactory` centralise leur sélection. L’adaptateur Anam utilise l’API serveur officielle
`POST /v1/auth/session-token` avec un Bearer token, un délai d’expiration HTTP et un jeton client
éphémère. La conversation textuelle d’un déploiement réel nécessite de configurer séparément un
fournisseur de modèle de langage ; aucun faux appel Anam n’est simulé en production.

Configuration Development :

```json
"AiTrainer": {
  "Enabled": true,
  "Provider": "Mock",
  "LanguageModelProvider": "Mock",
  "SpeechToTextProvider": "Mock",
  "TextToSpeechProvider": "Mock",
  "MaximumMessagesPerSession": 20,
  "MaximumSessionDurationMinutes": 30,
  "MaximumSessionsPerUserPerDay": 10,
  "MaximumAudioBytes": 5000000,
  "ConversationRetentionDays": 365,
  "ConsentPolicyVersion": "2026-01"
}
```

Variables réservées à une configuration réelle :

```text
AiTrainer__Enabled=true
AiTrainer__Provider=Anam
AiTrainer__AvatarProvider=Anam
AiTrainer__LanguageModelProvider=<fournisseur configuré>
Anam__ApiKey=<secret fourni par le gestionnaire de secrets>
Anam__LlmId=<identifiant du modèle Anam>
Anam__BaseAddress=https://api.anam.ai/v1/
```

Ne placez jamais `Anam__ApiKey` dans un fichier versionné. Les sections `HeyGen`,
`LanguageModel`, `SpeechToText` et `TextToSpeech` servent de points d’extension ; aucun appel réel
ne leur est envoyé dans cette étape.

### Routes

Admin :

```text
/Admin/AiTrainerProfiles
/Admin/AiTrainerProfiles/Details/{id}
/Admin/AiTrainerProfiles/Create?trainingId={id}
/Admin/AiTrainerProfiles/Edit/{id}
/Admin/AiTrainerProfiles/Test/{id}
/Admin/AiSessions
/Admin/AiSessions/Details/{id}
```

Trainer, limité à ses formations :

```text
/Trainer/AiTrainer
/Trainer/AiTrainer/Session/{id}
/Trainer/AiTrainer/History
```

Learner, limité à son propre historique et à ses inscriptions :

```text
/Learner/AiTrainer/Session/{id}
/Learner/AiTrainer/History
```

Le démarrage d’une session se fait par POST depuis une leçon accessible. Les messages, audios,
consentements, fins et annulations de session sont également des actions POST.

### Seed, migration et tests

Le seed Development crée de manière idempotente le profil `Coach ASP.NET Core`, lié à la formation
`ASP.NET Core MVC — Fondamentaux`, en mode Mock, avec texte et transcription Mock activés. Il ne
réalise aucun appel HTTP.

Migration :

```text
AddAiTrainerIntegration
```

Commandes :

```powershell
dotnet ef migrations list --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web
dotnet ef database update --project src/TrainingManagement.Infrastructure --startup-project src/TrainingManagement.Web -- --environment Development
dotnet test TrainingManagement.sln
```

Les tests couvrent le domaine, les limites, le consentement, la modération, les formats audio, le
Mock, les index et suppressions EF, les rôles, antiforgery, politiques de débit et validations Web.

### Limites et étape suivante

Cette version ne diffuse pas encore un avatar Anam dans le navigateur, ne stocke pas de média,
n’effectue pas d’appel à un LLM réel, ne produit pas de synthèse vocale Mock, et ne fournit ni
WebSocket ni streaming de tokens. Les coûts restent des estimations optionnelles.

L’étape suivante recommandée est d’ajouter un fournisseur de langage réel derrière
`IAiLanguageModelProvider`, une tâche d’arrière-plan pour expiration/rétention, puis l’intégration
WebRTC du SDK avatar avec renouvellement contrôlé des jetons éphémères. Les notifications,
paiements et fonctions IA génératives de contenu doivent rester des modules distincts.
