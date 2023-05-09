using FoxMud.Common.Models;

namespace FoxMud.Common.Interfaces;

public interface IDataStore
{
    bool CharacterExists(string characterName);
    Task<bool> CreateCharacterAsync(string characterName, string passwordHash, string characterClass, string characterRace, string description);
    Task<string?> GetPasswordHashAsync(string characterName);
    Task SaveCharacterAsync(string characterName, string characterData);
    Task<Character?> LoadCharacterAsync(string characterName);
}
