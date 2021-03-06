using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm {

    int populationSize;
    float mutationRate;
    int genesLength;

    int maxGenerations;
    float thresholdScore;

    bool finished;
    public bool IsFinished { get => finished; }

    bool generationFinished;
    public bool IsGenerationFinished { get => generationFinished; }

    int generationsNumber;
    public int CurrentGenerationNumber { get => generationsNumber; }

    float fitnessSum;
    int chromosomeSimulationRepetitions;

    float bestFitnessScore;
    public float BestFitnessScore {get => bestFitnessScore; }

    Chromosome bestFitnessChromosome;
    public Chromosome BestFitnessChromosome {get => bestFitnessChromosome; }

    Chromosome[] populationChromosomes;
    int currentChromosomeIndex;
     public int CurrentChromosomeIndex {get => currentChromosomeIndex; }

    public GeneticAlgorithm(int populationSize, float mutationRate, int genesLength,  int maxGenerations, float thresholdScore, bool simpleTactic, int chromosomeSimulationRepetitions) {

        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.genesLength = genesLength;
        this.chromosomeSimulationRepetitions = chromosomeSimulationRepetitions;

        this.maxGenerations = maxGenerations;
        this.thresholdScore = thresholdScore;

        finished = false;
        generationFinished = false;


        this.currentChromosomeIndex = 0;
        this.generationsNumber = 0;
        this.populationChromosomes = new Chromosome[populationSize];


        for(int i = 0; i<populationSize; i++) {
            this.populationChromosomes[i] = new Chromosome(this.mutationRate, this.genesLength, simpleTactic, this.chromosomeSimulationRepetitions);
        }

        generationsNumber++;
        Debug.Log("Creating generation number " + generationsNumber);

    }

    public void CreateNextGeneration(){


        generationFinished = false;
        currentChromosomeIndex = 0;


        Chromosome[] newPopulationChromosomes = new Chromosome[this.populationSize];

        for(int i = 0; i<this.populationSize; i++){
            Chromosome partnerA = PickChromosomeWeighted();
            Chromosome partnerB = PickChromosomeWeighted();
        
            Chromosome childChromosome = partnerA.Crossover(partnerB);

            childChromosome.Mutate();
            // childChromosome.CalculateFitnessScore();
 
           newPopulationChromosomes[i] = childChromosome;

        }

        this.generationsNumber++;
        this.populationChromosomes = newPopulationChromosomes;
        Debug.Log("Creating generation number " + generationsNumber);
    }


    public Chromosome PickChromosomeWeighted(){
        while(true) {

            Chromosome chromosome = this.populationChromosomes[UnityEngine.Random.Range(0,this.populationSize)];
            float acceptThreshold = UnityEngine.Random.Range(0, this.bestFitnessScore);
            // Debug.Log("acceptthresh: " + acceptThreshold);

            if (acceptThreshold < chromosome.FitnessScore){
                return chromosome;
            }
       
        }
    }


    public Chromosome GetBest() {
        this.bestFitnessScore = 0;
        this.bestFitnessChromosome = null;

        this.fitnessSum = 0;

        for(int i = 0; i<this.populationSize; i++) {

            Chromosome currentChromosome = populationChromosomes[i];
            float currentFitness = currentChromosome.FitnessScore;
            
            this.fitnessSum += currentFitness;

            if (currentFitness > bestFitnessScore){
                this.bestFitnessScore = currentFitness;
                this.bestFitnessChromosome = currentChromosome;
            }


        }

        if (this.bestFitnessScore > this.thresholdScore || this.generationsNumber >= this.maxGenerations) {
            this.finished = true;
        }
        
        return this.bestFitnessChromosome;
    }

    public Chromosome GetNextChromosome() {
        if(currentChromosomeIndex==(populationSize - 1)) {
            generationFinished = true;
            currentChromosomeIndex++;
            return populationChromosomes[currentChromosomeIndex-1];
        }
        else if(currentChromosomeIndex > (populationSize - 1)) {
            return null;
        }
        else{
            currentChromosomeIndex++;
            return populationChromosomes[currentChromosomeIndex-1];
        }
        
    }

}
