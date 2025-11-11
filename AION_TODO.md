# 📌 Aion v0.4.x TODO

## Platforme & Infrastructure
- [ ] Finaliser les scénarios multi-tenant avancés (switch dynamique de locataire en UI).
- [ ] Ajouter la commande CLI `aion provision --tenant <id>` pour provisionnement incrémental.
- [ ] Centraliser la configuration de connexion (SQL Server / SQLite) dans `appsettings.*.json` avec secrets utilisateur.

## Sécurité
- [x] Forcer le changement de mot de passe administrateur au premier login.
- [ ] Ajouter l’expiration configurable des mots de passe.
- [ ] Générer des rapports d’audit hebdomadaires (`SecurityAudit`).

## Métabase & Data Engine
- [x] Requête d’inspection SQL Server enrichie (clé primaire, unique, FK).
- [ ] Support de l’inspection PostgreSQL.
- [ ] Synchroniser automatiquement les préférences de grille (tri multiple, filtres).

## IA & Automation
- [x] Journaliser les exécutions IA dans `SAuditRecord`.
- [ ] Connecter un LLM externe (Azure OpenAI) via configuration `SXAiConfig`.
- [ ] Étendre le simulateur pour valider les migrations Entity Framework.

## UI Fluent / Blazor
- [ ] Finaliser la page `/modules` (recherche, favoris, récents).
- [ ] Persister la configuration du `FluentDataGrid` côté utilisateur.
- [ ] Corriger `FluentDialog` (focus management + taille responsive).
- [ ] Vérifier le chargement mobile de `FluentDesignTheme.razor.js`.

## Tests & CI/CD
- [x] Ajouter un pipeline GitHub Actions (build + tests).
- [ ] Écrire un test d’intégration de provisioning complet.
- [ ] Ajouter une étape de packaging AppHost (zip) dans la CI.

## Documentation
- [x] Aligner le README sur .NET 10 et la migration v0.4.x.
- [ ] Documenter la configuration multi-tenant approfondie.
- [ ] Publier un guide contributeur (structure modules, conventions UI).
