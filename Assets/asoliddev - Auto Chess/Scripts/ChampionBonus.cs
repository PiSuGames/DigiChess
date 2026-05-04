using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChampionBonusType
{
    Damage,
    Defense,
    Stun,
    Heal,
    AttackSpeed,        // aumenta velocidad de ataque
    LifeSteal,          // cura % del daño infligido
    Burn,               // daño por segundo
    Shield,             // gana escudo al atacar/recibir daño
    Slow,               // ralentiza al enemigo al golpearlo
    CritChance,         // añade probabilidad de crítico
    Range,              // aumenta rango
    Summon,             // invoca algo al morir o al atacar
    ManaGain,           // gana mana/energía al atacar o ser golpeado
    Reflect,            // devuelve daño al atacante
    Regen,              // se cura cada segundo
    ArmorBreak,         // reduce defensa enemiga
    Taunt               // obliga a atacar a este campeón
}

public enum BonusTarget { Self, Enemy };

/// <summary>
/// Controls the bonuses to get when have enough champions of the same type
/// </summary>
[System.Serializable]
public class ChampionBonus
{
    ///How many champions needed to get the bonus effect
    public int championCount = 0;

    ///Type of the bonus
    public ChampionBonusType championBonusType;

    ///Target of the bonus
    public BonusTarget bonusTarget;

    ///Float value of the bonus
    public float bonusValue = 0;

    ///How many secounds bonus lasts
    public float duration;

    ///Prefab to instantiate when bonus occours
    public GameObject effectPrefab;

    /// <summary>
    /// Calculates bonuses of a champion when attacking
    /// </summary>
    public float ApplyOnAttack(ChampionController champion, ChampionController targetChampion)
    {
        float bonusDamage = 0;
        bool addEffect = false;

        switch (championBonusType)
        {
            case ChampionBonusType.Damage:
                bonusDamage += bonusValue;
                break;

            case ChampionBonusType.Stun:
                int rand = Random.Range(0, 100);
                if (rand < bonusValue)
                {
                    targetChampion.OnGotStun(duration);
                    addEffect = true;
                }
                break;

            case ChampionBonusType.Heal:
                champion.OnGotHeal(bonusValue);
                addEffect = true;
                break;

            case ChampionBonusType.AttackSpeed:
                champion.bonusAttackSpeed += bonusValue;
                break;

            case ChampionBonusType.LifeSteal:
                champion.bonusLifeSteal = bonusValue; // % ejemplo 0.1 = 10%
                break;

            case ChampionBonusType.Burn:
                targetChampion.ApplyBurn(bonusValue, duration);
                break;

            case ChampionBonusType.Shield:
                champion.GiveShield(bonusValue);
                break;

            case ChampionBonusType.Slow:
                targetChampion.ApplySlow(bonusValue, duration);
                break;

            case ChampionBonusType.CritChance:
                champion.bonusCritChance += bonusValue;
                break;

            case ChampionBonusType.Range:
                champion.champion.attackRange += bonusValue;
                break;

            case ChampionBonusType.ManaGain:
                champion.GainMana(bonusValue);
                break;

            case ChampionBonusType.Taunt:
                targetChampion.ApplyTaunt(duration);
                break;

            case ChampionBonusType.Regen:
                champion.bonusRegen += bonusValue;
                break;

            case ChampionBonusType.Reflect:
                champion.bonusReflect += bonusValue;
                break;

            case ChampionBonusType.ArmorBreak:
                targetChampion.ApplyArmorBreak(bonusValue, duration);
                break;

            case ChampionBonusType.Summon:
                // aquí podrías instanciar algo si quieres
                // Instantiate(effectPrefab, champion.transform.position, Quaternion.identity);
                break;
        }

        if (addEffect)
        {
            if (bonusTarget == BonusTarget.Self)
                champion.AddEffect(effectPrefab, duration);
            else if (bonusTarget == BonusTarget.Enemy)
                targetChampion.AddEffect(effectPrefab, duration);
        }

        return bonusDamage;
    }

    /// <summary>
    /// Calculates bonuses of a Champion when got hit
    /// </summary>
    public float ApplyOnGotHit(ChampionController champion, float damage)
    {
        switch (championBonusType)
        {
            case ChampionBonusType.Defense:
                damage = ((100 - bonusValue) / 100f) * damage;
                break;

            // ArmorBreak lo manejas como debuff en el objetivo,
            // no hace falta tocarlo aquí si usas ApplyArmorBreak en el target.
            default:
                break;
        }

        return damage;
    }
}
