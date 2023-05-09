namespace FoxMud.Common.Models;

public class Character
{
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public string CharacterClass { get; set; }
    public string CharacterRace { get; set; }
    public string Description { get; set; }

    public Character(string name, string passwordHash, string characterClass, string characterRace, string description)
    {
        Name = name;
        PasswordHash = passwordHash;
        CharacterClass = characterClass;
        CharacterRace = characterRace;
        Description = description;
    }
}
