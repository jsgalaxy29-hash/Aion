namespace Aion.AI.Models;

/// <summary>
/// High level intents extracted from a natural language prompt.
/// </summary>
public enum RecognizedIntentType
{
    Unknown = 0,
    CreateModule,
    ExtendModule,
    AddEntity,
    AddField,
    AddRelation,
    GenerateCrud,
    DefineAction,
    DefineReport,
    SetPermissions
}
