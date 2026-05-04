/// <summary>
/// Habilidades únicas por Digimon, lore-accurate.
/// Asignar en el ScriptableObject Champion de cada Digimon.
/// </summary>
public enum DigimonAbilityType
{
    None,
    PepperBreath,   // Agumon:    Daño de fuego a los 2 enemigos más cercanos
    BlueBlaster,    // Gabumon:   Ralentiza a todos los enemigos + daño leve
    BoomBubble,     // Patamon:   Escudo para todos los aliados
    SuperShocker,   // Tentomon:  Aturde al enemigo más cercano
    PoisonIvy,      // Palmon:    Quemadura (DoT) a todos los enemigos
    MarchingFishes, // Gomamon:   Cura a todos los aliados
    SpiralTwister,  // Biyomon:   Daño a los 3 enemigos más cercanos
    HowlBoost,      // Garurumon: Boost de velocidad de ataque propia por 5s
    NovaBlast,      // Greymon:   Daño masivo a un único objetivo
    TerraForce,     // WarGreymon:Daño masivo a TODOS los enemigos
}