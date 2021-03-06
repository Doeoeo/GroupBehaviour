using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Rendering;
using System;
using System.Threading;
using System.IO;
//using System.Runtime.Remo

/* A Monobehaviour that spawns iterations of entities for each evolution cycle
 * The class sets a custom FixedUpdate rate to speed up the behaviour
 * The Monobehaviour can be disabled by the isActive toggle in the edditor
 *  TO DO:
 *      - The generateEntity has to cast the function to a specific IComponent data this can be made more flexible with an array of types or something
 *      - Spawn predators (they will be killed automaticly)
 *      - Create an evaluating function for evolution
 */      

public class SimulationController : MonoBehaviour {

    [SerializeField] private Mesh fishMesh;
    [SerializeField] private Material fishMaterial;

    [SerializeField] private Mesh predatorMesh;
    [SerializeField] private Material predatorMaterial;

    [SerializeField] private int agentNumberParam;
    [SerializeField] public bool isActive;
    [SerializeField] public PredatorType predatorType;

    public static int simulationFrames = 700;
    public static float timestep = 0.005f;
    private static int simulationNo;
    private int currentSimulationFrame;

    public static float bodyLength = 0.1f;

    private EntityManager entityManager;
    FixedStepSimulationSystemGroup a;

    public static Chromosome currentChromosome;
    public static GeneticAlgorithm geneticAlgorithm;
    public static Chromosome bestChromosome;

    public static int randomSeed;

    NativeArray<Entity> fishEntityArray;
    NativeArray<Entity> predatorEntityArray;


    [SerializeField] int populationSize;
    [SerializeField] float mutationRate;
    [SerializeField] int genesLength;
    [SerializeField] int maxGenerations;
    [SerializeField] float thresholdScore;
    [SerializeField] int chromosomeSimulationRepetitions;
    [SerializeField] bool NUJNO_isLockOn;

    public static String outputFileName;
    public static Chromosome bestChromosomeOverall;

    void Start() {
        Time.fixedDeltaTime = timestep;
        currentSimulationFrame = 0;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        fishEntityArray = generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent, fishMaterial, fishMesh);
        disposeOfEntityArray(fishEntityArray);

        randomSeed = (int) UnityEngine.Random.Range(0f, 10000f);

        if(predatorType == PredatorType.SimpleTactic) {
            predatorEntityArray = generateEntity(StaticPredatorSTData.getArchetype(), 1, constructPredatorSTComponent, predatorMaterial, predatorMesh);
        } else {
            predatorEntityArray = generateEntity(StaticPredatorData.getArchetype(), 1, constructPredatorComponent, predatorMaterial, predatorMesh);
        }

        Debug.Log("PEA: " + predatorEntityArray);
        Debug.Log("PEA[0]: " + predatorEntityArray[0]);

        // for (int i = 0; i < predatorEntityArray.Length; i++) {
        //     PredatorPropertiesComponent predator = entityManager.GetComponentData<PredatorPropertiesComponent>(predatorEntityArray[i]);
        //     Debug.Log("num of fish caught of this predator: " + predator.numOfFishCaught);
        // }
        //

        bool isSimpleTactic = (predatorType == PredatorType.SimpleTactic);

        geneticAlgorithm = new GeneticAlgorithm(populationSize, mutationRate, genesLength, maxGenerations, thresholdScore, isSimpleTactic, chromosomeSimulationRepetitions);

        currentChromosome = geneticAlgorithm.GetNextChromosome();


        outputFileName = "results-" + (isSimpleTactic  ? "simple" : "dispersing") + "-" + (NUJNO_isLockOn  ? "LockOn" : "NoLockOn") + ".csv";
        using(StreamWriter w = new StreamWriter(outputFileName))
        {
            w.WriteLine("Evolution: ");
            
            w.WriteLine("\tPredator type: " + (isSimpleTactic  ? "Mixture of simple tactics" : "Dispersing"));
            w.WriteLine("\tLock on: " + (NUJNO_isLockOn  ? "Yes" : "No"));
            w.WriteLine("\tNum of generations: " + maxGenerations);
            w.WriteLine("\tMutation rate: " + mutationRate);
            w.WriteLine("\tSimulations per chromosome: " + chromosomeSimulationRepetitions);

            w.WriteLine("");
            w.WriteLine("\tFrames in each simulation: " + simulationFrames);
            w.WriteLine("\tNumber of agents: " + agentNumberParam);

            w.WriteLine("");
            w.WriteLine("");
            w.WriteLine("generationNum,fishCaught,gene1,gene2,isFinal,isFinalOverall");

        }

        bestChromosomeOverall = new Chromosome(mutationRate, genesLength, isSimpleTactic, chromosomeSimulationRepetitions);
        bestChromosomeOverall.FitnessScore = -1.0f;

    }

    void FixedUpdate() {
        if (!isActive) return;
        // This disaster needs to be called every frame so that we reset the Timestep. If not we are stuck at 60fps.
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FixedStepSimulationSystemGroup>().Timestep = Time.fixedDeltaTime;
        if (currentSimulationFrame == simulationFrames) wrapUp();
        currentSimulationFrame++;
    }


    // Finalize a simulation
    private void wrapUp() {


        // EntityQuery m_Group_p = GetEntityQuery(ComponentType.ReadOnly<PredatorPropertiesComponent>());
        // NativeArray <PredatorPropertiesComponent> predators = m_Group_p.ToComponentDataArray<PredatorPropertiesComponent>(Allocator.TempJob);

        // NOTE(miha): reset random generators seed
        UnityEngine.Random.InitState(randomSeed);

        float fishCaughtScore = 0.0f;
        for (int i = 0; i < predatorEntityArray.Length; i++) {

            if(predatorType == PredatorType.SimpleTactic) {
                PredatorSTPropertiesComponent predator = entityManager.GetComponentData<PredatorSTPropertiesComponent>(predatorEntityArray[i]);
                fishCaughtScore += predator.numOfFishCaught;
            } else {
                PredatorPropertiesComponent predator = entityManager.GetComponentData<PredatorPropertiesComponent>(predatorEntityArray[i]);
                fishCaughtScore += predator.numOfFishCaught;
            }
        }

        disposeOfEntityArray(predatorEntityArray);

        // fishCaughtScore = 0.0f;
        // for(int i = 0; i<currentChromosome.Genes.Length; i++) {
        //     fishCaughtScore += currentChromosome.Genes[i];
        // }
        // Debug.Log("current chromosome score: " + fishCaughtScore);
        //Debug.Log("random not so random?: " + UnityEngine.Random.Range(0f, 10000f));

        Debug.Log("gen-" + geneticAlgorithm.CurrentGenerationNumber + ",chr-" + geneticAlgorithm.CurrentChromosomeIndex + ",rep-" + currentChromosome.SimulationRepetitionsDone + ",score: " + fishCaughtScore);
        // float fitnessScore = StaticPredatorData.getNumOfFishCaught();
        currentChromosome.FitnessScore += fishCaughtScore;

        if(!geneticAlgorithm.IsFinished) {

            if(!geneticAlgorithm.IsGenerationFinished) {

                if(!currentChromosome.AreSimulationsFinished) {
                    currentChromosome.nextSimulation();
                }
                else {
                    currentChromosome.FitnessScore /= chromosomeSimulationRepetitions;
                    Debug.Log("current chromosome score: " + currentChromosome.FitnessScore);

                    currentChromosome = geneticAlgorithm.GetNextChromosome();
                }

            }
            else {

                if(!currentChromosome.AreSimulationsFinished) {
                    currentChromosome.nextSimulation();
                }
                else {
                    currentChromosome.FitnessScore /= chromosomeSimulationRepetitions;
                    Debug.Log("current chromosome score: " + currentChromosome.FitnessScore);


                    bestChromosome = geneticAlgorithm.GetBest();
                    Debug.Log("!! best chromosome of gen " + geneticAlgorithm.CurrentGenerationNumber + " --->" +  bestChromosome.ToString() + ", score: " + bestChromosome.FitnessScore);

                    using (StreamWriter w = File.AppendText(outputFileName))
                    {
                        w.WriteLine(geneticAlgorithm.CurrentGenerationNumber + "," + bestChromosome.FitnessScore + "," + bestChromosome.Genes[0] + "," + bestChromosome.Genes[1] + ",false,false");
                    }

                    if(bestChromosome.FitnessScore > bestChromosomeOverall.FitnessScore) {
                        bestChromosomeOverall = bestChromosome;
                    }

                    if(!geneticAlgorithm.IsFinished) {

                        // TODO(miha): Here we generate new position :)
                        randomSeed = (int) UnityEngine.Random.Range(0f, 10000f);

                        geneticAlgorithm.CreateNextGeneration();
                        currentChromosome = geneticAlgorithm.GetNextChromosome();
                    }
                    else {


                        Debug.Log("!! Final best chromosome --->" + bestChromosome.ToString() + ", score: " + bestChromosome.FitnessScore);
                        using (StreamWriter w = File.AppendText(outputFileName))
                        {
                            w.WriteLine(geneticAlgorithm.CurrentGenerationNumber + "," + bestChromosome.FitnessScore + "," + bestChromosome.Genes[0] + "," + bestChromosome.Genes[1] + ",true,false");
                        }    
                        using (StreamWriter w = File.AppendText(outputFileName))
                        {
                            w.WriteLine(geneticAlgorithm.CurrentGenerationNumber + "," + bestChromosomeOverall.FitnessScore + "," + bestChromosomeOverall.Genes[0] + "," + bestChromosomeOverall.Genes[1] + ",false,true");
                        } 
                        Application.Quit();                      
                        return;                 
                    }

                }

            }

            cleanUp();
            // Debug.Log("Cleared Entities");

            // TODO(miha): Here we need to return same result for the simulation of the generatrion (eg. position of the fish)
            // Create fish
            fishEntityArray = generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent, fishMaterial, fishMesh);
            // Debug.Log("Array len: " + fishEntityArray.Length);
            disposeOfEntityArray(fishEntityArray);
            // Debug.Log("Made Fish");

            float[] evolvingParameters = currentChromosome.Genes;
            StaticPredatorData.setEvolve(evolvingParameters);

            // Create predator

            // TODO(miha): Here we need to return same result for the simulation of the generatrion (eg. position of the predator)
            if(predatorType == PredatorType.SimpleTactic) {
                predatorEntityArray = generateEntity(StaticPredatorSTData.getArchetype(), 1, constructPredatorSTComponent, predatorMaterial, predatorMesh);
            } else {
                predatorEntityArray = generateEntity(StaticPredatorData.getArchetype(), 1, constructPredatorComponent, predatorMaterial, predatorMesh);
            }

            // Debug.Log("Made Predator");

            currentSimulationFrame = 0;

        }
        else {
            
            Debug.Log("KONEC !!!");
            return;

        }

    }

    // Kill every entity
    private void cleanUp() {
        NativeArray<Entity> entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
    }

    // Create new entities
    private NativeArray<Entity> generateEntity(EntityArchetype entityArchetype, int agentNumber, Func<IComponentData> f, Material material, Mesh mesh) {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(agentNumber, Allocator.Persistent);
        entityManager.CreateEntity(entityArchetype, entityArray);

        float bl = 0.1f;
        for (int i = 0; i < entityArray.Length; i++) {

            Entity entity = entityArray[i];
            var t = f();

            if (t is FishPropertiesComponent) entityManager.SetComponentData(entity, (FishPropertiesComponent)t);
            else if (t is PredatorPropertiesComponent) entityManager.SetComponentData(entity, (PredatorPropertiesComponent)t);
            else if (t is PredatorSTPropertiesComponent) entityManager.SetComponentData(entity, (PredatorSTPropertiesComponent)t);

            entityManager.SetComponentData(entity, new Translation {Value = new float3(StaticFishData.getNoIncFloat3())});

            entityManager.SetSharedComponentData(entity, new RenderMesh {
                mesh = mesh,
                material = material
            });

            entityManager.SetComponentData(entity, new Scale {Value = bl / 2});
        }

        return entityArray;
    }

    private void disposeOfEntityArray(NativeArray<Entity> entityArray) {
        entityArray.Dispose();
    }

    // Predefined component for Fish
    private IComponentData constructFishComponent() {
        StaticFishData.reset();
        return new FishPropertiesComponent {
            id = StaticFishData.getIndex(),
            vM = StaticFishData.getNextFloat(),
            vC = StaticFishData.getNextFloat(),
            foV = StaticFishData.getNextFloat(),
            sD = StaticFishData.getNextFloat3(),
            aD = StaticFishData.getNextFloat3(),
            cD = StaticFishData.getNextFloat3(),
            eD = StaticFishData.getNextFloat3(),
            bD = StaticFishData.getNextFloat3(),
            sW = StaticFishData.getNextFloat(),
            aW = StaticFishData.getNextFloat(),
            cW = StaticFishData.getNextFloat(),
            eW = StaticFishData.getNextFloat(),
            bW = StaticFishData.getNextFloat(),
            mA = StaticFishData.getNextFloat(),
            len = StaticFishData.getBl(),
            direction = StaticFishData.getNextFloat3(),
            speed = StaticFishData.getRandom(),
            position = StaticFishData.getRandom(),
        };
    }

    // Predifined component for Predator
    private IComponentData constructPredatorComponent() {
        StaticPredatorData.reset();
        return new PredatorPropertiesComponent {
            vM = StaticPredatorData.getNextFloat(),
            vC = StaticPredatorData.getNextFloat(),
            mA = StaticPredatorData.getNextFloat(),
            len = StaticPredatorData.getBl(),

            direction = StaticPredatorData.getNextFloat3(),
            position = StaticPredatorData.getRandom(),
            speed = StaticPredatorData.getRandom(),

            closestFish = StaticPredatorData.getNextInt(),
            status = StaticPredatorData.getNextInt(),
            restTime = StaticPredatorData.getNextInt(),
            remainingRest = StaticPredatorData.getNextInt(),
            fishToEat = StaticPredatorData.getNextInt(),
            centerFish = StaticPredatorData.getNextInt(),
            mostIsolated = StaticPredatorData.getNextInt(),
            closestGroupRadius = StaticPredatorData.getNextInt(),

            lockOnDistance = StaticPredatorData.getNextEvolve(),
            lockOnRadius = StaticPredatorData.getNextEvolve(),
            numOfFishCaught = StaticPredatorData.getNumOfFishCaught(),
        };
    }

    private IComponentData constructPredatorSTComponent() {
        StaticPredatorSTData.reset();
        return new PredatorSTPropertiesComponent {
            vM = StaticPredatorSTData.getNextFloat(),
            vC = StaticPredatorSTData.getNextFloat(),
            mA = StaticPredatorSTData.getNextFloat(),
            catchDistance = StaticPredatorSTData.getNextFloat(),
            confusionProbability = StaticPredatorSTData.getNextFloat(),
            len = StaticPredatorSTData.getBl(),

            direction = StaticPredatorSTData.getNextFloat3(),
            position = StaticPredatorSTData.getRandom(),
            speed = StaticPredatorSTData.getRandom(),

            restTime = StaticPredatorSTData.getNextInt(),
            remainingRest = StaticPredatorSTData.getNextInt(),

            tactic = StaticPredatorSTData.getSimpleTactic(),
            state = StaticPredatorSTData.getState(),

            firstRandomTacticBarrier = StaticPredatorSTData.getNextEvolve(),
            secondRandomTacticBarrier = StaticPredatorSTData.getNextEvolve(),
            //firstSectionTactic = StaticPredatorSTData.getNextInt(),
            //secondSectionTactic = StaticPredatorSTData.getNextInt(),
            //thirdSectionTactic = StaticPredatorSTData.getNextInt(),
        };
    }


}
