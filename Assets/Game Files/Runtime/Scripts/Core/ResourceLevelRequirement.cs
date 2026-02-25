[System.Serializable]
public class ResourceLevelRequirement
{
    public ResourceData resource;
    public int unlockLevel = 1;       // На каком уровне становится доступным
    public float amountPerLevel = 1f; // Сколько нужно на каждый уровень
}