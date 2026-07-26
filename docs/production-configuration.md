# Configuration Production

| Variable | Requise | Exemple fictif | Secret | Description |
|---|---:|---|---:|---|
| `ASPNETCORE_ENVIRONMENT` | oui | `Production` | non | Sélectionne SQL Server et les protections Production |
| `PORT` | plateforme | `8080` | non | Port injecté par Railway |
| `ASPNETCORE_URLS` | facultatif | `http://0.0.0.0:8080` | non | Repli si `PORT` est absent |
| `Application__Name` | oui | `Training Academy` | non | Nom public |
| `Application__PublicBaseUrl` | oui | `https://academy.example` | non | HTTPS absolu sans slash final |
| `ConnectionStrings__DefaultConnection` | oui | `Server=db.example;Database=Training;...` | oui | Connexion SQL Server |
| `SeedAdmin__Enabled` | oui | `false` | non | Désactivé par défaut |
| `SeedAdmin__Email` | si activé | `admin@example.test` | donnée privée | Administrateur initial |
| `SeedAdmin__Password` | si activé | valeur du coffre | oui | 12 caractères, casse, chiffre et symbole |
| `SeedAdmin__FirstName`, `SeedAdmin__LastName` | si activé | `Initial`, `Admin` | donnée privée | Identité initiale |
| `AiTrainer__Enabled` | oui | `false` | non | Active l’IA réelle |
| `AiTrainer__Provider` | si IA | `Anam` | non | Fournisseur enregistré |
| `Anam__ApiKey` | si Anam | valeur du coffre | oui | Clé serveur |
| `HeyGen__ApiKey` | si adaptateur ajouté | valeur du coffre | oui | Extension future |
| `LanguageModel__ApiKey` | si LLM réel | valeur du coffre | oui | Extension future |
| `SpeechToText__ApiKey` | si STT réel | valeur du coffre | oui | Extension future |
| `TextToSpeech__ApiKey` | si TTS réel | valeur du coffre | oui | Extension future |
| `CertificateStorage__Provider` | oui | `Local` | non | Fournisseur actuel |
| `CertificateStorage__BasePath` | oui | `/app/data/certificates` | non | Volume PDF |
| `DataProtection__KeysPath` | oui | `/app/data/keys` | non | Volume de clés |
| `DataProtection__ApplicationName` | oui | `TrainingManagement` | non | Discriminateur partagé |
| `Maintenance__TempPath` | oui | `/app/data/temp` | non | Fichiers temporaires contrôlés |

Une absence de connexion, une URL publique non HTTPS, un seed incomplet ou des options invalides
font échouer le démarrage sans afficher la valeur sensible.
