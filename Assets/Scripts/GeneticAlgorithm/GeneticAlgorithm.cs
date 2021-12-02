using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithm {

    int populationSize;
    float mutationRate;
    int genesLength;

    int maxGenerations;
    float thresholdScore;


    public GeneticAlgorithm(int populationSize, float mutationRate, int genesLength, int maxGenerations, float thresholdScore) {
        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.genesLength = genesLength;

        this.maxGenerations = maxGenerations;
        this.thresholdScore = thresholdScore;
    }

    public Chromosome EvolvePredator() {

        Population population = new Population(populationSize, mutationRate, genesLength, maxGenerations, thresholdScore);
        Chromosome bestChromosome = population.GetBest();
        int generationNum = 1;

        while (!population.Finished) {
 
           population.CreateNextGeneration();
           bestChromosome = population.GetBest();

           Debug.Log("Generation: " + generationNum + ", best score this generation: " + population.BestFitnessScore);
           generationNum ++;
        }

        Debug.Log("Finished --> Best score: " + population.BestFitnessScore);

        
        return bestChromosome;
    }
}
