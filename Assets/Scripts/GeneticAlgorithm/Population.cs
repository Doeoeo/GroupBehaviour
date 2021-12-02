using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Population {

    int populationSize;
    float mutationRate;
    int genesLength;

    int maxGenerations;
    float thresholdScore;

    bool finished = false;
    public bool Finished { get => finished; }    


    int generationsNumber;
    float fitnessSum;

    float bestFitnessScore;
    public float BestFitnessScore {get => bestFitnessScore; }

    Chromosome bestFitnessChromosome;
    public Chromosome BestFitnessChromosome {get => bestFitnessChromosome; }

    Chromosome[] populationChromosomes;

    public Population(int populationSize, float mutationRate, int genesLength,  int maxGenerations, float thresholdScore) {

        Debug.Log("Creating population");

        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.genesLength = genesLength;

        this.maxGenerations = maxGenerations;
        this.thresholdScore = thresholdScore;

        this.generationsNumber = 0;
        this.populationChromosomes = new Chromosome[populationSize];



        for(int i = 0; i<populationSize; i++) {
            Chromosome newChromosome = new Chromosome(this.mutationRate, this.genesLength);
            newChromosome.CalculateFitnessScore();

            this.populationChromosomes[i] = newChromosome;

        }

    }

    public void CreateNextGeneration(){

        // Debug.Log("Creating next generation");

        Chromosome[] newPopulationChromosomes = new Chromosome[this.populationSize];

        for(int i = 0; i<this.populationSize; i++){
            Chromosome partnerA = PickChromosomeWeighted();
            Chromosome partnerB = PickChromosomeWeighted();
        
            Chromosome childChromosome = partnerA.Crossover(partnerB);

            childChromosome.Mutate();
            childChromosome.CalculateFitnessScore();
 
           newPopulationChromosomes[i] = childChromosome;

        }

        this.generationsNumber++;
        this.populationChromosomes = newPopulationChromosomes;
    }


    public Chromosome PickChromosomeWeighted(){
        while(true) {

            Chromosome chromosome = this.populationChromosomes[UnityEngine.Random.Range(0,this.populationSize)];
            float acceptThreshold = UnityEngine.Random.Range(0, this.bestFitnessScore);

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


    // public void CalculateFitnessScore() {
    //     for(int i = 0; i<this.populationSize; i++) {
    //         populationChromosomes[i].CalculateFitness();
    //     }
    // }

}
