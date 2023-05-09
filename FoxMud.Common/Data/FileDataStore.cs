using FoxMud.Common.Interfaces;
using FoxMud.Common.Models;

namespace FoxMud.Common.Data;

public class FileDataStore : IDataStore
{
    private readonly string _dataDirectory;

    public FileDataStore(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    public bool CharacterExists(string characterName)
    {
        string filePath = GetCharacterFilePath(characterName);
        return File.Exists(filePath);
    }

    public async Task<bool> CreateCharacterAsync(string characterName, string passwordHash, string characterClass, string characterRace, string description)
    {
        if (CharacterExists(characterName))
        {
            return false;
        }

        string data = $"{passwordHash}\n{characterClass}\n{characterRace}\n{description}";
        await SaveCharacterAsync(characterName, data);
        return true;
    }

    public async Task<string?> GetPasswordHashAsync(string characterName)
    {
        string filePath = GetCharacterFilePath(characterName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string[] lines = await File.ReadAllLinesAsync(filePath);
        return lines.Length > 0 ? lines[0] : null;
    }

    public async Task SaveCharacterAsync(string characterName, string characterData)
    {
        string filePath = GetCharacterFilePath(characterName);
        await File.WriteAllTextAsync(filePath, characterData);
    }

    private string GetCharacterFilePath(string characterName)
    {
        return Path.Combine(_dataDirectory, $"{characterName}.txt");
    }

    // todo: make this less hacky, more serializable
    public async Task<Character?> LoadCharacterAsync(string characterName)
    {
        string filePath = GetCharacterFilePath(characterName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        string[] lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length < 4)
        {
            // Not enough data to load a character
            return null;
        }

        return new Character(characterName, lines[0], lines[1], lines[2], lines[3]);
    }
}
