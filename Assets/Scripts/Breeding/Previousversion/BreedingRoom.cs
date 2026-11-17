
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class BreedingRoom : Room
{

    [Header("Display Settings")]
    [SerializeField] private float breedingDelay = 5f;
    [SerializeField] private TextMeshProUGUI delayText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject uiPanel;


    [Header("Spawn Objects")]
    [SerializeField] private GameObject childPrefab1;
    [SerializeField] private GameObject childPrefab2;
    [SerializeField] private GameObject childPrefab3;
    [SerializeField] private Transform babySpawnPoint;

    [Header("Mutation Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.15f;
    [SerializeField] private Stat mutationStatTemplate;


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

    [Header("Mutation Appearance: Colors")]
    [SerializeField] private Color redMutation = Color.red;
    [SerializeField] private Color blueMutation = Color.blue;
    [SerializeField] private Color greenMutation = Color.green;
    [SerializeField] private Color yellowMutation = Color.yellow;
    [SerializeField] private Color purpleMutation = Color.magenta;


    private Transform parent1;
    private Transform parent2;

    private bool isBreeding;
    private bool hasBred;

    private void Awake()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    private void Update()
    {
        CheckForBreeding();
    }

    private void CheckForBreeding()
    {
        List<Transform> creatures = GetChildrenInRoom();

        if (isBreeding)
        {
            if (creatures.Count != 2 ||
                !creatures.Contains(parent1) ||
                !creatures.Contains(parent2))
            {
                StopAllCoroutines();
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

        if (creatures.Count != 2)
            return;

        Creature firstParent = creatures[0].GetComponent<Creature>();
        Creature secondParent = creatures[1].GetComponent<Creature>();

        if (firstParent == null || secondParent == null)
            return;

        parent1 = creatures[0];
        parent2 = creatures[1];

        StartCoroutine(BreedCreatures(firstParent, secondParent));
    }

    private IEnumerator BreedCreatures(
        Creature firstParent,
        Creature secondParent)
    {
        isBreeding = true;

        if (uiPanel != null)
            uiPanel.SetActive(true);

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

            timer -= Time.deltaTime;

            if (delayText != null)
                delayText.text = Mathf.Ceil(Mathf.Max(0f, timer)).ToString();

            yield return null;
        }

        List<Transform> finalCreatures = GetChildrenInRoom();

        if (finalCreatures.Count != 2 ||
            !finalCreatures.Contains(parent1) ||
            !finalCreatures.Contains(parent2))
        {
            CancelBreeding();
            yield break;
        }

        GameObject babyPrefab = GetBabyPrefab(firstParent.GetName());

        if (babyPrefab == null || babySpawnPoint == null)
        {
            CancelBreeding();
            yield break;
        }

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

        if (!InheritTraits(firstParent, secondParent, baby))
        {
            Destroy(babyObject);
            CancelBreeding();
            yield break;
        }

        InheritAppearance(firstParent, secondParent, babyObject);

        bool mutated = Random.value < mutationChance;

        if (mutated && !ApplyMutation(baby, babyObject))
            mutated = false;

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
        List<Stat> firstStats = firstParent.GetStats();
        List<Stat> secondStats = secondParent.GetStats();
        List<Stat> babyStats = baby.GetStats();

        if (firstStats == null ||
            secondStats == null ||
            babyStats == null)
            return false;

        List<string> inheritedNames = new List<string>();

        AddTraitNames(firstStats, inheritedNames);
        AddTraitNames(secondStats, inheritedNames);

        if (inheritedNames.Count == 0)
            return false;

        Dictionary<string, Stat> chosenSources =
            new Dictionary<string, Stat>();

        foreach (string traitName in inheritedNames)
        {
            Stat firstSource = FindStat(firstStats, traitName);
            Stat secondSource = FindStat(secondStats, traitName);

            Stat chosenSource;

            if (firstSource != null && secondSource != null)
            {
                chosenSource = Random.value < 0.5f
                    ? firstSource
                    : secondSource;
            }
            else
            {
                chosenSource = firstSource != null
                    ? firstSource
                    : secondSource;
            }

            if (chosenSource != null && chosenSource.GetTrait() != null)
                chosenSources.Add(traitName, chosenSource);
        }

        List<Stat> oldBabyStats = new List<Stat>(babyStats);
        babyStats.Clear();

        foreach (Stat oldStat in oldBabyStats)
        {
            if (oldStat != null)
                Destroy(oldStat);
        }

        foreach (KeyValuePair<string, Stat> entry in chosenSources)
        {
            Stat source = entry.Value;

            Stat newStat = baby.gameObject.AddComponent(source.GetType()) as Stat;

            if (newStat == null)
                return false;

            CopySerializedSettings(source, newStat);

            if (!AssignIndependentTrait(newStat, source.GetTrait()))
            {
                Destroy(newStat);
                return false;
            }

            babyStats.Add(newStat);
        }

        return babyStats.Count > 0;
    }

    private void AddTraitNames(List<Stat> stats, List<string> names)
    {
        foreach (Stat stat in stats)
        {
            if (stat == null || stat.GetTrait() == null)
                continue;

            string traitName = stat.GetTrait().Name;

            if (!string.IsNullOrEmpty(traitName) &&
                !names.Contains(traitName))
                names.Add(traitName);
        }
    }

    private Stat FindStat(List<Stat> stats, string traitName)
    {
        foreach (Stat stat in stats)
        {
            if (stat != null &&
                stat.GetTrait() != null &&
                stat.GetTrait().Name == traitName)
                return stat;
        }

        return null;
    }

    private void CopySerializedSettings(Stat source, Stat destination)
    {
        System.Type type = source.GetType();

        while (type != null &&
               type != typeof(Stat) &&
               type != typeof(MonoBehaviour) &&
               type != typeof(Behaviour) &&
               type != typeof(Component) &&
               type != typeof(Object))
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
                    field.SetValue(destination, field.GetValue(source));
                }
                catch (System.Exception)
                {
                    // This continues if an individual field cannot be copied.
                }
            }

            type = type.BaseType;
        }
    }

    private bool AssignIndependentTrait(Stat destination, Trait source)
    {
        if (destination == null || source == null)
            return false;

        FieldInfo traitField = typeof(Stat).GetField(
            "trait",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (traitField == null)
            return false;

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
        Transform chosenParent = Random.value < 0.5f
            ? firstParent.transform
            : secondParent.transform;

        baby.transform.localScale = chosenParent.localScale;

        Renderer parentRenderer =
            chosenParent.GetComponentInChildren<Renderer>();

        if (parentRenderer == null)
            return;

        Renderer[] babyRenderers =
            baby.GetComponentsInChildren<Renderer>();

        foreach (Renderer babyRenderer in babyRenderers)
            babyRenderer.material.color = parentRenderer.material.color;
    }

    private bool ApplyMutation(Creature baby, GameObject babyObject)
    {
        if (mutationStatTemplate == null)
            return false;

        List<Stat> babyStats = baby.GetStats();

        if (babyStats == null)
            return false;

        Stat existingMutation = FindStat(babyStats, "Mutation");
        Stat mutationStat = existingMutation;

        if (mutationStat == null)
        {
            mutationStat = babyObject.AddComponent(
                mutationStatTemplate.GetType()
            ) as Stat;

            if (mutationStat == null)
                return false;

            CopySerializedSettings(mutationStatTemplate, mutationStat);
            babyStats.Add(mutationStat);
        }

        Trait newTrait = new Trait(
            "Mutation",
            Random.Range(1f, 10f),
            "A new trait developed through mutation."
        );

        newTrait.MaxValue = 10f;

        if (!AssignIndependentTrait(mutationStat, newTrait))
        {
            if (existingMutation == null)
            {
                babyStats.Remove(mutationStat);
                Destroy(mutationStat);
            }

            return false;
        }

        Vector3[] sizes =
        {
            smallMutation,
            largeMutation,
            giantMutation
        };

        Color[] colours =
        {
            redMutation,
            blueMutation,
            greenMutation,
            yellowMutation,
            purpleMutation
        };

        babyObject.transform.localScale =
            sizes[Random.Range(0, sizes.Length)];

        Color mutationColour =
            colours[Random.Range(0, colours.Length)];

        Renderer[] renderers =
            babyObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
            renderer.material.color = mutationColour;

        return true;
    }

    private void CancelBreeding()
    {
        isBreeding = false;
        hasBred = false;

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
