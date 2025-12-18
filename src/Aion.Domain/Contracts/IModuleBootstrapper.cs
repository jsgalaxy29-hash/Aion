namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Contrat pour initialiser un module externe. Permet d'enregistrer des routes, des widgets et des policies.
    /// </summary>
    public interface IModuleBootstrapper
    {
        void Register();
    }
}
