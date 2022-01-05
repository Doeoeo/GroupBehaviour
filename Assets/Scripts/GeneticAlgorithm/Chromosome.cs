using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chromosome
{
    public float max = 1.0f;

    float[] genes;
    public float[] Genes {get => genes; set => genes = value;}

    int genesLength;
    float mutationRate;

    float fitnessScore;
    public float FitnessScore {get => fitnessScore; set => fitnessScore = value;}

    public Chromosome(float mutationRate, int genesLength) {

        this.mutationRate = mutationRate;
        this.genesLength = genesLength;
        this.genes = new float[genesLength];

        this.GenerateNewGenes();
    }

    // public float CalculateFitnessScore(){

    //     // run simulation
    //     // get number of fish caught and return it as score

    //     // just a temporary testing code, tries to evolve a gene that sums up to the biggest number

    //     float genesSum = 0;
    //     for(int i=0; i<this.genesLength; i++) {
    //         genesSum += genes[i];
    //     }

    //     this.fitnessScore = genesSum;

    //     return genesSum;
    // }

    public void GenerateNewGenes(){

        // TODO(miha): If it is simple tactic generate two random values between 0 and 1.
        //for(int i=0; i<this.genesLength; i++) {
        //    this.genes[i] = this.GenerateSingleGeneFloat(max);
        //}
        
        float rng1 = GenerateSingleGeneFloat(1.0f);
        float rng2 = GenerateSingleGeneFloat(1.0f);

        if(rng1 < rng2) {
            this.genes[0] = rng1;
            this.genes[1] = rng2;
        } else {
            this.genes[0] = rng2;
            this.genes[1] = rng1;
        }

    }

    public float GenerateSingleGeneFloat(float max) {
        return UnityEngine.Random.Range(0.0f, max);
    }

    public void Mutate() {

        for(int i=0; i<this.genesLength; i++) {

            float mutationProbability = Random.Range(0.0f, 1.0f);

            if(mutationRate>mutationProbability){
                this.genes[i] = this.GenerateSingleGeneFloat(max);
            }

        }
    }

    public Chromosome Crossover(Chromosome partner) {

        float[] childGenes = new float[this.genesLength];

        for(int i=0; i<this.genesLength; i++) {
 
            float crossoverProbability = UnityEngine.Random.Range(0.0f, 1.0f);
 
            childGenes[i] = crossoverProbability*this.Genes[i] + (1-crossoverProbability)*partner.Genes[i];
           
        }
        
        Chromosome childChromosome = new Chromosome(this.mutationRate, this.genesLength);
        childChromosome.Genes = childGenes;
        return childChromosome;
    }
    
    public string ToString() {

        string toString = "";
        for(int i=0; i<this.genesLength; i++) {
            toString += this.genes[i] + " ";
        }

        return toString;
    }

}
