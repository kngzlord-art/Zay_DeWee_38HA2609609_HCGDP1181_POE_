
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

//My breeding script controls parent checks, baby creation, triat inheritance and mutations.
public class ImprovedBreedingRoom : Room
{
    //These settings controls the Displays of the Delay time, uiPanel and the resulttext
    [Header("Display Settings")]
    [SerializeField] private float breedingDelay = 5f;
    [SerializeField] private TextMeshProUGUI delayText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject uiPanel;

    //These are the spawn settings are they take in the childprefabs used to create the child and the spawnpoint.
    [Header("Spawn Objects")]
    [SerializeField] private GameObject childPrefab1;
    [SerializeField] private GameObject childPrefab2;
    [SerializeField] private GameObject childPrefab3;
    [SerializeField] private Transform babySpawnPoint;

    //This is my mutation chance settings
    [Header("Mutation Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.15f;
    [SerializeField] private Stat mutationStatTemplate;

    //This is my mutation Appearance size settings used to mutate the size of the child that is created 
    [Header("Mutation Appearance: Size")]
    [SerializeField]
    private Vector3 smallMutation =
        new Vector3(0.7f, 0.7f, 0.7f);

    
    [SerializeField]
    private Vector3 largeMutation =
        new Vector3(1.5f, 1.5f, 1.5f);

 
    [SerializeField]
    private Vector3 giantMutation =
        new Vector3(2f, 2f, 2f);

    //This is my mutation Appearance color settings used to mutate the color of the child that is created 
    [Header("Mutation Appearance: Colors")]
    [SerializeField] private Color redMutation = Color.red;
    [SerializeField] private Color blueMutation = Color.blue;
    [SerializeField] private Color greenMutation = Color.green;
    [SerializeField] private Color yellowMutation = Color.yellow;
    [SerializeField] private Color purpleMutation = Color.magenta;

    private Transform parent1;
    private Transform parent2;

    private bool isBreeding; //This bool will prevent another breeding process from starting while one is active.
    private bool hasBred; //This bool will prevent the same pair from breeding repeatedly without leaving the room.

    private Coroutine breedingCoroutine;

    private void Awake()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    private void Update()
    {
        //this checks the room contents every frame so breeding starts or cancels when creatures move 
        CheckForBreeding();
    }

    private void CheckForBreeding()
    {
        //this list gets the creaturs currently occupying this room.
        List<Transform> creatures = GetChildrenInRoom();

        if (isBreeding)
        {
            if (creatures.Count != 2 ||
                !creatures.Contains(parent1) ||
                !creatures.Contains(parent2))
            {
                if (breedingCoroutine != null)
                    StopCoroutine(breedingCoroutine);

                CancelBreeding();
            }

            return;
        }

        if (hasBred)
        {
            if (!creatures.Contains(parent1) ||
                !creatures.Contains(parent2))
            {
                hasBred = false;
                parent1 = null;
                parent2 = null;
            }

            return;
        }

        //This checks if, exactly two creaturs are needed; more than two cannot breed.
        if (creatures.Count != 2)
            return;

        Creature firstParent =
            creatures[0].GetComponent<Creature>();

        Creature secondParent =
            creatures[1].GetComponent<Creature>();

        if (firstParent == null || secondParent == null)
            return;

        parent1 = creatures[0];
        parent2 = creatures[1];

        breedingCoroutine = StartCoroutine(
            BreedCreatures(firstParent, secondParent)
        );
    }

    private IEnumerator BreedCreatures(
        Creature firstParent,
        Creature secondParent)
    {
        isBreeding = true; // This prevents a negative inspector value from creating a negative countdown.

        if (uiPanel != null)
            uiPanel.SetActive(true);

        if (resultText != null)
            resultText.text = "";

        float timer = Mathf.Max(0f, breedingDelay);

        while (timer > 0f)
        {
            List<Transform> creatures = GetChildrenInRoom();

            if (creatures.Count != 2 ||
                !creatures.Contains(parent1) ||
                !creatures.Contains(parent2))
            {
                CancelBreeding();
                yield break;
            }

            timer -= Time.deltaTime;//Here i reduce the countdown by the time passed since the previous frame

            if (delayText != null)
            {
                delayText.text =
                    Mathf.Ceil(Mathf.Max(0f, timer)).ToString();
            }

            yield return null;//Here it waits one frame before checking the room and update the countdown again 
        }

        List<Transform> finalCreatures = GetChildrenInRoom();

        if (finalCreatures.Count != 2 ||
            !finalCreatures.Contains(parent1) ||
            !finalCreatures.Contains(parent2))
        {
            CancelBreeding();
            yield break;
        }

        GameObject babyPrefab =
            GetBabyPrefab(firstParent.GetName());

        if (babyPrefab == null || babySpawnPoint == null)
        {
            CancelBreeding();
            yield break;
        }

        //Here is Create the baby prefab at the configured spawn posistions and rotations.

        GameObject babyObject = Instantiate(
            babyPrefab,
            babySpawnPoint.position,
            babySpawnPoint.rotation
        );

        babyObject.transform.SetParent(transform, true);

        Creature baby = babyObject.GetComponent<Creature>();

        if (baby == null)
        {
            Destroy(babyObject);
            CancelBreeding();
            yield break;
        }


        //The baby inherits traits from both the parent creatures.
        if (!InheritTraits(firstParent, secondParent, baby))
        {
            Destroy(babyObject);
            CancelBreeding();
            yield break;
        }

        //Here i visually represent inherited characteristics through size and colour.
        InheritAppearance(
            firstParent,
            secondParent,
            babyObject
        );

        bool mutated = false;

        //Here i give the baby a chance to develop a trait its parents do not have.
        if (Random.value < mutationChance)
        {
            if (!ParentHasMutation(firstParent, secondParent))
            {
                mutated = ApplyMutation(baby, babyObject);
            }
        }

        if (delayText != null)
            delayText.text = "0";

        if (resultText != null)
        {
            resultText.text = mutated
                ? "A baby was born with a new mutation!"
                : "A baby inherited traits from both parents.";
        }

        isBreeding = false;
        hasBred = true;
        breedingCoroutine = null;

        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    private GameObject GetBabyPrefab(string parentName)
    {
        switch (parentName)
        {
            case "Frant":
                return childPrefab1;

            case "Locky":
                return childPrefab2;

            case "Trotters":
                return childPrefab3;

            default:
                return null;
        }
    }

    private bool InheritTraits(
        Creature firstParent,
        Creature secondParent,
        Creature baby)
    {
        //This retrieves each creatures stats list so their traits can be compmared and inherited.
        List<Stat> firstStats = firstParent.GetStats();
        List<Stat> secondStats = secondParent.GetStats();
        List<Stat> babyStats = baby.GetStats();

        if (firstStats == null ||
            secondStats == null ||
            babyStats == null)
            return false;


        //This collects unique trait names from both parents, avoiding duplicates entries.
        List<string> inheritedNames = new List<string>();

        AddTraitNames(firstStats, inheritedNames);
        AddTraitNames(secondStats, inheritedNames);

        if (inheritedNames.Count == 0)
            return false;

        Dictionary<string, Stat> chosenSources =
            new Dictionary<string, Stat>();

        foreach (string traitName in inheritedNames)
        {
            Stat firstSource =
                FindStat(firstStats, traitName);

            Stat secondSource =
                FindStat(secondStats, traitName);

            Stat chosenSource;

            //If both parents have this trait, randomly choose which parents version is inherited.
            if (firstSource != null && secondSource != null)
            {
                chosenSource = Random.value < 0.5f
                    ? firstSource
                    : secondSource;
            }
            else
            {
                //if only one parent has the trait, inherit it from that parent
                chosenSource = firstSource != null
                    ? firstSource
                    : secondSource;
            }

            if (chosenSource != null &&
                chosenSource.GetTrait() != null)
            {
                chosenSources.Add(traitName, chosenSource);
            }
        }

        if (chosenSources.Count == 0)
            return false;

        List<Stat> newBabyStats = new List<Stat>();

        foreach (KeyValuePair<string, Stat> entry in chosenSources)
        {
            Stat source = entry.Value;

            //here it adds a matching stat component to the baby.
            Stat newStat = baby.gameObject.AddComponent(
                source.GetType()
            ) as Stat;

            if (newStat == null)
            {
                RemoveCreatedStats(newBabyStats);
                return false;
            }

            //I copy the slecte parent's serialized stat settings to the baby component.
            CopySerializedSettings(source, newStat);

            //Here i give the baby its own copy of the trait data instead of sharing the parent' object.
            if (!AssignIndependentTrait(
                newStat,
                source.GetTrait()))
            {
                Destroy(newStat);
                RemoveCreatedStats(newBabyStats);
                return false;
            }

            newBabyStats.Add(newStat);
        }

        //I replace the prefabs default Stats with inherited Stats after craeting the new ones successfully.
        List<Stat> oldBabyStats = new List<Stat>(babyStats);

        babyStats.Clear();

        foreach (Stat oldStat in oldBabyStats)
        {
            if (oldStat != null)
                Destroy(oldStat);
        }

        babyStats.AddRange(newBabyStats);

        return babyStats.Count > 0;
    }

    private void RemoveCreatedStats(List<Stat> stats)
    {
        foreach (Stat stat in stats)
        {
            if (stat != null)
                Destroy(stat);
        }
    }

    private void AddTraitNames(
        List<Stat> stats,
        List<string> names)
    {
        foreach (Stat stat in stats)
        {
            if (stat == null || stat.GetTrait() == null)
                continue;

            string traitName = stat.GetTrait().Name;

            if (!string.IsNullOrEmpty(traitName) &&
                !names.Contains(traitName))
            {
                names.Add(traitName);
            }
        }
    }

    private Stat FindStat(
        List<Stat> stats,
        string traitName)
    {
        foreach (Stat stat in stats)
        {
            if (stat != null &&
                stat.GetTrait() != null &&
                stat.GetTrait().Name == traitName)
            {
                return stat;
            }
        }

        return null;
    }

    private void CopySerializedSettings(
        Stat source,
        Stat destination)
    {
        //Here i use reflection to indentify the concrete stat subclass and its serialized feilds.
        System.Type type = source.GetType();

        while (type != null &&
               type != typeof(Stat) &&
               type != typeof(MonoBehaviour) &&
               type != typeof(Behaviour) &&
               type != typeof(Component) &&
               type != typeof(UnityEngine.Object))
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo field in fields)
            {
                if (field.Name == "trait" ||
                    field.IsStatic ||
                    field.IsInitOnly)
                    continue;

                bool isSerialized =
                    field.IsPublic ||
                    System.Attribute.IsDefined(
                        field,
                        typeof(SerializeField)
                    );

                if (!isSerialized)
                    continue;

                try
                {
                    field.SetValue(
                        destination,
                        field.GetValue(source)
                    );
                }
                catch (System.Exception)
                {
                    // Skip fields that cannot be copied.
                }
            }

            type = type.BaseType;
        }
    }

    private bool AssignIndependentTrait(
        Stat destination,
        Trait source)
    {
        if (destination == null || source == null)
            return false;

        FieldInfo traitField = typeof(Stat).GetField(
            "trait",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (traitField == null)
            return false;

        //here i create an independent trait instance so changes to the baby do not modify the parent's trait object.
        Trait copiedTrait = new Trait(
            source.Name,
            source.Value,
            source.Description
        );

        copiedTrait.MaxValue = source.MaxValue;

        traitField.SetValue(destination, copiedTrait);

        return destination.GetTrait() != null;
    }

    private void InheritAppearance(
        Creature firstParent,
        Creature secondParent,
        GameObject baby)
    {
        // Combine the parents' scales.

        Vector3 firstScale =
            firstParent.transform.localScale;

        Vector3 secondScale =
            secondParent.transform.localScale;

        baby.transform.localScale = new Vector3(
            (firstScale.x + secondScale.x) / 2f,
            (firstScale.y + secondScale.y) / 2f,
            (firstScale.z + secondScale.z) / 2f
        );

        // Combine the parents' colours.

        Renderer firstRenderer =
            firstParent.GetComponentInChildren<Renderer>();

        Renderer secondRenderer =
            secondParent.GetComponentInChildren<Renderer>();

        Renderer[] babyRenderers =
            baby.GetComponentsInChildren<Renderer>();

        if (firstRenderer == null || secondRenderer == null)
            return;

        Color firstColour = firstRenderer.material.color;
        Color secondColour = secondRenderer.material.color;

        Color inheritedColour = Color.Lerp(
            firstColour,
            secondColour,
            0.5f
        );

        foreach (Renderer babyRenderer in babyRenderers)
        {
            babyRenderer.material.color = inheritedColour;
        }
    }

    private bool ParentHasMutation(
        Creature firstParent,
        Creature secondParent)
    {
        Stat firstMutation =
            FindStat(firstParent.GetStats(), "Mutation");

        Stat secondMutation =
            FindStat(secondParent.GetStats(), "Mutation");

        return firstMutation != null || secondMutation != null;
    }

    private bool ApplyMutation(
        Creature baby,
        GameObject babyObject)
    {
        if (mutationStatTemplate == null)
            return false;

        List<Stat> babyStats = baby.GetStats();

        if (babyStats == null)
            return false;

        if (FindStat(babyStats, "Mutation") != null)
            return false;

        Stat mutationStat = babyObject.AddComponent(
            mutationStatTemplate.GetType()
        ) as Stat;

        if (mutationStat == null)
            return false;

        CopySerializedSettings(
            mutationStatTemplate,
            mutationStat
        );

        Trait newTrait = new Trait(
            "Mutation",
            Random.Range(1f, 10f),
            "A new trait developed through mutation."
        );

        newTrait.MaxValue = 10f;

        if (!AssignIndependentTrait(mutationStat, newTrait))
        {
            Destroy(mutationStat);
            return false;
        }

        babyStats.Add(mutationStat);

        //Apply a visible size mutation.

        Vector3[] sizes =
        {
            smallMutation,
            largeMutation,
            giantMutation
        };

        babyObject.transform.localScale =
            sizes[Random.Range(0, sizes.Length)];

        // Apply a visible colour mutation.

        Color[] colours =
        {
            redMutation,
            blueMutation,
            greenMutation,
            yellowMutation,
            purpleMutation
        };

        Color mutationColour =
            colours[Random.Range(0, colours.Length)];

        Renderer[] renderers =
            babyObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.material.color = mutationColour;
        }

        return true;
    }

    //This resets the process if a parent leaves, an extra creatures enters, or setup fails.
    private void CancelBreeding()
    {
        isBreeding = false;
        hasBred = false;
        breedingCoroutine = null;

        parent1 = null;
        parent2 = null;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        if (delayText != null)
            delayText.text = "";

        if (resultText != null)
            resultText.text = "Breeding cancelled.";
    }
}
