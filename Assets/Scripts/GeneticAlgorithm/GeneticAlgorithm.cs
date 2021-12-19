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
    float fitnessSum;

    float bestFitnessScore;
    public float BestFitnessScore {get => bestFitnessScore; }

    Chromosome bestFitnessChromosome;
    public Chromosome BestFitnessChromosome {get => bestFitnessChromosome; }

    Chromosome[] populationChromosomes;
    int currentChromosomeIndex;

    public GeneticAlgorithm(int populationSize, float mutationRate, int genesLength,  int maxGenerations, float thresholdScore) {

        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.genesLength = genesLength;

        this.maxGenerations = maxGenerations;
        this.thresholdScore = thresholdScore;

        finished = false;
        generationFinished = false;


        Debug.Log("Creating first generation");

        this.currentChromosomeIndex = 0;
        this.generationsNumber = 0;
        this.populationChromosomes = new Chromosome[populationSize];


        for(int i = 0; i<populationSize; i++) {
            this.populationChromosomes[i] = new Chromosome(this.mutationRate, this.genesLength);
        }

        generationsNumber++;

    }

    public void CreateNextGeneration(){

        Debug.Log("Creating generation number " + generationsNumber);
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

        if (this.bestFitnessScore > this.thresholdScore || this.generationsNumber > this.maxGenerations) {
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
