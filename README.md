# 🚀 Aion ERP - Moteur universel open-source

![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-blue)
![EF Core 8](https://img.shields.io/badge/EF%20Core-8.0-green)
![License](https://img.shields.io/badge/license-MIT-orange)

**Aion** est un moteur ERP modulaire et générique basé sur une architecture métadonnées. Le système se configure via un fichier YAML central (`AION_ROADMAP.yaml`) et génère automatiquement les entités, interfaces et droits d'accès.

---

## ✨ Fonctionnalités principales

- 🔐 **Système RBAC complet** : Droits granulaires par groupe (Menu, Module, Table, Action, Report)
- 🧩 **Architecture modulaire** : Modules métiers indépendants et réutilisables
- 📊 **Moteur de données dynamique** : Tables générées depuis métadonnées
- 🎨 **Interface Fluent UI** : Composants modernes et accessibles
- 🌐 **Multi-tenant natif** : Isolation des données par organisation
- 📝 **Audit complet** : Traçabilité de toutes les modifications
- 🔄 **Soft delete** : Conservation de l'historique
- ⚡ **Cache intelligent** : Performance optimisée avec invalidation ciblée
- 🤖 **IA intégrée** : Support OpenAI pour génération et analyse

---

## 🧱 Modules livrés en v0.4.x

- `SYSTEM_CATALOG` – gestion des menus, modules, tables et champs système.
- `SECURITY_ADMIN` – administration utilisateurs, groupes, droits et tenants.
- `TABLE_MANAGER` – inspection SQL des tables physiques et synchronisation métabase.
- `LIST_DYN` – listes dynamiques (grilles Fluent UI avec tri multi-colonnes persisté).
- `FORM_DYN` – rendu de formulaires dynamiques basés sur les métadonnées.
- `DATA_ENGINE` – synchronisation de la métabase et configuration IA (`SXAiConfig`, synonymes, templates).
- `ACTION_ENGINE` – exécution et planification des actions serveur.
- `REPORT_ENGINE` – registres de rapports et génération dynamique.
- `CRM` – module fonctionnel d'exemple (entreprises & contacts).
- `AI_ENGINE` – orchestrateur IA (intents, planner, patcher, simulateur, audit `SAuditRecord`).

---

## 🏗️ Architecture

```
/src
  ├── Aion.Domain/          # Entités métier, Value Objects, interfaces métier (aucune dépendance)
  ├── Aion.Infrastructure/  # DbContext, repositories, implémentations techniques dépendantes du domaine
  ├── Aion.AI/              # Orchestration IA et contrats fournisseurs (dépend du domaine)
  ├── Aion.AI/Providers.*   # Providers IA interchangeables (OpenAI, Mistral, etc.)
  ├── Aion.AppHost/         # Hôte MAUI Blazor Hybrid (UI, navigation, DI uniquement)
  └── AION_ROADMAP.yaml     # Source de vérité (métadonnées)

/tests
  ├── Aion.Domain.Tests/
  └── Aion.Infrastructure.Tests/

/docs
  ├── AION_Vision/
  ├── AION_Specification/
  └── AION_Prompts/
```

### Principes architecturaux

- **Séparation stricte** : Domaine isolé de toute dépendance UI ou infrastructure.
- **MAUI comme AppHost** : Aucun DbContext, service métier ou logique IA n’y réside.
- **Interchangeabilité IA** : Fournisseurs OpenAI / Mistral déclarés via configuration sécurisée.
- **CQRS léger** : Séparation lecture/écriture si nécessaire.

---

## 🚀 Démarrage rapide

### Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server 2019+](https://www.microsoft.com/sql-server) ou SQLite
- IDE : Visual Studio 2022 / VS Code / Rider

### Installation

```bash
# 1. Cloner le repository
git clone https://github.com/jsgalaxy29-hash/Aion.git
cd Aion

# 2. Restaurer les packages
dotnet restore

# 3. Configurer la connexion (appsettings.json)
# "AionDb": "Server=localhost;Database=AionDb;Trusted_Connection=True;TrustServerCertificate=True"

# 4. Créer la base de données
dotnet ef database update --project src/Aion.Security --startup-project src/Aion.AppHost

# 5. Lancer l'application
dotnet run --project src/Aion.AppHost
```

### Première connexion

- URL : `https://localhost:5001/login`
- Utilisateur : `admin`
- Mot de passe : `admin`
- TenantId : `1`

⚠️ **IMPORTANT** : lors de la première connexion, Aion impose la définition d'un nouveau mot de passe (BCrypt facteur 12).
Profitez-en pour adopter un secret fort avant d'aller en production.

### Build & tests

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --no-build
```

### Configuration IA sécurisée

- Les exemples `src/Aion.AppHost/appsettings.OpenAI.example.json` et `src/Aion.AppHost/appsettings.Mistral.example.json` fournissent les clés attendues sans secret.
- Les fichiers réels `appsettings.OpenAI.json` et `appsettings.Mistral.json` sont ignorés par Git ; configurez-les via `dotnet user-secrets` en local :

```bash
dotnet user-secrets set "OpenAI:ApiKey" "<clé>" --project src/Aion.AppHost
dotnet user-secrets set "Mistral:ApiKey" "<clé>" --project src/Aion.AppHost
```

- En CI / production, privilégiez les variables d'environnement ou un provider de secrets externe.

---

## 🔐 Système de droits (RBAC)

### Structure

```
SUser (Utilisateur)
  └─> SUserGroup (Association)
       └─> SGroup (Groupe)
            └─> SRight (Droit)
                 └─> SRightType (Type de droit)
```

### Types de droits

| Code | Description | Axes disponibles |
|------|-------------|------------------|
| `Menu` | Visibilité des menus | R1: Voir |
| `Module` | Accès aux modules | R1: Lire, R2: Écrire, R3: Supprimer, R4: Exporter, R5: Admin |
| `Table` | Opérations sur tables | R1: Lire, R2: Créer, R3: Modifier, R4: Supprimer, R5: Exporter |
| `Action` | Exécution d'actions | R1: Exécuter |
| `Report` | Génération de rapports | R1: Voir, R2: Générer |

### Règle de fusion

**true > false** : Si un utilisateur appartient à plusieurs groupes, le droit le plus permissif gagne.

Exemple :
- Groupe A : Menu:5 = `Right1: false`
- Groupe B : Menu:5 = `Right1: true`
- **Résultat** : L'utilisateur voit le menu 5 ✅

---

## 💻 Utilisation dans le code

### Vérifier un droit (Service)

```csharp
@inject IRightService RightService

var hasRight = await RightService.HasRightAsync(
    userId: 1,
    tenantId: 1,
    target: "Module",
    subjectId: 5,
    flag: RightFlag.Right2  // Écriture
);

if (hasRight)
{
    // Autoriser l'action
}
```

### Vérifier un droit (Composant)

```razor
<AionAuthorize Target="Action" SubjectId="42" RequiredRight="RightFlag.Right1">
    <button @onclick="ExecuteAction">Exécuter</button>
</AionAuthorize>
```

### Récupérer l'utilisateur courant

```csharp
@using Aion.Security.Extensions

var userId = User.GetUserId();          // int?
var tenantId = User.GetTenantId();      // int?
var fullName = User.GetFullName();      // string
var isAdmin = User.IsAdmin();           // bool
```

---

## 📊 Configuration via YAML

### Exemple : Ajout d'un module

```yaml
modules:
  - code: "INVENTORY"
    name: "Gestion des stocks"
    icon: "Box"
    color: "#107C10"
    
    menus:
      - id: 200
        label: "Stock"
        route: "/modules/inventory"
        is_leaf: true
        icon: "BoxMultiple"
    
    tables:
      - name: "FProduct"
        fields:
          - name: "Reference"
            type: "string"
            required: true
          - name: "Quantity"
            type: "int"
```

Après modification du YAML, régénérer les métadonnées :

```bash
dotnet run --project src/Aion.AppHost -- --regen-meta
```

---

## 🧪 Tests et debug

### Page de debug des droits

URL : `/debug/rights`

Affiche :
- Claims chargés
- Droits effectifs par target
- Informations utilisateur

### Tests unitaires

```bash
dotnet test
```

### Logs détaillés

```bash
dotnet run --verbosity detailed
```

---

## 📦 Structure des tables système

| Table | Description |
|-------|-------------|
| `SUser` | Utilisateurs |
| `SGroup` | Groupes (rôles) |
| `SUserGroup` | Association user-groupe |
| `SRight` | Droits par groupe |
| `SRightType` | Types de droits |
| `SMenu` | Menus de navigation |
| `STable` | Métadonnées tables |
| `SField` | Métadonnées colonnes |
| `SModule` | Modules métiers |
| `SAction` | Actions système |

---

## 🔧 Configuration avancée

### Changer le provider de cache

Par défaut : `IMemoryCache` (in-process)

Pour Redis (production) :

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### Activer le hashing BCrypt

```csharp
// AuthService.cs
private bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}

// Lors de la création d'utilisateur
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

### Multi-database

```csharp
// Program.cs
builder.Services.AddDbContext<SecurityDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SecurityDb")));

builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AionDb")));
```

---

## 🤝 Contribution

Les contributions sont les bienvenues ! Processus :

1. Fork du projet
2. Créer une branche (`git checkout -b feature/AmazingFeature`)
3. Commit (`git commit -m 'Add AmazingFeature'`)
4. Push (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

### Conventions de code

- **Namespaces** : `Aion.<Layer>.<Feature>`
- **Entities** : PascalCase, suffixe selon type (`SUser`, `F_Customer`)
- **Services** : Interface `I<Name>Service`, implémentation `<Name>Service`
- **Composants Razor** : PascalCase, suffixe `.razor`

---

## 📝 Roadmap

- [x] Système RBAC complet
- [x] Multi-tenant
- [x] Audit & soft delete
- [ ] API REST générée automatiquement
- [ ] Moteur de workflow visuel
- [ ] Générateur de rapports WYSIWYG
- [ ] Import/Export Excel/CSV
- [ ] Module BI intégré
- [ ] Mobile app (MAUI)
- [ ] Marketplace de modules

---

## 📄 License

Ce projet est sous licence MIT. Voir le fichier `LICENSE` pour plus de détails.

---

## 👤 Auteur

**Julien Sénécal**

- GitHub: [@jsgalaxy29-hash](https://github.com/jsgalaxy29-hash)
- Projet: [Aion](https://github.com/jsgalaxy29-hash/Aion)

---

## 🙏 Remerciements

- Communauté .NET et Blazor
- Projet Fluent UI
- Tous les contributeurs open-source

---

**⭐ Si ce projet vous est utile, n'hésitez pas à lui donner une étoile !**