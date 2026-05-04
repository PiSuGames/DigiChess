using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChampionController : MonoBehaviour
{
    public static int TEAMID_PLAYER = 0;
    public static int TEAMID_AI = 1;

    public GameObject levelupEffectPrefab;
    public GameObject projectileStart;
    public GameObject evo1;
    public GameObject evo2;
    public GameObject evo3;

    [HideInInspector] public int gridType = 0;
    [HideInInspector] public int gridPositionX = 0;
    [HideInInspector] public int gridPositionZ = 0;
    [HideInInspector] public int teamID = 0;

    [HideInInspector] public Champion champion;

    [HideInInspector] public float maxHealth = 0;
    [HideInInspector] public float currentHealth = 0;
    [HideInInspector] public float currentDamage = 0;

    public int lvl = 1;

    private Map map;
    private GamePlayController gamePlayController;
    private AIopponent aIopponent;
    private ChampionAnimation championAnimation;
    private WorldCanvasController worldCanvasController;
    private NavMeshAgent navMeshAgent;

    private Vector3 gridTargetPosition;
    private bool _isDragged = false;

    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public bool isDead = false;
    private bool isInCombat = false;
    private float combatTimer = 0;

    private bool isStuned = false;
    private float stunTimer = 0;

    private List<Effect> effects;

    // =================================================================
    // -------------------- BONUS / SINERGY ATTRIBUTES -----------------
    // =================================================================

    [HideInInspector] public float bonusAttackSpeed = 0f;
    [HideInInspector] public float bonusLifeSteal = 0f;
    [HideInInspector] public float bonusCritChance = 0f;
    [HideInInspector] public float bonusCritMultiplier = 1.5f;
    [HideInInspector] public float bonusManaGain = 0f;
    [HideInInspector] public float bonusRegen = 0f;
    [HideInInspector] public float bonusReflect = 0f;
    [HideInInspector] public float armorDebuff = 0f;

    private float regenTimer = 0f;
    private float tempShield = 0f;
    private float slowMultiplier = 1f;

    // Attack timing
    public float baseAttackCooldown = 1f;
    private float attackCooldownTimer = 0f;

    // Mana / habilidad especial
    [HideInInspector] public float currentMana = 0f;
    private float maxMana;
    private float manaGainPerAttack;
    private float manaGainOnHit;

    //---------------------------------------------------

    void Start() { }

    public void Init(Champion _champion, int _teamID)
    {
        champion = _champion;
        teamID = _teamID;

        map = GameObject.Find("Scripts").GetComponent<Map>();
        aIopponent = GameObject.Find("Scripts").GetComponent<AIopponent>();
        gamePlayController = GameObject.Find("Scripts").GetComponent<GamePlayController>();
        worldCanvasController = GameObject.Find("Scripts").GetComponent<WorldCanvasController>();
        navMeshAgent = this.GetComponent<NavMeshAgent>();
        championAnimation = this.GetComponent<ChampionAnimation>();

        navMeshAgent.enabled = false;

        maxHealth = champion.health;
        currentHealth = champion.health;
        currentDamage = champion.damage;



        worldCanvasController.AddHealthBar(this.gameObject);
        effects = new List<Effect>();

        attackCooldownTimer = baseAttackCooldown;
    }

    //---------------------------------------------------
    // UPDATE
    //---------------------------------------------------

    void Update()
    {
        HandleDragging();
        HandlePreparationMovement();
        HandleCombatLogic();
        HandleStun();
        HandleRegen();
    }

    //---------------------------------------------------
    // DRAGGING
    //---------------------------------------------------

    private void HandleDragging()
    {
        if (!_isDragged) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float enter = 100.0f;

        if (map.m_Plane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 p = new Vector3(hitPoint.x, 1.0f, hitPoint.z);
            transform.position = Vector3.Lerp(transform.position, p, 0.15f);
        }
    }

    //---------------------------------------------------
    // PREPARATION MOVEMENT
    //---------------------------------------------------

    private void HandlePreparationMovement()
    {
        if (_isDragged) return;
        if (gamePlayController.currentGameStage != GameStage.Preparation) return;

        float distance = Vector3.Distance(gridTargetPosition, transform.position);

        if (distance > 0.25f)
            transform.position = Vector3.Lerp(transform.position, gridTargetPosition, 0.1f);
        else
            transform.position = gridTargetPosition;
    }

    //---------------------------------------------------
    // COMBAT LOGIC
    //---------------------------------------------------

    private void HandleCombatLogic()
    {
        if (!isInCombat || isStuned) return;

        attackCooldownTimer -= Time.deltaTime * (1f + bonusAttackSpeed);

        // find new target if needed
        if (target == null)
        {
            combatTimer += Time.deltaTime;
            if (combatTimer > 0.5f)
            {
                combatTimer = 0;
                TryAttackNewTarget();
            }
            return;
        }

        if (target.GetComponent<ChampionController>().isDead)
        {
            target = null;
            navMeshAgent.isStopped = true;
            return;
        }

        transform.LookAt(target.transform, Vector3.up);

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (!isAttacking)
        {
            if (distance < champion.attackRange)
            {
                if (attackCooldownTimer <= 0f)
                {
                    DoAttack();
                    attackCooldownTimer = baseAttackCooldown;
                }
            }
            else
            {
                navMeshAgent.destination = target.transform.position;
            }
        }
    }

    //---------------------------------------------------
    // STUN HANDLING
    //---------------------------------------------------

    private void HandleStun()
    {
        if (!isStuned) return;

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            isStuned = false;
            championAnimation.IsAnimated(true);

            if (target != null)
            {
                navMeshAgent.destination = target.transform.position;
                navMeshAgent.isStopped = false;
            }
        }
    }

    //---------------------------------------------------
    // REGEN
    //---------------------------------------------------

    private void HandleRegen()
    {
        if (bonusRegen <= 0) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= 1f)
        {
            currentHealth = Mathf.Min(currentHealth + bonusRegen, maxHealth);
            regenTimer = 0f;
        }
    }

    //---------------------------------------------------
    // GRID AND POSITIONING
    //---------------------------------------------------

    public bool IsDragged
    {
        get => _isDragged;
        set => _isDragged = value;
    }

    public void Reset()
    {
        gameObject.SetActive(true);

        maxHealth = champion.health * lvl;
        currentHealth = champion.health * lvl;

        isDead = false;
        isInCombat = false;
        target = null;
        isAttacking = false;
        currentMana = 0f;

        SetWorldPosition();
        SetWorldRotation();

        foreach (Effect e in effects)
            e.Remove();

        effects = new List<Effect>();
    }

    public void SetGridPosition(int _gridType, int _gridPositionX, int _gridPositionZ)
    {
        gridType = _gridType;
        gridPositionX = _gridPositionX;
        gridPositionZ = _gridPositionZ;

        gridTargetPosition = GetWorldPosition();
    }

    public Vector3 GetWorldPosition()
    {
        if (gridType == Map.GRIDTYPE_OWN_INVENTORY)
            return map.ownInventoryGridPositions[gridPositionX];

        if (gridType == Map.GRIDTYPE_HEXA_MAP)
            return map.mapGridPositions[gridPositionX, gridPositionZ];

        return Vector3.zero;
    }

    public void SetWorldPosition()
    {
        navMeshAgent.enabled = false;
        Vector3 worldPosition = GetWorldPosition();
        transform.position = worldPosition;
        gridTargetPosition = worldPosition;
    }

    public void SetWorldRotation()
    {
        Vector3 rotation = (teamID == 0)
            ? new Vector3(0, 200, 0)
            : new Vector3(0, 20, 0);

        transform.rotation = Quaternion.Euler(rotation);
    }

    //---------------------------------------------------
    // COMBAT
    //---------------------------------------------------

    private GameObject target;

    private GameObject FindTarget()
    {
        GameObject closestEnemy = null;
        float bestDistance = 9999f;

        GameObject[,] grid =
            (teamID == TEAMID_PLAYER)
            ? aIopponent.gridChampionsArray
            : gamePlayController.gridChampionsArray;

        int maxZ = Map.hexMapSizeZ / 2;

        for (int x = 0; x < Map.hexMapSizeX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                GameObject go = grid[x, z];
                if (go == null) continue;

                ChampionController c = go.GetComponent<ChampionController>();
                if (c.isDead) continue;

                float d = Vector3.Distance(transform.position, go.transform.position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    closestEnemy = go;
                }
            }
        }

        return closestEnemy;
    }

    private void TryAttackNewTarget()
    {
        target = FindTarget();
        if (target != null)
        {
            navMeshAgent.destination = target.transform.position;
            navMeshAgent.isStopped = false;
        }
    }

    public void OnCombatStart()
    {
        IsDragged = false;
        transform.position = gridTargetPosition;

        if (gridType == Map.GRIDTYPE_HEXA_MAP)
        {
            isInCombat = true;
            navMeshAgent.enabled = true;
            TryAttackNewTarget();
        }
    }

    // =====================================================================
    // UPGRADE LEVEL (NECESARIO PARA SHOP Y COMBINACIONES 3-STAR)
    // =====================================================================
    public void UpgradeLevel()
    {
        lvl++;

        float newSize = 1f;

        // Reset stats to scale with lvl
        maxHealth = champion.health * lvl;
        currentHealth = maxHealth;
        currentDamage = champion.damage * lvl;

        // Evolution model switch
        if (lvl == 2)
        {
            newSize = 1.5f;
            if (evo1 != null) evo1.SetActive(false);
            if (evo2 != null) evo2.SetActive(true);
        }
        else if (lvl == 3)
        {
            newSize = 2f;
            if (evo2 != null) evo2.SetActive(false);
            if (evo3 != null) evo3.SetActive(true);
        }

        // Scale model
        transform.localScale = new Vector3(newSize, newSize, newSize);

        // Level-up FX
        if (levelupEffectPrefab != null)
        {
            GameObject fx = Instantiate(levelupEffectPrefab);
            fx.transform.position = transform.position;
            Destroy(fx, 1f);
        }
    }



    // =====================================================================
    // STUN HANDLER (NECESARIO PARA BONUS Stun)
    // =====================================================================
    public void OnGotStun(float duration)
    {
        isStuned = true;
        stunTimer = duration;

        if (championAnimation != null)
            championAnimation.IsAnimated(false);

        if (navMeshAgent != null)
            navMeshAgent.isStopped = true;
    }



    // =====================================================================
    // HEAL HANDLER (NECESARIO PARA BONUS Heal)
    // =====================================================================
    public void OnGotHeal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }


    private void DoAttack()
    {
        isAttacking = true;
        navMeshAgent.isStopped = true;

        // Método REAL que existe en tu proyecto:
        championAnimation.DoAttack(true);
    }

    public void OnAttackAnimationFinished()
    {
        isAttacking = false;
        if (target == null) return;

        ChampionController targetChampion = target.GetComponent<ChampionController>();

        List<ChampionBonus> activeBonuses =
            (teamID == TEAMID_PLAYER)
            ? gamePlayController.activeBonusList
            : aIopponent.activeBonusList;

        float bonusDamage = 0;
        foreach (ChampionBonus b in activeBonuses)
            bonusDamage += b.ApplyOnAttack(this, targetChampion);

        float finalDamage = currentDamage + bonusDamage;

        if (RollCrit())
            finalDamage *= bonusCritMultiplier;

        bool isTargetDead = targetChampion.OnGotHit(finalDamage);

        if (bonusLifeSteal > 0)
            ApplyLifeSteal(finalDamage);

        GainManaFromAttack();

        if (isTargetDead)
            TryAttackNewTarget();

        if (champion.attackProjectile != null && projectileStart != null)
        {
            GameObject projectile = Instantiate(champion.attackProjectile);
            projectile.transform.position = projectileStart.transform.position;
            projectile.GetComponent<Projectile>().Init(target);
        }
    }

    //---------------------------------------------------
    // DAMAGE, SHIELD, REFLECT, REGEN, ARMORBREAK
    //---------------------------------------------------

    public bool OnGotHit(float damage)
    {
        List<ChampionBonus> activeBonuses =
            (teamID == TEAMID_PLAYER)
            ? gamePlayController.activeBonusList
            : aIopponent.activeBonusList;

        foreach (ChampionBonus b in activeBonuses)
            damage = b.ApplyOnGotHit(this, damage);

        // ARMORBREAK
        if (armorDebuff > 0)
            damage *= (1f + armorDebuff);

        // SHIELD
        ConsumeShield(ref damage);

        // REFLECT
        if (bonusReflect > 0f)
        {
            float reflected = damage * bonusReflect;
            worldCanvasController.AddDamageText(transform.position + Vector3.up * 2.5f, reflected);
            currentHealth -= reflected;
        }

        currentHealth -= damage;

        worldCanvasController.AddDamageText(transform.position + Vector3.up * 2.5f, damage);

        if (currentHealth <= 0)
        {
            isDead = true;
            gameObject.SetActive(false);
            aIopponent.OnChampionDeath();
            gamePlayController.OnChampionDeath();
            return true;
        }
        GainManaFromHit();
        return false;
    }

    public void ApplyLifeSteal(float dmg)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + dmg * bonusLifeSteal);
    }

    public void ApplyBurn(float dps, float duration)
    {
        StartCoroutine(BurnCoroutine(dps, duration));
    }

    IEnumerator BurnCoroutine(float dps, float duration)
    {
        float timer = 0;
        while (timer < duration && !isDead)
        {
            currentHealth -= dps;
            worldCanvasController.AddDamageText(transform.position + Vector3.up * 2.5f, dps);

            if (currentHealth <= 0)
            {
                isDead = true;
                gameObject.SetActive(false);
                aIopponent.OnChampionDeath();
                gamePlayController.OnChampionDeath();
                yield break;
            }

            yield return new WaitForSeconds(1f);
            timer += 1f;
        }
    }

    public void GiveShield(float amount)
    {
        tempShield += amount;
    }

    public void ConsumeShield(ref float damage)
    {
        if (tempShield <= 0) return;

        float absorb = Mathf.Min(tempShield, damage);
        tempShield -= absorb;
        damage -= absorb;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        StartCoroutine(SlowCoroutine(multiplier, duration));
    }

    IEnumerator SlowCoroutine(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        navMeshAgent.speed *= multiplier;
        yield return new WaitForSeconds(duration);
        navMeshAgent.speed /= multiplier;
        slowMultiplier = 1f;
    }

    public bool RollCrit()
    {
        return Random.Range(0f, 100f) < bonusCritChance;
    }

    public void GainMana(float amount)
    {
        // Llamado por la sinergia ManaGain - acumula mana real
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        if (currentMana >= maxMana)
            CastAbility();
    }

    public void ApplyTaunt(float duration)
    {
        StartCoroutine(TauntCoroutine(duration));
    }

    IEnumerator TauntCoroutine(float duration)
    {
        // Si quieres que ataque SIEMPRE a quien hizo taunt, aquí iría la lógica.
        yield return new WaitForSeconds(duration);
    }

    public void ApplyArmorBreak(float value, float duration)
    {
        StartCoroutine(ArmorBreakCoroutine(value, duration));
    }

    IEnumerator ArmorBreakCoroutine(float value, float duration)
    {
        armorDebuff += value;
        yield return new WaitForSeconds(duration);
        armorDebuff -= value;
    }

    //---------------------------------------------------
    // EFFECTS (PARTICLES / VISUALS)
    //---------------------------------------------------

    public void AddEffect(GameObject effectPrefab, float duration)
    {
        if (effectPrefab == null) return;

        foreach (Effect e in effects)
        {
            if (e.effectPrefab == effectPrefab)
            {
                e.duration = duration;
                return;
            }
        }

        Effect newEffect = gameObject.AddComponent<Effect>();
        newEffect.Init(effectPrefab, gameObject, duration);
        effects.Add(newEffect);
    }

    public void RemoveEffect(Effect effect)
    {
        effects.Remove(effect);
        effect.Remove();
    }


    //---------------------------------------------------
    // MANA Y HABILIDADES ESPECIALES
    //---------------------------------------------------

    private void GainManaFromAttack()
    {
        if (!isInCombat) return;
        currentMana = Mathf.Min(maxMana, currentMana + manaGainPerAttack);
        if (currentMana >= maxMana)
            CastAbility();
    }

    private void GainManaFromHit()
    {
        if (!isInCombat) return;
        currentMana = Mathf.Min(maxMana, currentMana + manaGainOnHit);
        if (currentMana >= maxMana)
            CastAbility();
    }

    private void CastAbility()
    {
        if (champion.abilityType == DigimonAbilityType.None) return;
        currentMana = 0f;

        switch (champion.abilityType)
        {
            case DigimonAbilityType.PepperBreath: CastPepperBreath(); break;
            case DigimonAbilityType.BlueBlaster: CastBlueBlaster(); break;
            case DigimonAbilityType.BoomBubble: CastBoomBubble(); break;
            case DigimonAbilityType.SuperShocker: CastSuperShocker(); break;
            case DigimonAbilityType.PoisonIvy: CastPoisonIvy(); break;
            case DigimonAbilityType.MarchingFishes: CastMarchingFishes(); break;
            case DigimonAbilityType.SpiralTwister: CastSpiralTwister(); break;
            case DigimonAbilityType.HowlBoost: CastHowlBoost(); break;
            case DigimonAbilityType.NovaBlast: CastNovaBlast(); break;
            case DigimonAbilityType.TerraForce: CastTerraForce(); break;
        }
    }

    // Devuelve los N enemigos vivos más cercanos
    private System.Collections.Generic.List<ChampionController> GetNearestEnemies(int maxCount)
    {
        GameObject[,] grid = (teamID == TEAMID_PLAYER)
            ? aIopponent.gridChampionsArray
            : gamePlayController.gridChampionsArray;

        var candidates = new System.Collections.Generic.List<(ChampionController cc, float dist)>();

        for (int x = 0; x < Map.hexMapSizeX; x++)
            for (int z = 0; z < Map.hexMapSizeZ / 2; z++)
            {
                GameObject go = grid[x, z];
                if (go == null) continue;
                ChampionController cc = go.GetComponent<ChampionController>();
                if (cc.isDead) continue;
                candidates.Add((cc, Vector3.Distance(transform.position, go.transform.position)));
            }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        var result = new System.Collections.Generic.List<ChampionController>();
        for (int i = 0; i < Mathf.Min(maxCount, candidates.Count); i++)
            result.Add(candidates[i].cc);
        return result;
    }

    // Devuelve los N aliados vivos más cercanos
    private System.Collections.Generic.List<ChampionController> GetNearestAllies(int maxCount)
    {
        GameObject[,] grid = (teamID == TEAMID_PLAYER)
            ? gamePlayController.gridChampionsArray
            : aIopponent.gridChampionsArray;

        var candidates = new System.Collections.Generic.List<(ChampionController cc, float dist)>();

        for (int x = 0; x < Map.hexMapSizeX; x++)
            for (int z = 0; z < Map.hexMapSizeZ / 2; z++)
            {
                GameObject go = grid[x, z];
                if (go == null) continue;
                ChampionController cc = go.GetComponent<ChampionController>();
                if (cc.isDead || cc == this) continue;
                candidates.Add((cc, Vector3.Distance(transform.position, go.transform.position)));
            }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        var result = new System.Collections.Generic.List<ChampionController>();
        for (int i = 0; i < Mathf.Min(maxCount, candidates.Count); i++)
            result.Add(candidates[i].cc);
        return result;
    }

    // Agumon – Pepper Breath: daño de fuego a los 2 enemigos más cercanos
    private void CastPepperBreath()
    {
        foreach (var enemy in GetNearestEnemies(2))
            enemy.OnGotHit(champion.abilityValue);
    }

    // Gabumon – Blue Blaster: daño leve + ralentiza a todos los enemigos
    private void CastBlueBlaster()
    {
        foreach (var enemy in GetNearestEnemies(99))
        {
            enemy.OnGotHit(champion.abilityValue * 0.4f);
            enemy.ApplySlow(0.4f, 3f);
        }
    }

    // Patamon – Boom Bubble: escudo para todos los aliados
    private void CastBoomBubble()
    {
        GiveShield(champion.abilityValue);
        foreach (var ally in GetNearestAllies(99))
            ally.GiveShield(champion.abilityValue);
    }

    // Tentomon – Super Shocker: aturde al enemigo más cercano
    private void CastSuperShocker()
    {
        var enemies = GetNearestEnemies(1);
        if (enemies.Count > 0)
            enemies[0].OnGotStun(2.5f);
    }

    // Palmon – Poison Ivy: quemadura (DoT) a todos los enemigos
    private void CastPoisonIvy()
    {
        foreach (var enemy in GetNearestEnemies(99))
            enemy.ApplyBurn(champion.abilityValue / 3f, 3f);
    }

    // Gomamon – Marching Fishes: cura a todos los aliados
    private void CastMarchingFishes()
    {
        OnGotHeal(champion.abilityValue);
        foreach (var ally in GetNearestAllies(99))
            ally.OnGotHeal(champion.abilityValue);
    }

    // Biyomon – Spiral Twister: daño a los 3 enemigos más cercanos
    private void CastSpiralTwister()
    {
        foreach (var enemy in GetNearestEnemies(3))
            enemy.OnGotHit(champion.abilityValue * 0.75f);
    }

    // Garurumon – Howl Boost: aumenta velocidad de ataque propia durante 5s
    private void CastHowlBoost()
    {
        StartCoroutine(HowlBoostCoroutine());
    }

    IEnumerator HowlBoostCoroutine()
    {
        bonusAttackSpeed += 1.0f;
        yield return new WaitForSeconds(5f);
        bonusAttackSpeed -= 1.0f;
    }

    // Greymon – Nova Blast: daño masivo a un único objetivo
    private void CastNovaBlast()
    {
        var enemies = GetNearestEnemies(1);
        if (enemies.Count > 0)
            enemies[0].OnGotHit(champion.abilityValue);
    }

    // WarGreymon – Terra Force: daño masivo a TODOS los enemigos
    private void CastTerraForce()
    {
        foreach (var enemy in GetNearestEnemies(99))
            enemy.OnGotHit(champion.abilityValue);
    }
}
