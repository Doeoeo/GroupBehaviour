using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chromosome
{
    float[] genes;
    public float[] Genes {get => genes; set => genes = value;}

    int genesLength;
    float mutationRate;

    float fitnessScore;
    public float FitnessScore {get => fitnessScore; set => fitnessScore = value;}

    public bool simpleTactic;
    public float disperseTacticRandom = 20.0f;
    public float simpleTacticRandom = 1.0f;

    public Chromosome(float mutationRate, int genesLength, bool simpleTactic) {

        this.mutationRate = mutationRate;
        this.genesLength = genesLength;
        this.genes = new float[genesLength];
        this.simpleTactic = simpleTactic;

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
        if(this.simpleTactic) {
            float rng1 = GenerateSingleGeneFloat(simpleTacticRandom);
            float rng2 = GenerateSingleGeneFloat(simpleTacticRandom);

            if(rng1 < rng2) {
                this.genes[0] = rng1;
                this.genes[1] = rng2;
            } else {
                this.genes[0] = rng2;
                this.genes[1] = rng1;
            }
        } else {
            for(int i=0; i<this.genesLength; i++) {
                this.genes[i] = this.GenerateSingleGeneFloat(disperseTacticRandom);
            }
        }

    }

    public float GenerateSingleGeneFloat(float max) {
        return UnityEngine.Random.Range(0.0f, max);
    }

    public void Mutate() {

        for(int i=0; i<this.genesLength; i++) {

            float mutationProbability = Random.Range(0.0f, 1.0f);

            if(mutationRate>mutationProbability){
                if(this.simpleTactic) {
                    this.genes[i] = this.GenerateSingleGeneFloat(simpleTacticRandom);
                } else {
                    this.genes[i] = this.GenerateSingleGeneFloat(disperseTacticRandom);
                }
            }

        }
    }

    public Chromosome Crossover(Chromosome partner) {

        float[] childGenes = new float[this.genesLength];

        for(int i=0; i<this.genesLength; i++) {
 
            float crossoverProbability = UnityEngine.Random.Range(0.0f, 1.0f);
 
            childGenes[i] = crossoverProbability*this.Genes[i] + (1-crossoverProbability)*partner.Genes[i];
           
        }
        
        Chromosome childChromosome = new Chromosome(this.mutationRate, this.genesLength, this.simpleTactic);
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
